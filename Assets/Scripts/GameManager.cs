using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] Belt belt;
    [SerializeField] BlueprintBoard board;

    [Header("Rules")]
    [SerializeField, Range(2, 4)] int colorsInPlay = 3;
    [SerializeField] int pointsPerChip = 10;
    [SerializeField] float streakBonus = 0.25f;
    [SerializeField] float restartDelay = 0.75f;

    [Header("UI")]
    [SerializeField] TMP_Text hudText;
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] TMP_Text gameOverText;

    public bool IsPlaying { get; private set; }
    public event System.Action GameEnded;

    const string BestKey = "RivetRush.Best";
    int score, streak, best;
    float gameOverTime;

    void Start()
    {
        best = PlayerPrefs.GetInt(BestKey, 0);

        belt.colorsInPlay = colorsInPlay;
        board.colorsInPlay = colorsInPlay;
        belt.OnJam += GameOver;

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
        streak++;
        float multiplier = 1f + streakBonus * (streak - 1);
        score += Mathf.RoundToInt(length * pointsPerChip * multiplier);
        UpdateHud();
    }

    public void BreakStreak()
    {
        streak = 0;
        UpdateHud();
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
        if (hudText) hudText.text = $"Score {score}\nStreak {streak}\nBest {best}";
    }
}