using UnityEngine;

public enum ChipColor { Blue, Green, Red, Yellow }
public enum ChipKind { Normal, Burnt, Rainbow }

[System.Serializable]
public struct ChipData
{
    public ChipKind kind;
    public ChipColor color;

    public ChipData(ChipKind kind, ChipColor color = ChipColor.Blue)
    {
        this.kind = kind;
        this.color = color;
    }

    public static ChipData Normal(ChipColor c) => new ChipData(ChipKind.Normal, c);

    public bool Fits(ChipColor needed) =>
        kind == ChipKind.Rainbow || (kind == ChipKind.Normal && color == needed);
}

public static class ChipPalette
{
    public static Color Get(ChipData d) => d.kind switch
    {
        ChipKind.Burnt => new Color(0.22f, 0.18f, 0.18f),
        ChipKind.Rainbow => Color.white,
        _ => Get(d.color)
    };

    public static Color Get(ChipColor c) => c switch
    {
        ChipColor.Blue => new Color(0.25f, 0.55f, 1f),
        ChipColor.Green => new Color(0.3f, 0.85f, 0.4f),
        ChipColor.Red => new Color(1f, 0.3f, 0.3f),
        _ => new Color(1f, 0.85f, 0.2f)
    };
}