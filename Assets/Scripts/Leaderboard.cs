using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RecordEntry
{
    public string name;
    public int score;
}

public static class Leaderboard
{
    public static int Size = 10;
    const string Key = "RivetRush.Records";

    [Serializable]
    class RecordList
    {
        public List<RecordEntry> entries = new();
    }

    public static List<RecordEntry> Load()
    {
        string json = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(json)) return new List<RecordEntry>();
        RecordList list = JsonUtility.FromJson<RecordList>(json);
        return list?.entries ?? new List<RecordEntry>();
    }

    public static bool Qualifies(int score)
    {
        if (score <= 0) return false;
        List<RecordEntry> list = Load();
        return list.Count < Size || score > list[list.Count - 1].score;
    }

    public static int Add(string name, int score)
    {
        List<RecordEntry> list = Load();
        var entry = new RecordEntry { name = name, score = score };
        list.Add(entry);
        list.Sort((a, b) => b.score.CompareTo(a.score));
        if (list.Count > Size) list.RemoveRange(Size, list.Count - Size);

        PlayerPrefs.SetString(Key, JsonUtility.ToJson(new RecordList { entries = list }));
        PlayerPrefs.Save();
        return list.IndexOf(entry);
    }

    public static int Best
    {
        get
        {
            List<RecordEntry> list = Load();
            return list.Count > 0 ? list[0].score : 0;
        }
    }
}