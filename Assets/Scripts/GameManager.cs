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
    [Tooltip("Ignore presses for this long after game over, so the fatal press doesn't restart instantly")]
    [SerializeField] float restartDelay = 0.75f;

    [Header("Streak")]
    [Tooltip("Streak length where each multiplier starts, lowest first")]
    [SerializeField] int[] streakThresholds = { 5, 10, 20 };
    [Tooltip("Multiplier for each threshold above, same order")]
    [SerializeField] int[] streakMultipliers = { 2, 3, 5 };
    [Tooltip("Only show 'Streak broken' if the lost streak was at least this long")]
    [SerializeField] int streakBrokenMin = 1;

    [Header("UI")]
    [SerializeField] TMP_Text hudText;
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] TMP_Text gameOverText;

    public bool IsPlaying { get; private set; }
    public event System.Action GameEnded;
    public event System.Action<int, int> ScoreGained;
    public event System.Action<int> StreakBroken;

    const string BestKey = "RivetRush.Best";
    int score, streak, best, built;
    float gameOverTime;

    void Start()
    {
        best = PlayerPrefs.GetInt(BestKey, 0);
        belt.OnJam += GameOver;

        difficulty.Begin();
        board.Init();
        belt.Begin();

        if (gameOverPanel) gameOverPanel.SetActive(false);
        IsPlaying = true;
        UpdateHud();
    }

    void Update()
    {
        if (!IsPlaying && Time.time - gameOverTime > restartDelay && OneButton.Pressed)
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
        if (streak >= streakBrokenMin) StreakBroken?.Invoke(streak);
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
        gameOverTime = Time.time;
        belt.Stop();

        bool newBest = score > best;
        if (newBest)
        {
            best = score;
            PlayerPrefs.SetInt(BestKey, best);
            PlayerPrefs.Save();
        }

        GameEnded?.Invoke();
        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (gameOverText)
            gameOverText.text = $"BELT JAMMED\n\nScore {score}\nBest {best}{(newBest ? "  NEW!" : "")}\n\nPress to try again";
        UpdateHud();
    }

    void UpdateHud()
    {
        if (hudText) hudText.text = $"Score:\n{score:D5}";
    }
}