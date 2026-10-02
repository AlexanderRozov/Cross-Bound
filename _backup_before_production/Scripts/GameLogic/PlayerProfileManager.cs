using UnityEngine;
using Cysharp.Threading.Tasks;

public static class PlayerProfileManager
{
    private const string SaveKey = "crossbound.player_profile";
    private static PlayerProfile _profile;
    
    public static PlayerProfile Profile => _profile ??= Load();
    
    public static PlayerProfile Load()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
        {
            _profile = new PlayerProfile();
            return _profile;
        }

        string json = PlayerPrefs.GetString(SaveKey);
        if (string.IsNullOrEmpty(json))
        {
            _profile = new PlayerProfile();
            return _profile;
        }

        _profile = JsonUtility.FromJson<PlayerProfile>(json) ?? new PlayerProfile();
        return _profile;
    }
    
    public static UniTask SaveAsync()
    {
        Save();
        return UniTask.CompletedTask;
    }
    
    public static void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Profile));
        PlayerPrefs.Save();
    }
    
    public static void CompletePuzzle(string puzzleId, int score)
    {
        PlayerProfile profile = Profile;
        if (!profile.completedPuzzleIds.Contains(puzzleId))
        {
            profile.completedPuzzles++;
            profile.completedPuzzleIds.Add(puzzleId);
        }
        profile.totalScore += score;
        profile.lastPlayed = System.DateTime.Now;
        Save();
    }
    
    public static void SetPlayerName(string name)
    {
        Profile.playerName = name;
        Save();
    }
}
