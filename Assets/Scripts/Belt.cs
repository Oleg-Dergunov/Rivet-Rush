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
    [Tooltip("Belt animation frames (Belt_0..3). Each frame moves the surface 1 px to the right. " +
             "Import with Mesh Type = Full Rect so they can tile.")]
    [SerializeField] Sprite[] beltFrames;
    [SerializeField] Sprite grabMarkerSprite;
    [Tooltip("Above the chips, so the marker stays visible when it locks onto one")]
    [SerializeField] int grabMarkerOrder = 3;
    [SerializeField] Sprite cornerCapSprite;
    [SerializeField] int beltSortingOrder = 0;
    [SerializeField] float pixelsPerUnit = 16f;

    [Header("Slots")]
    [Tooltip("Perimeter is 2 x (width + height). Slots = perimeter gives one tile per chip, nose to tail.")]
    [SerializeField] int slotCount = 38;
    [SerializeField] ChipView chipPrefab;
    [SerializeField] int startingChips = 10;

    [Header("Dispenser drop")]
    [Tooltip("How far above the top belt the chip starts (hidden behind the dispenser sprite)")]
    [SerializeField] float dropHeight = 1f;
    [Tooltip("How many slots before the dispenser the drop starts. Smaller = faster drop.")]
    [SerializeField, Range(0.25f, 2f)] float dropSlots = 1f;

    [Header("Tuning (GameManager will drive these later)")]
    [Tooltip("Slots per second")]
    public float speed = 2f;
    [Tooltip("How far (in slots) a chip can be from the grab point and still count as 'in the zone'")]
    [Range(0.1f, 0.5f)] public float grabWindow = 0.45f;
    [Tooltip("Seconds between new chips from the dispenser")]
    public float spawnInterval = 1.5f;
    [Range(0f, 1f)] public float burntChance = 0f;
    [Range(0f, 1f)] public float rainbowChance = 0f;
    [HideInInspector] public int colorsInPlay = 3;

    public event Action OnJam;
    public event Action Dispensed;

    readonly List<SpriteRenderer> beltSides = new();
    readonly List<int> incoming = new();
    SpriteRenderer grabMarker;
    ChipView[] slots;
    float offset;
    float spawnTimer;
    int pending;
    bool running;

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
        pending = 0;
        incoming.Clear();

        int n = Mathf.Clamp(startingChips, 0, slotCount);
        for (int k = 0; k < n; k++)
            Place(Mathf.RoundToInt((k + 0.5f) * slotCount / n) % slotCount, RandomChip());

        running = true;
    }

    public void Stop() => running = false;

    void Update()
    {
        if (!running) return;

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
                if (Mathf.Repeat(claimPoint - prev, slotCount) <= delta)
                {
                    Place(i, RandomChip());
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
            if (slots[i] != null) slots[i].transform.position = SlotPosition(i);

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
        return d;
    }

    public bool DropAfterGrab(IReadOnlyList<ChipData> chips)
    {
        if (chips.Count == 0) return true;

        var empty = new List<int>();
        for (int i = 0; i < slotCount; i++)
            if (slots[i] == null) empty.Add(i);

        float start = GrabPos + grabWindow;
        empty.Sort((a, b) =>
            Mathf.Repeat(a + offset - start, slotCount).CompareTo(Mathf.Repeat(b + offset - start, slotCount)));

        int placed = Mathf.Min(chips.Count, empty.Count);
        for (int k = 0; k < placed; k++)
            Place(empty[k], chips[k]);

        return placed == chips.Count;
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