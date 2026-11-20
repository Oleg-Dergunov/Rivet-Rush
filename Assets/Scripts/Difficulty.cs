using System;
using UnityEngine;

[Serializable]
public class DifficultyLevel
{
    public string announce = "";
    public int blueprintsNeeded;

    [Header("Belt")]
    public float beltSpeed = 2f;
    public float spawnInterval = 1.6f;
    [Range(0f, 1f)] public float burntChance;
    [Range(0f, 1f)] public float rainbowChance;

    [Header("Blueprints")]
    [Range(1, 4)] public int blueprintCount = 2;
    [Range(3, 5)] public int minLength = 3;
    [Range(3, 5)] public int maxLength = 3;
    [Range(2, 4)] public int colorsInPlay = 3;

    public DifficultyLevel() { }

    public DifficultyLevel(int needed, float speed, float interval, int count, int minLen, int maxLen,
                           int colors, float burnt, float rainbow, string text)
    {
        blueprintsNeeded = needed;
        beltSpeed = speed;
        spawnInterval = interval;
        blueprintCount = count;
        minLength = minLen;
        maxLength = maxLen;
        colorsInPlay = colors;
        burntChance = burnt;
        rainbowChance = rainbow;
        announce = text;
    }
}

public class Difficulty : MonoBehaviour
{
    [SerializeField] Belt belt;
    [SerializeField] BlueprintBoard board;

    [SerializeField]
    DifficultyLevel[] levels =
{
        new DifficultyLevel(0,  1.5f,  2.8f, 2, 3, 3, 3, 0f,    0f,    ""),
        new DifficultyLevel(3,  1.7f,  2.6f, 2, 3, 3, 3, 0f,    0f,    "Faster!"),
        new DifficultyLevel(6,  1.95f, 2.4f, 2, 3, 3, 3, 0.08f, 0f,    "Burnt chips!"),
        new DifficultyLevel(10, 2.15f, 2.2f, 3, 3, 4, 3, 0.08f, 0f,    "New blueprint!"),
        new DifficultyLevel(15, 2.4f,  2.0f, 3, 3, 4, 3, 0.10f, 0.06f, "Rainbow chips!"),
        new DifficultyLevel(20, 2.6f,  1.8f, 3, 3, 4, 4, 0.10f, 0.06f, "New colour!"),
        new DifficultyLevel(26, 2.85f, 1.6f, 4, 4, 5, 4, 0.12f, 0.07f, "Bigger blueprints!"),
        new DifficultyLevel(33, 3.05f, 1.4f, 4, 4, 5, 4, 0.12f, 0.07f, "Faster!"),
        new DifficultyLevel(40, 3.3f,  1.2f, 4, 4, 5, 4, 0.14f, 0.08f, "Faster!"),
    };

    public event Action<string> LevelStarted;
    public int Level { get; private set; } = -1;

    public void Begin() => Apply(0);

    public void OnBlueprintsBuilt(int total)
    {
        int next = Level;
        while (next + 1 < levels.Length && total >= levels[next + 1].blueprintsNeeded) next++;
        if (next == Level) return;

        Apply(next);
        if (!string.IsNullOrEmpty(levels[next].announce))
            LevelStarted?.Invoke(levels[next].announce);
    }

    void Apply(int i)
    {
        Level = i;
        DifficultyLevel l = levels[i];

        belt.speed = l.beltSpeed;
        belt.spawnInterval = l.spawnInterval;
        belt.burntChance = l.burntChance;
        belt.rainbowChance = l.rainbowChance;
        belt.colorsInPlay = l.colorsInPlay;

        board.colorsInPlay = l.colorsInPlay;
        board.minLength = l.minLength;
        board.maxLength = Mathf.Max(l.minLength, l.maxLength);
        board.SetCount(l.blueprintCount);
    }
}