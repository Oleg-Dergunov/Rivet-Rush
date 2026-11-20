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
    [Tooltip("Idle bob: how many times per second Rivet moves up a pixel")]
    [SerializeField] float bobRate = 1.5f;

    SpriteRenderer sr;
    Vector3 basePos;
    float poseUntil;
    bool over;

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

    void OnPickedUp() => Show(grab, grabTime);
    void OnBuilt() => Show(success, successTime);
    void OnFumbled() => Show(fumble, fumbleTime);
    void OnGameEnded() { over = true; sr.sprite = sad; }

    void Show(Sprite s, float time)
    {
        if (over) return;
        sr.sprite = s;
        poseUntil = Time.time + time;
    }

    void Update()
    {
        float px = 1f / pixelsPerUnit;
        Vector3 move = Vector3.zero;
        bool posing = Time.time < poseUntil;

        if (!over && !posing && sr.sprite != idle) sr.sprite = idle;

        if (over)
            move.y = -px;
        else if (posing && sr.sprite == grab)
            move.y = -px;
        else if (posing && sr.sprite == success)
            move.y = 2f * px;
        else if (posing && sr.sprite == fumble)
            move.x = (Mathf.Repeat(Time.time * 20f, 2f) < 1f ? -1f : 1f) * px;
        else
            move.y = Mathf.Repeat(Time.time * bobRate, 1f) < 0.5f ? 0f : px;

        transform.localPosition = basePos + move;
    }
}