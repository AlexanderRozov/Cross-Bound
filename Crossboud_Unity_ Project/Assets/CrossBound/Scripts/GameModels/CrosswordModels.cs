using System;
using System.Collections.Generic;

[Serializable]
public class CrosswordQuestion
{
    public string id;
    public int number;
    // The editor generator serializes Across = 0 and Down = 1.
    public int direction;
    public string question;
    public string answer;
    public int startX;
    public int startY;
    public bool isHorizontal;
}

[Serializable]
public class CrosswordData
{
    public List<CrosswordQuestion> questions = new();
    // Editor generator output uses `entries`; keeping it here makes generated JSON
    // directly playable without a conversion step.
    public List<CrosswordQuestion> entries = new();
    public int gridWidth = 15;
    public int gridHeight = 15;
    public int width;
    public int height;

    public IReadOnlyList<CrosswordQuestion> Questions => questions != null && questions.Count > 0 ? questions : entries ??= new List<CrosswordQuestion>();

    public void NormalizeGeneratedLayout()
    {
        if (width > 0) gridWidth = width;
        if (height > 0) gridHeight = height;
        foreach (CrosswordQuestion question in Questions)
            if (entries != null && entries.Contains(question)) question.isHorizontal = question.direction == 0;
    }
}

[Serializable]
public class PlayerProfile
{
    public string playerName = "Player";
    public int currentLevel = 0;
    public int completedPuzzles = 0;
    public int totalScore = 0;
    public List<string> completedPuzzleIds = new();
    public DateTime lastPlayed = DateTime.Now;
}
