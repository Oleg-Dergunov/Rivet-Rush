using UnityEngine;
using UnityEngine.UI;

public class GameSounds : MonoBehaviour
{
    [SerializeField] GameManager game;
    [SerializeField] RivetController rivet;
    [SerializeField] Belt belt;
    [SerializeField] Difficulty difficulty;

    [Header("Sound names, as in AudioManager (empty = no sound)")]
    [SerializeField] string grab = "Grab";
    [SerializeField] string build = "Build";
    [SerializeField] string bonus = "Bonus";
    [SerializeField] string fumble = "Fumble";
    [SerializeField] string streakBroken = "StreakBroken";
    [SerializeField] string burnt = "Burnt";
    [SerializeField] string dispense = "Dispense";
    [SerializeField] string levelUp = "LevelUp";
    [SerializeField] string gameOver = "GameOver";
    [SerializeField] string newRecord = "NewRecord";
    [SerializeField] string click = "Click";

    void Awake()
    {
        foreach (Button b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            b.onClick.AddListener(OnClick);
    }

    void OnEnable()
    {
        rivet.PickedUp += OnGrab;
        rivet.Built += OnBuild;
        rivet.Fumbled += OnFumble;
        rivet.BurntDestroyed += OnBurnt;
        game.ScoreGained += OnScore;
        game.StreakBroken += OnStreakBroken;
        game.GameEnded += OnGameOver;
        belt.Dispensed += OnDispense;
        difficulty.LevelStarted += OnLevelUp;
    }

    void OnDisable()
    {
        rivet.PickedUp -= OnGrab;
        rivet.Built -= OnBuild;
        rivet.Fumbled -= OnFumble;
        rivet.BurntDestroyed -= OnBurnt;
        game.ScoreGained -= OnScore;
        game.StreakBroken -= OnStreakBroken;
        game.GameEnded -= OnGameOver;
        belt.Dispensed -= OnDispense;
        difficulty.LevelStarted -= OnLevelUp;
    }

    void OnGrab() => Play(grab);
    void OnBuild() => Play(build);
    void OnFumble() => Play(fumble);
    void OnBurnt() => Play(burnt);
    void OnStreakBroken() => Play(streakBroken);
    void OnGameOver() => Play(Leaderboard.Qualifies(game.Score) ? newRecord : gameOver);
    void OnDispense() => Play(dispense);
    void OnLevelUp(string announce) => Play(levelUp);
    void OnClick() => Play(click);

    void OnScore(int points, int bonusReached)
    {
        if (bonusReached > 0) Play(bonus);
    }

    static void Play(string name)
    {
        if (!string.IsNullOrEmpty(name)) AudioManager.PlaySound(name);
    }

}