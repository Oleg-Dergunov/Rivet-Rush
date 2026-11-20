using System.Collections.Generic;
using UnityEngine;

public class BlueprintBoard : MonoBehaviour
{
    [Header("Rules")]
    [Tooltip("Capped at panels x 2")]
    public int blueprintCount = 2;
    public int minLength = 3;
    public int maxLength = 3;
    [HideInInspector] public int colorsInPlay = 3;

    [Header("Display")]
    [Tooltip("Board sprites in order (left board first). Each holds two blueprints.")]
    [SerializeField] Transform[] panels;
    [SerializeField] ChipView iconPrefab;
    [Tooltip("Local position of the first chip in the top row, relative to its panel")]
    [SerializeField] Vector2 firstCell = new Vector2(-2f, 0.625f);
    [SerializeField] float cellSpacing = 1f;
    [SerializeField] float rowSpacing = 1.1875f;
    [Tooltip("Alpha of cells already filled by the tray")]
    [SerializeField, Range(0f, 1f)] float doneAlpha = 0.3f;

    readonly List<List<ChipColor>> blueprints = new();
    readonly List<GameObject> rows = new();
    IReadOnlyList<ChipData> lastTray;
    bool initialized;

    int Capacity => panels.Length * 2;

    public void Init()
    {
        blueprints.Clear();
        int count = Mathf.Min(blueprintCount, Capacity);
        for (int i = 0; i < count; i++)
            blueprints.Add(Generate(i));
        initialized = true;
        Redraw(null);
    }

    public void SetCount(int count)
    {
        blueprintCount = count;
        if (!initialized) return;

        count = Mathf.Min(count, Capacity);
        if (blueprints.Count >= count) return;
        while (blueprints.Count < count)
            blueprints.Add(Generate(blueprints.Count));
        Redraw(lastTray);
    }

    public bool Accepts(IReadOnlyList<ChipData> tray, ChipData next)
    {
        foreach (var bp in blueprints)
            if (IsPrefix(bp, tray, next)) return true;
        return false;
    }

    public int FindCompleted(IReadOnlyList<ChipData> tray)
    {
        for (int b = 0; b < blueprints.Count; b++)
            if (blueprints[b].Count == tray.Count && IsPrefix(blueprints[b], tray, null))
                return b;
        return -1;
    }

    public int Complete(int index)
    {
        int length = blueprints[index].Count;
        blueprints[index] = Generate(index);
        return length;
    }

    public void Redraw(IReadOnlyList<ChipData> tray)
    {
        lastTray = tray;
        foreach (var r in rows) Destroy(r);
        rows.Clear();

        int filled = tray?.Count ?? 0;
        for (int b = 0; b < blueprints.Count; b++)
        {
            var row = new GameObject($"Blueprint {b}");
            row.transform.SetParent(panels[b / 2], false);
            row.transform.localPosition = new Vector3(firstCell.x, firstCell.y - (b % 2) * rowSpacing, 0f);
            rows.Add(row);

            bool onTrack = filled > 0 && IsPrefix(blueprints[b], tray, null);
            for (int k = 0; k < blueprints[b].Count; k++)
            {
                ChipView icon = Instantiate(iconPrefab, row.transform);
                icon.transform.localPosition = new Vector3(k * cellSpacing, 0f, 0f);
                icon.Set(ChipData.Normal(blueprints[b][k]));
                if (onTrack && k < filled) icon.SetAlpha(doneAlpha);
            }
        }
    }

    static bool IsPrefix(List<ChipColor> bp, IReadOnlyList<ChipData> tray, ChipData? next)
    {
        int count = tray.Count + (next.HasValue ? 1 : 0);
        if (count > bp.Count) return false;
        for (int k = 0; k < tray.Count; k++)
            if (!tray[k].Fits(bp[k])) return false;
        return !next.HasValue || next.Value.Fits(bp[tray.Count]);
    }

    List<ChipColor> Generate(int replacingIndex)
    {
        int length = Random.Range(minLength, maxLength + 1);
        var bp = new List<ChipColor>(length);

        for (int c = 0; c < colorsInPlay && bp.Count < length; c++)
            if (!UsedElsewhere((ChipColor)c, replacingIndex)) bp.Add((ChipColor)c);

        while (bp.Count < length)
            bp.Add((ChipColor)Random.Range(0, colorsInPlay));

        for (int i = bp.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (bp[i], bp[j]) = (bp[j], bp[i]);
        }
        return bp;
    }

    bool UsedElsewhere(ChipColor c, int skipIndex)
    {
        for (int b = 0; b < blueprints.Count; b++)
            if (b != skipIndex && blueprints[b].Contains(c)) return true;
        return false;
    }
}