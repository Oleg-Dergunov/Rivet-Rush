using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ChipView : MonoBehaviour
{
    [SerializeField] Sprite[] colorSprites;
    [SerializeField] Sprite burntSprite;
    [SerializeField] Sprite rainbowSprite;

    SpriteRenderer sr;
    Sprite defaultSprite;
    bool isOnTop;

    const float PixelsPerUnit = 16f;

    public ChipData Data { get; private set; }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        defaultSprite = sr.sprite;
    }

    public void Set(ChipData d)
    {
        Data = d;
        Sprite s = d.kind switch
        {
            ChipKind.Burnt => burntSprite,
            ChipKind.Rainbow => rainbowSprite,
            _ => colorSprites != null && (int)d.color < colorSprites.Length ? colorSprites[(int)d.color] : null
        };
        sr.sprite = s != null ? s : defaultSprite;
        sr.color = s != null ? Color.white : ChipPalette.Get(d);
    }

    public void SetOnTop(bool onTop)
    {
        if (onTop && !isOnTop) sr.sortingOrder++;
        else if (!onTop && isOnTop) sr.sortingOrder--;
        isOnTop = onTop;
    }

    public void SetAlpha(float a)
    {
        Color c = sr.color;
        c.a = a;
        sr.color = c;
    }

    public void FlyFrom(Vector3 worldFrom, float duration, float arc)
    {
        Vector3 localTarget = transform.localPosition;
        transform.position = worldFrom;
        StartCoroutine(FlyRoutine(worldFrom, localTarget, duration, arc));
    }

    IEnumerator FlyRoutine(Vector3 from, Vector3 localTarget, float duration, float arc)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float e = 1f - (1f - k) * (1f - k);
            Vector3 to = transform.parent.TransformPoint(localTarget);
            Vector3 p = Vector3.Lerp(from, to, e);
            p.y += arc * 4f * k * (1f - k);
            transform.position = Snap(p);
            yield return null;
        }
        transform.localPosition = localTarget;
    }

    public void Vanish(Vector3 drift, float duration, float delay = 0f) =>
        StartCoroutine(VanishRoutine(drift, duration, delay));

    IEnumerator VanishRoutine(Vector3 drift, float duration, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        Vector3 start = transform.position;
        float startAlpha = sr.color.a;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            transform.position = Snap(start + drift * (k * k));
            SetAlpha(startAlpha * (1f - k));
            yield return null;
        }
        Destroy(gameObject);
    }

    static Vector3 Snap(Vector3 p)
    {
        p.x = Mathf.Round(p.x * PixelsPerUnit) / PixelsPerUnit;
        p.y = Mathf.Round(p.y * PixelsPerUnit) / PixelsPerUnit;
        return p;
    }
}