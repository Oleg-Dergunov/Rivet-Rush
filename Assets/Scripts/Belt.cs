using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Belt : MonoBehaviour
{
    [Header("Shape")]
    [SerializeField] float width = 14f;
    [SerializeField] float height = 5f;

    [Header("Visuals (built automatically on Awake)")]
    [SerializeField] Sprite[] beltFrames;
    [SerializeField] Sprite grabMarkerSprite;
    [SerializeField] int grabMarkerOrder = 3;
    [SerializeField] Sprite cornerCapSprite;
    [SerializeField] int beltSortingOrder = 0;
    [SerializeField] float pixelsPerUnit = 16f;

    [Header("Slots")]
    [SerializeField] int slotCount = 38;
    [SerializeField] ChipView chipPrefab;
    [SerializeField] BlueprintBoard board;
    [SerializeField] int startingChips = 10;

    [Header("Dispenser drop")]
    [SerializeField] float dropHeight = 1f;
    [SerializeField, Range(0.25f, 2f)] float dropSlots = 1f;

    [Header("Tuning (set by Difficulty at runtime)")]
    public float speed = 2f;
    [Range(0.1f, 0.5f)] public float grabWindow = 0.45f;
    public float spawnInterval = 1.5f;
    [Range(0f, 1f)] public float burntChance = 0f;
    [Range(0f, 1f)] public float rainbowChance = 0f;
    [HideInInspector] public int colorsInPlay = 3;

    [Header("Effects")]
    [SerializeField] float discardDrop = 0.75f;
    [SerializeField] float discardTime = 0.5f;
    [SerializeField]
    AnimationCurve blinkCurve = new AnimationCurve(
        new Keyframe(0f, 1f), new Keyframe(0.4f, 0.1f), new Keyframe(0.5f, 0.1f), new Keyframe(0.8f, 1f));

    [Header("Fumble animation")]
    [SerializeField] float flyTime = 0.35f;
    [SerializeField] float flyStagger = 0.05f;
    [SerializeField] float flyArc = 0.5f;
    [SerializeField] float shiftTime = 0.25f;

    class Move
    {
        public bool fromTray;
        public Vector3 fromWorld;
        public float lag;
        public float start;
        public float delay;
        public float duration;
    }

    struct Carried
    {
        public ChipView view;
        public int fromSlot;
        public float prevLag;
        public Vector3 fromWorld;
        public float delay;
    }

    public event Action OnJam;
    public event Action Dispensed;

    readonly List<SpriteRenderer> beltSides = new();
    readonly List<int> incoming = new();
    readonly Dictionary<int, Move> moves = new();
    readonly List<int> moveKeys = new();
    SpriteRenderer grabMarker;
    ChipView[] slots;
    float offset;
    float spawnTimer;
    int pending;
    bool running;
    bool blinking;
    float blinkStart;

    const float DepthStep = 0.001f;

    float Perimeter => 2f * (width + height);
    float GrabPos => width * 0.5f / Perimeter * slotCount;
    float DispenserPos => (1.5f * width + height) / Perimeter * slotCount;

    public Vector3 GrabPoint => PointOnPath(GrabPos / slotCount);
    public Vector3 DispenserPoint => PointOnPath(DispenserPos / slotCount);

    void Awake() => BuildVisuals();

    public void Begin()
    {
        slots = new ChipView[slotCount];
        offset = 0f;
        spawnTimer = 0f;
        pending = 1;
        incoming.Clear();
        moves.Clear();

        int n = Mathf.Clamp(startingChips, 0, slotCount);
        for (int k = 0; k < n; k++)
            Place(Mathf.RoundToInt((k + 0.5f) * slotCount / n) % slotCount, RandomChip());

        running = true;
    }

    public void Stop()
    {
        running = false;
        blinking = true;
        blinkStart = Time.time;
    }

    void Update()
    {
        if (!running)
        {
            moveKeys.Clear();
            moveKeys.AddRange(moves.Keys);
            foreach (int i in moveKeys)
                if (slots[i] != null) slots[i].transform.position = MovingPosition(i);
            if (blinking) Blink();
            return;
        }

        float delta = speed * Time.deltaTime;
        float prevOffset = offset;
        offset += delta;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer -= spawnInterval;
            pending++;
        }

        if (pending > 0)
        {
            float claimPoint = DispenserPos - dropSlots;
            for (int i = 0; i < slotCount && pending > 0; i++)
            {
                if (slots[i] != null) continue;
                float prev = Mathf.Repeat(i + prevOffset, slotCount);
                float dist = Mathf.Repeat(claimPoint - prev, slotCount);
                if (dist > 0f && dist <= delta)
                {
                    Place(i, NextDispensed());
                    slots[i].SetOnTop(true); 
                    incoming.Add(i);
                    pending--;
                    Dispensed?.Invoke();
                }
            }

            if (pending > 0 && CountEmpty() == 0 && incoming.Count == 0)
            {
                running = false;
                OnJam?.Invoke();
            }
        }

        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i] == null) continue;
            slots[i].transform.position = moves.ContainsKey(i) ? MovingPosition(i) : SlotPosition(i);
        }

        UpdateIncoming();
        UpdateGrabMarker();
        AnimateBelt();
    }

    void UpdateGrabMarker()
    {
        if (grabMarker == null) return;
        int s = SlotAtGrab();
        bool locked = s >= 0 && slots[s] != null && !incoming.Contains(s);
        Vector3 p = locked ? slots[s].transform.position : GrabPoint;
        p.z = 0f;
        grabMarker.transform.position = p;
    }

    void UpdateIncoming()
    {
        for (int k = incoming.Count - 1; k >= 0; k--)
        {
            int i = incoming[k];
            float left = Mathf.Repeat(DispenserPos - (i + offset), slotCount);
            if (left > dropSlots || slots[i] == null)
            {
                if (slots[i] != null) slots[i].SetOnTop(false);
                incoming.RemoveAt(k);
                continue;
            }
            float p = 1f - left / dropSlots;
            slots[i].transform.position = DispenserPoint + Vector3.up * (dropHeight * (1f - p));
        }
    }

    Vector3 MovingPosition(int i)
    {
        Move m = moves[i];
        float k = Mathf.Clamp01((Time.time - m.start - m.delay) / m.duration);
        float e = 1f - (1f - k) * (1f - k);

        if (k >= 1f)
        {
            moves.Remove(i);
            if (m.fromTray) slots[i].SetOnTop(false);
            return SlotPosition(i);
        }

        if (m.fromTray)
        {
            Vector3 p = Vector3.Lerp(m.fromWorld, SlotPosition(i), e);
            p.y += flyArc * 4f * k * (1f - k);
            return p;
        }

        float u = i + offset - m.lag * (1f - e);
        Vector3 q = PointOnPath(Mathf.Repeat(u, slotCount) / slotCount);
        q.z = Mathf.Repeat(u - GrabPos, slotCount) * DepthStep;
        return q;
    }

    float RemainingLag(int i)
    {
        if (!moves.TryGetValue(i, out Move m) || m.fromTray) return 0f;
        float k = Mathf.Clamp01((Time.time - m.start - m.delay) / m.duration);
        float e = 1f - (1f - k) * (1f - k);
        return m.lag * (1f - e);
    }

    void Blink()
    {
        float length = blinkCurve.keys[blinkCurve.length - 1].time;
        float a = blinkCurve.Evaluate(Mathf.Repeat(Time.time - blinkStart, length));
        foreach (var s in slots)
            if (s != null) s.SetAlpha(a);
    }

    void AnimateBelt()
    {
        if (beltFrames == null || beltFrames.Length == 0) return;
        float travelledPixels = offset * (Perimeter / slotCount) * pixelsPerUnit;
        Sprite frame = beltFrames[Mathf.FloorToInt(travelledPixels) % beltFrames.Length];
        foreach (var side in beltSides) side.sprite = frame;
    }

    public int SlotAtGrab()
    {
        float f = Mathf.Repeat(GrabPos - offset, slotCount);
        float nearest = Mathf.Round(f);
        if (Mathf.Abs(f - nearest) > grabWindow) return -1;
        return (int)nearest % slotCount;
    }

    public bool TryPeek(int i, out ChipData data)
    {
        data = default;
        if (i < 0 || slots[i] == null) return false;
        data = slots[i].Data;
        return true;
    }

    public ChipData Take(int i)
    {
        ChipData d = slots[i].Data;
        Destroy(slots[i].gameObject);
        slots[i] = null;
        moves.Remove(i);
        return d;
    }

    public void Discard(int i)
    {
        ChipView v = slots[i];
        slots[i] = null;
        moves.Remove(i);
        v.Vanish(Vector3.down * discardDrop, discardTime);
    }

    public bool DropAfterGrab(IReadOnlyList<ChipData> chips, IReadOnlyList<Vector3> fromPositions)
    {
        if (chips.Count == 0) return true;

        float start = GrabPos + grabWindow;
        var order = new List<int>();
        for (int i = 0; i < slotCount; i++)
            if (!incoming.Contains(i)) order.Add(i);
        order.Sort((a, b) =>
            Mathf.Repeat(a + offset - start, slotCount).CompareTo(Mathf.Repeat(b + offset - start, slotCount)));

        var queue = new Queue<Carried>();
        for (int k = 0; k < chips.Count; k++)
        {
            ChipView v = Instantiate(chipPrefab, transform);
            v.Set(chips[k]);
            v.transform.position = fromPositions[k];
            v.SetOnTop(true);
            queue.Enqueue(new Carried { view = v, fromSlot = -1, fromWorld = fromPositions[k], delay = k * flyStagger });
        }

        foreach (int s in order)
        {
            if (queue.Count == 0) break;

            if (slots[s] != null)
            {
                if (moves.TryGetValue(s, out Move old) && old.fromTray) slots[s].SetOnTop(false);
                queue.Enqueue(new Carried { view = slots[s], fromSlot = s, prevLag = RemainingLag(s) });
                moves.Remove(s);
            }

            Carried next = queue.Dequeue();
            slots[s] = next.view;

            if (next.fromSlot < 0)
                moves[s] = new Move { fromTray = true, fromWorld = next.fromWorld, start = Time.time, delay = next.delay, duration = flyTime };
            else
                moves[s] = new Move { lag = Mathf.Repeat(s - next.fromSlot, slotCount) + next.prevLag, start = Time.time, duration = shiftTime };
        }

        bool fit = queue.Count == 0;
        while (queue.Count > 0)
            queue.Dequeue().view.Vanish(Vector3.down * discardDrop, discardTime);
        return fit;
    }

    void Place(int i, ChipData d)
    {
        ChipView v = Instantiate(chipPrefab, transform);
        v.Set(d);
        v.transform.position = SlotPosition(i);
        slots[i] = v;
    }

    int CountEmpty()
    {
        int n = 0;
        foreach (var s in slots) if (s == null) n++;
        return n;
    }

    ChipData RandomChip()
    {
        float r = Random.value;
        if (r < burntChance) return new ChipData(ChipKind.Burnt);
        if (r < burntChance + rainbowChance) return new ChipData(ChipKind.Rainbow);
        return ChipData.Normal((ChipColor)Random.Range(0, colorsInPlay));
    }

    ChipData NextDispensed()
    {
        ChipColor? missing = MissingColor();
        return missing.HasValue ? ChipData.Normal(missing.Value) : RandomChip();
    }

    ChipColor? MissingColor()
    {
        if (board == null) return null;

        var present = new bool[4];
        foreach (var s in slots)
        {
            if (s == null) continue;
            if (s.Data.kind == ChipKind.Rainbow) return null;
            if (s.Data.kind == ChipKind.Normal) present[(int)s.Data.color] = true;
        }

        var missing = new List<ChipColor>();
        for (int c = 0; c < colorsInPlay; c++)
            if (!present[c] && board.Needs((ChipColor)c)) missing.Add((ChipColor)c);

        if (missing.Count == 0) return null;
        return missing[Random.Range(0, missing.Count)];
    }

    Vector3 SlotPosition(int i)
    {
        Vector3 p = PointOnPath(Mathf.Repeat(i + offset, slotCount) / slotCount);
        p.z = Mathf.Repeat(i + offset - GrabPos, slotCount) * DepthStep;
        return p;
    }

    Vector3 PointOnPath(float t)
    {
        Vector3 c = transform.position;
        float hw = width * 0.5f, hh = height * 0.5f;
        float d = Mathf.Repeat(t, 1f) * Perimeter;

        if (d < width) return c + new Vector3(-hw + d, -hh);
        d -= width;
        if (d < height) return c + new Vector3(hw, -hh + d);
        d -= height;
        if (d < width) return c + new Vector3(hw - d, hh);
        d -= width;
        return c + new Vector3(-hw, hh - d);
    }

    void BuildVisuals()
    {
        float hw = width * 0.5f, hh = height * 0.5f;
        if (beltFrames != null && beltFrames.Length > 0)
        {
            Sprite first = beltFrames[0];
            var bottom = MakeSprite("Belt Bottom", first, new Vector3(-0.5f, -hh), 0f, new Vector2(width, 1f));
            var right = MakeSprite("Belt Right", first, new Vector3(hw, -0.5f), 90f, new Vector2(height, 1f));
            var top = MakeSprite("Belt Top", first, new Vector3(0.5f, hh), 180f, new Vector2(width, 1f));
            var left = MakeSprite("Belt Left", first, new Vector3(-hw, 0.5f), 270f, new Vector2(height, 1f));

            beltSides.Add(bottom);
            beltSides.Add(right);
            beltSides.Add(top);
            beltSides.Add(left);

            AddCaps(bottom, width);
            AddCaps(right, height);
            AddCaps(top, width);
            AddCaps(left, height);
        }
        if (grabMarkerSprite != null)
        {
            grabMarker = MakeSprite("Grab Marker", grabMarkerSprite, new Vector3(0f, -hh), 0f, null);
            grabMarker.sortingOrder = grabMarkerOrder;
        }
    }

    void AddCaps(SpriteRenderer side, float length)
    {
        if (cornerCapSprite == null) return;
        float capWidth = cornerCapSprite.bounds.size.x;
        float px = 1f / pixelsPerUnit;

        AddCap(side, "Corner Cap", -length * 0.5f + capWidth * 0.5f, false);
        AddCap(side, "End Shadow", length * 0.5f + capWidth * 0.5f - px, true);
    }

    void AddCap(SpriteRenderer side, string name, float x, bool flip)
    {
        var go = new GameObject(name);
        go.transform.SetParent(side.transform, false);
        go.transform.localPosition = new Vector3(x, 0f, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = cornerCapSprite;
        sr.flipX = flip;
        sr.sortingOrder = beltSortingOrder + 1;
    }

    SpriteRenderer MakeSprite(string name, Sprite sprite, Vector3 localPos, float angle, Vector2? tiledSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = beltSortingOrder;
        if (tiledSize.HasValue)
        {
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = tiledSize.Value;
        }
        return sr;
    }

    void OnDrawGizmos()
    {
        if (slotCount <= 0 || width <= 0f || height <= 0f) return;
        Gizmos.color = Color.gray;
        Gizmos.DrawWireCube(transform.position, new Vector3(width, height, 0f));
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(GrabPoint, 0.4f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(DispenserPoint, 0.4f);
    }
}