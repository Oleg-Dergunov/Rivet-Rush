using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RivetView : MonoBehaviour
{
    [SerializeField] RivetController rivet;
    [SerializeField] GameManager game;

    [Header("Poses")]
    [SerializeField] Sprite idle;
    [SerializeField] Sprite grab;
    [SerializeField] Sprite success;
    [SerializeField] Sprite fumble;
    [SerializeField] Sprite sad;

    [Header("Timing (seconds)")]
    [SerializeField] float grabTime = 0.15f;
    [SerializeField] float successTime = 0.6f;
    [SerializeField] float fumbleTime = 0.6f;

    [Header("Motion")]
    [SerializeField] float pixelsPerUnit = 16f;
    [SerializeField] float bobRate = 1.5f;

    [Header("Menus")]
    [SerializeField] Vector3 asideOffset = new Vector3(-5f, 0f, 0f);
    [SerializeField] float asideTime = 0.3f;

    SpriteRenderer sr;
    Vector3 basePos;
    float poseUntil;
    Sprite queued;
    float queuedTime;
    bool over;
    bool aside;
    float asideT;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        basePos = transform.localPosition;
        sr.sprite = idle;
    }

    void OnEnable()
    {
        rivet.PickedUp += OnPickedUp;
        rivet.Built += OnBuilt;
        rivet.Fumbled += OnFumbled;
        game.GameEnded += OnGameEnded;
    }

    void OnDisable()
    {
        rivet.PickedUp -= OnPickedUp;
        rivet.Built -= OnBuilt;
        rivet.Fumbled -= OnFumbled;
        game.GameEnded -= OnGameEnded;
    }

    public void SetAside(bool value, bool instant = false)
    {
        aside = value;
        if (instant) asideT = value ? 1f : 0f;
    }

    void OnPickedUp() => Show(grab, grabTime);
    void OnBuilt() => ShowThen(grab, grabTime, success, successTime);
    void OnFumbled() => ShowThen(grab, grabTime, fumble, fumbleTime);

    void OnGameEnded()
    {
        over = true;
        if (Time.time < poseUntil && sr.sprite == grab)
        {
            queued = sad;
            queuedTime = 0f;
        }
        else
        {
            queued = null;
            sr.sprite = sad;
        }
    }

    void Show(Sprite s, float time)
    {
        if (over) return;
        sr.sprite = s;
        poseUntil = Time.time + time;
        queued = null;
    }

    void ShowThen(Sprite first, float firstTime, Sprite then, float thenTime)
    {
        if (over) return;
        Show(first, firstTime);
        queued = then;
        queuedTime = thenTime;
    }

    void Update()
    {
        float px = 1f / pixelsPerUnit;
        bool posing = Time.time < poseUntil;

        if (!posing)
        {
            if (queued != null)
            {
                sr.sprite = queued;
                poseUntil = Time.time + queuedTime;
                queued = null;
                posing = Time.time < poseUntil;
            }
            else if (over) sr.sprite = sad;
            else if (sr.sprite != idle) sr.sprite = idle;
        }

        Vector3 move = Vector3.zero;
        if (posing && sr.sprite == grab)
            move.y = -px;
        else if (over)
            move.y = -px;
        else if (posing && sr.sprite == success)
            move.y = 2f * px;
        else if (posing && sr.sprite == fumble)
            move.x = (Mathf.Repeat(Time.time * 20f, 2f) < 1f ? -1f : 1f) * px;
        else
            move.y = Mathf.Repeat(Time.time * bobRate, 1f) < 0.5f ? 0f : px;

        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
        asideT = Mathf.MoveTowards(asideT, aside ? 1f : 0f, dt / asideTime);
        float e = asideT * asideT * (3f - 2f * asideT);
        Vector3 p = basePos + asideOffset * e + move;
        p.x = Mathf.Round(p.x * pixelsPerUnit) / pixelsPerUnit;
        p.y = Mathf.Round(p.y * pixelsPerUnit) / pixelsPerUnit;
        transform.localPosition = p;
    }
}