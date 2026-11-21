using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Menus : MonoBehaviour
{
    [SerializeField] GameManager game;
    [SerializeField] RivetView rivetView;
    [Tooltip("Score, Menu and How to play buttons: hidden while any panel is open")]
    [SerializeField] GameObject hud;

    [Header("Panels")]
    [SerializeField] GameObject mainPanel;
    [SerializeField] GameObject recordsPanel;
    [SerializeField] GameObject howToPanel;
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] GameObject newRecordPanel;

    [Header("Main")]
    [Tooltip("Hidden in the web build, where quitting does nothing")]
    [SerializeField] GameObject exitButton;

    [Header("Records")]
    [SerializeField, Range(1, 10)] int recordsCount = 10;
    [SerializeField] TMP_Text recordNames;
    [SerializeField] TMP_Text recordScores;

    [Header("How to play")]
    [Tooltip("Optional: scrolled back to the top every time the panel opens")]
    [SerializeField] ScrollRect howToScroll;

    [Header("Game over")]
    [SerializeField] TMP_Text gameOverText;
    [Tooltip("Ignore presses for this long after the panel opens, so the fatal press doesn't restart instantly")]
    [SerializeField] float restartDelay = 0.75f;

    [Header("New record")]
    [SerializeField] TMP_Text newRecordText;
    [SerializeField] TMP_InputField nameInput;
    [SerializeField] int maxNameLength = 8;
    [SerializeField] string defaultName = "RIVET";

    GameObject[] allPanels;
    GameObject current;
    GameObject howToReturn;
    bool howToPausedGame;
    float gameOverShownAt;

    void Awake()
    {
        Leaderboard.Size = recordsCount;
        allPanels = new[] { mainPanel, recordsPanel, howToPanel, gameOverPanel, newRecordPanel };
        game.GameEnded += OnGameEnded;

        if (nameInput)
        {
            nameInput.characterLimit = maxNameLength;
            nameInput.onSubmit.AddListener(_ => SaveAndRetry());
        }
        if (exitButton && Application.platform == RuntimePlatform.WebGLPlayer)
            exitButton.SetActive(false);
    }

    void Start()
    {
        if (GameManager.AutoStart)
        {
            GameManager.AutoStart = false;
            rivetView.SetAside(false, true);
            Play();
        }
        else
        {
            ShowMain();
        }
    }

    void Update()
    {
        if (!OneButton.Pressed) return;

        if (current == mainPanel) Play();
        else if (current == gameOverPanel && Time.time - gameOverShownAt > restartDelay) Retry();
    }

    void Show(GameObject panel)
    {
        current = panel;
        foreach (var p in allPanels)
            if (p) p.SetActive(p == panel);
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        rivetView.SetAside(panel != null);
        if (hud) hud.SetActive(panel == null);
    }

    public void Play()
    {
        if (game.HasStarted)
        {
            game.Restart();
            return;
        }
        Show(null);
        game.StartGame();
    }

    public void ShowMain() => Show(mainPanel);

    public void ShowRecords()
    {
        FillRecords();
        Show(recordsPanel);
    }

    public void ShowHowTo()
    {
        if (current == howToPanel) return;

        howToPausedGame = game.IsPlaying && !game.IsPaused;
        howToReturn = current;
        if (howToPausedGame) game.SetPaused(true);

        Show(howToPanel);
        if (howToScroll) howToScroll.verticalNormalizedPosition = 1f;
    }

    public void CloseHowTo()
    {
        if (howToPausedGame)
        {
            howToPausedGame = false;
            game.SetPaused(false);
            Show(null);
        }
        else
        {
            Show(howToReturn);
            if (howToReturn == gameOverPanel) gameOverShownAt = Time.time;
            if (howToReturn == newRecordPanel) FocusNameInput();
        }
    }

    void FillRecords()
    {
        List<RecordEntry> list = Leaderboard.Load();
        var names = new StringBuilder();
        var scores = new StringBuilder();

        for (int i = 0; i < recordsCount; i++)
        {
            bool has = i < list.Count;
            names.AppendLine($"{i + 1,2}. {(has ? list[i].name : "---")}");
            scores.AppendLine(has ? list[i].score.ToString("D5") : "-----");
        }

        if (recordNames) recordNames.text = names.ToString();
        if (recordScores) recordScores.text = scores.ToString();
    }

    void OnGameEnded()
    {
        if (Leaderboard.Qualifies(game.Score))
        {
            if (newRecordText) newRecordText.text = $"Your score:\n{game.Score:D5}\n\nEnter nickname:";
            Show(newRecordPanel);
            nameInput.text = "";
            FocusNameInput();
        }
        else
        {
            if (gameOverText)
                gameOverText.text = $"Your score:\n{game.Score:D5}\n\nBest score:\n{Leaderboard.Best:D5}";
            Show(gameOverPanel);
            gameOverShownAt = Time.time;
        }
    }

    void FocusNameInput()
    {
        nameInput.Select();
        nameInput.ActivateInputField();
    }

    public void SaveAndRetry()
    {
        if (current != newRecordPanel) return;
        string name = nameInput.text.Trim().ToUpperInvariant();
        if (name.Length == 0) name = defaultName;
        Leaderboard.Add(name, game.Score);
        game.Restart();
    }

    public void Retry() => game.Restart();
    public void ToMenu() => game.ToMenu();
    public void Exit() => Application.Quit();
}