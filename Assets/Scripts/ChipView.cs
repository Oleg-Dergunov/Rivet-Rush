using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ChipView : MonoBehaviour
{
    [Tooltip("Optional: one sprite per ChipColor, in enum order (Blue, Green, Red, Yellow). " +
             "Leave empty to tint the default sprite instead.")]
    [SerializeField] Sprite[] colorSprites;
    [SerializeField] Sprite burntSprite;
    [SerializeField] Sprite rainbowSprite;

    SpriteRenderer sr;
    Sprite defaultSprite;
    bool isOnTop;

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
}