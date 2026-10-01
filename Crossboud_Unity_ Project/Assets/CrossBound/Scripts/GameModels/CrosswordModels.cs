using System;
using System.Collections.Generic;

[Serializable]
public class CrosswordQuestion
{
    public string id;
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
    public int gridWidth = 15;
    public int gridHeight = 15;
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