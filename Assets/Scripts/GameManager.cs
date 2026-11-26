using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] Belt belt;
    [SerializeField] BlueprintBoard board;
    [SerializeField] Difficulty difficulty;

    [Header("Rules")]
    [SerializeField] int pointsPerChip = 10;

    [Header("Streak")]
    [SerializeField] int[] streakThresholds = { 5, 10, 20 };
    [SerializeField] int[] streakMultipliers = { 2, 3, 5 };

    [Header("UI")]
    [SerializeField] TMP_Text hudText;

    public static bool AutoStart;

    public bool IsPlaying { get; private set; }
    public bool HasStarted { get; private set; }
    public bool IsPaused { get; private set; }
    public int StartFrame { get; private set; } = -1;
    public int Score => score;

    public event System.Action GameEnded;
    public event System.Action<int, int> ScoreGained;
    public event System.Action<int> StreakBroken;

    int score, streak, built;

    void Awake()
    {
        Time.timeScale = 1f;
        belt.OnJam += GameOver;
    }

    void Start() => UpdateHud();

    public void StartGame()
    {
        if (HasStarted) return;
        HasStarted = true;
        StartFrame = Time.frameCount;

        difficulty.Begin();
        board.Init();
        belt.Begin();

        IsPlaying = true;
        UpdateHud();
    }

    public void Restart()
    {
        AutoStart = true;
        Reload();
    }

    public void ToMenu()
    {
        AutoStart = false;
        Reload();
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
    }

    void Reload()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BlueprintBuilt(int length)
    {
        built++;
        streak++;
        int multiplier = MultiplierFor(streak);
        int gained = length * pointsPerChip * multiplier;
        score += gained;

        int bonusReached = 0;
        for (int i = 0; i < streakThresholds.Length; i++)
            if (streak == streakThresholds[i]) bonusReached = multiplier;

        ScoreGained?.Invoke(gained, bonusReached);
        difficulty.OnBlueprintsBuilt(built);
        UpdateHud();
    }

    public void BreakStreak()
    {
        if (MultiplierFor(streak) > 1) StreakBroken?.Invoke(streak);
        streak = 0;
        UpdateHud();
    }

    int MultiplierFor(int s)
    {
        int m = 1;
        for (int i = 0; i < streakThresholds.Length && i < streakMultipliers.Length; i++)
            if (s >= streakThresholds[i]) m = streakMultipliers[i];
        return m;
    }

    public void GameOver()
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        belt.Stop();
        GameEnded?.Invoke();
        UpdateHud();
    }

    void UpdateHud()
    {
        if (hudText) hudText.text = $"Score:\n{score:D5}";
    }
}