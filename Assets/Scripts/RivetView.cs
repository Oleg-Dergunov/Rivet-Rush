using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class RivetView : MonoBehaviour
{
    public enum Mood { Idle, Read, Wave, Crying, Celebrate }

    [SerializeField] RivetController rivet;
    [SerializeField] GameManager game;

    [Header("Poses")]
    [SerializeField] Sprite grab;
    [SerializeField] Sprite success;
    [SerializeField] Sprite fumble;

    [Header("Animation clips")]
    [SerializeField] AnimationClip idleClip;
    [SerializeField] AnimationClip readClip;
    [SerializeField] AnimationClip waveClip;
    [SerializeField] AnimationClip cryingClip;
    [SerializeField] AnimationClip celebrateClip;

    [Header("Timing (seconds)")]
    [SerializeField] float grabTime = 0.15f;
    [SerializeField] float successTime = 0.6f;
    [SerializeField] float fumbleTime = 0.6f;

    [Header("Motion")]
    [SerializeField] float pixelsPerUnit = 16f;

    [Header("Menus")]
    [SerializeField] Vector3 asideOffset = new Vector3(-5f, 0f, 0f);
    [SerializeField] float asideTime = 0.3f;

    SpriteRenderer sr;
    Animator animator;
    PlayableGraph graph;
    AnimationClip playing;
    Sprite baseSprite;
    Vector3 basePos;
    float poseUntil;
    Sprite queued;
    float queuedTime;
    bool over;
    bool aside;
    float asideT;
    Mood mood = Mood.Idle;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        baseSprite = sr.sprite;
        basePos = transform.localPosition;
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

    void OnDestroy() => StopClip();

    public void SetAside(bool value, bool instant = false)
    {
        aside = value;
        if (instant) asideT = value ? 1f : 0f;
    }

    public void SetMood(Mood value) => mood = value;

    void OnPickedUp() => Show(grab, grabTime);
    void OnBuilt() => ShowThen(grab, grabTime, success, successTime);
    void OnFumbled() => ShowThen(grab, grabTime, fumble, fumbleTime);

    void OnGameEnded()
    {
        over = true;
        queued = null;
        poseUntil = 0f;
    }

    void Show(Sprite s, float time)
    {
        if (over) return;
        StopClip();
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

        if (!posing && queued != null)
        {
            sr.sprite = queued;
            poseUntil = Time.time + queuedTime;
            queued = null;
            posing = true;
        }

        if (!posing) Play(ClipFor(mood));

        Vector3 move = Vector3.zero;
        if (posing && sr.sprite == grab)
            move.y = -px;
        else if (posing && sr.sprite == success)
            move.y = 2f * px;
        else if (posing && sr.sprite == fumble)
            move.x = (Mathf.Repeat(Time.time * 20f, 2f) < 1f ? -1f : 1f) * px;

        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
        asideT = Mathf.MoveTowards(asideT, aside ? 1f : 0f, dt / asideTime);
        float e = asideT * asideT * (3f - 2f * asideT);
        Vector3 p = basePos + asideOffset * e + move;
        p.x = Mathf.Round(p.x * pixelsPerUnit) / pixelsPerUnit;
        p.y = Mathf.Round(p.y * pixelsPerUnit) / pixelsPerUnit;
        transform.localPosition = p;
    }

    AnimationClip ClipFor(Mood m) => m switch
    {
        Mood.Read => readClip,
        Mood.Wave => waveClip,
        Mood.Crying => cryingClip,
        Mood.Celebrate => celebrateClip,
        _ => idleClip
    };

    void Play(AnimationClip clip)
    {
        if (clip == null)
        {
            StopClip();
            sr.sprite = baseSprite;
            return;
        }

        if (playing == clip && graph.IsValid()) return;

        StopClip();
        AnimationPlayableUtilities.PlayClip(animator, clip, out graph);
        graph.SetTimeUpdateMode(DirectorUpdateMode.UnscaledGameTime);
        playing = clip;
    }

    void StopClip()
    {
        if (graph.IsValid()) graph.Destroy();
        playing = null;
    }
}