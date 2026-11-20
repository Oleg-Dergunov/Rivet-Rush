using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class FloatingText : MonoBehaviour
{
    [Tooltip("Canvas pixels")]
    [SerializeField] float rise = 24f;
    [SerializeField] float lifetime = 1f;
    [Tooltip("Fraction of the lifetime before fading starts")]
    [SerializeField, Range(0f, 1f)] float fadeFrom = 0.6f;

    TextMeshProUGUI tmp;
    RectTransform rt;
    Vector2 start;
    float delay;
    float t;

    public void Init(string text, Color color, Vector2 position, float startDelay)
    {
        tmp = GetComponent<TextMeshProUGUI>();
        rt = (RectTransform)transform;
        tmp.text = text;
        tmp.color = color;
        start = position;
        rt.anchoredPosition = start;
        delay = startDelay;
        tmp.alpha = delay > 0f ? 0f : 1f;
    }

    void Update()
    {
        if (delay > 0f)
        {
            delay -= Time.deltaTime;
            if (delay > 0f) return;
            tmp.alpha = 1f;
        }

        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / lifetime);
        float eased = 1f - (1f - k) * (1f - k);

        float y = Mathf.Round(start.y + rise * eased);
        rt.anchoredPosition = new Vector2(Mathf.Round(start.x), y);

        tmp.alpha = k < fadeFrom ? 1f : 1f - (k - fadeFrom) / (1f - fadeFrom);
        if (k >= 1f) Destroy(gameObject);
    }
}