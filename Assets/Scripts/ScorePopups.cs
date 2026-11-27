using UnityEngine;

public class ScorePopups : MonoBehaviour
{
    [SerializeField] GameManager game;
    [SerializeField] Difficulty difficulty;
    [SerializeField] FloatingText prefab;
    [SerializeField] RectTransform container;
    [SerializeField] Transform worldTarget;

    [Header("Points")]
    [SerializeField] Color pointsColor = new Color32(0xE9, 0x8B, 0x3A, 0xFF);
    [SerializeField] string bonusFormat = "Streak bonus *{0}!";

    [Header("Streak broken")]
    [SerializeField] string brokenText = "Streak broken";
    [SerializeField] Color brokenColor = new Color32(0xB1, 0x3E, 0x53, 0xFF);

    [Header("Level up")]
    [SerializeField] Color levelColor = new Color32(0x73, 0xEF, 0xF7, 0xFF);
    [SerializeField] float levelDelay = 0.6f;

    Camera cam;

    void Awake() => cam = container.GetComponentInParent<Canvas>().worldCamera;

    void OnEnable()
    {
        game.ScoreGained += ShowPoints;
        game.StreakBroken += ShowBroken;
        if (difficulty) difficulty.LevelStarted += ShowLevel;
    }

    void OnDisable()
    {
        game.ScoreGained -= ShowPoints;
        game.StreakBroken -= ShowBroken;
        if (difficulty) difficulty.LevelStarted -= ShowLevel;
    }

    void ShowPoints(int points, int bonusReached)
    {
        string text = $"+{points}";
        if (bonusReached > 0) text = string.Format(bonusFormat, bonusReached) + "\n" + text;
        Spawn(text, pointsColor, 0f);
    }

    void ShowBroken() => Spawn(brokenText, brokenColor, 0f);

    void ShowLevel(string announce) => Spawn(announce, levelColor, levelDelay);

    void Spawn(string text, Color color, float delay)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldTarget.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screen, cam, out Vector2 local);

        FloatingText ft = Instantiate(prefab, container);
        ft.Init(text, color, local, delay);
    }
}