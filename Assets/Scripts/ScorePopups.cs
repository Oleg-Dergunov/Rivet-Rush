using UnityEngine;

public class ScorePopups : MonoBehaviour
{
    [SerializeField] GameManager game;
    [SerializeField] FloatingText prefab;
    [Tooltip("Full-screen UI object inside the Canvas that will hold the popups")]
    [SerializeField] RectTransform container;
    [Tooltip("Point in the game world the popups rise from (e.g. above the tray)")]
    [SerializeField] Transform worldTarget;

    [Header("Points")]
    [SerializeField] Color pointsColor = new Color32(0xE9, 0x8B, 0x3A, 0xFF);
    [Tooltip("{0} is replaced with the multiplier")]
    [SerializeField] string bonusFormat = "Streak bonus *{0}!";

    [Header("Streak broken")]
    [SerializeField] string brokenText = "Streak broken";
    [SerializeField] Color brokenColor = new Color32(0xB1, 0x3E, 0x53, 0xFF);

    Camera cam;

    void Awake() => cam = container.GetComponentInParent<Canvas>().worldCamera;

    void OnEnable()
    {
        game.ScoreGained += ShowPoints;
        game.StreakBroken += ShowBroken;
    }

    void OnDisable()
    {
        game.ScoreGained -= ShowPoints;
        game.StreakBroken -= ShowBroken;
    }

    void ShowPoints(int points, int bonusReached)
    {
        string text = $"+{points}";
        if (bonusReached > 0) text += "\n" + string.Format(bonusFormat, bonusReached);
        Spawn(text, pointsColor);
    }

    void ShowBroken(int lostStreak) => Spawn(brokenText, brokenColor);

    void Spawn(string text, Color color)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldTarget.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screen, cam, out Vector2 local);

        FloatingText ft = Instantiate(prefab, container);
        ft.Init(text, color, local, 0f);
    }
}