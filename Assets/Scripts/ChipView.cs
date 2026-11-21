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

    public void Vanish(Vector3 drift, float duration) => StartCoroutine(VanishRoutine(drift, duration));

    IEnumerator VanishRoutine(Vector3 drift, float duration)
    {
        Vector3 start = transform.position;
        float startAlpha = sr.color.a;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            Vector3 p = start + drift * (k * k);
            p.x = Mathf.Round(p.x * PixelsPerUnit) / PixelsPerUnit;
            p.y = Mathf.Round(p.y * PixelsPerUnit) / PixelsPerUnit;
            transform.position = p;
            SetAlpha(startAlpha * (1f - k));
            yield return null;
        }
        Destroy(gameObject);
    }
}