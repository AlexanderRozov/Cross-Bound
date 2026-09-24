using UnityEngine;
using System.IO;
using Cysharp.Threading.Tasks;

public static class PlayerProfileManager
{
    private const string SaveFileName = "player_profile.json";
#if UNITY_EDITOR || UNITY_STANDALONE_WIN 
    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);
#endif
    private static PlayerProfile _profile;
    
    public static PlayerProfile Profile => _profile ??= Load();
    
    public static PlayerProfile Load()
    {
        if (File.Exists(SavePath))
        {
            try
            {
                string json = File.ReadAllText(SavePath);
                _profile = JsonUtility.FromJson<PlayerProfile>(json);
                return _profile;
            }
            catch
            {
                _profile = new PlayerProfile();
            }
        }
        else
        {
            _profile = new PlayerProfile();
        }
        return _profile;
    }
    
    public static async UniTask SaveAsync()
    {
        string json = JsonUtility.ToJson(_profile, true);
        await File.WriteAllTextAsync(SavePath, json);
    }
    
    public static void Save() => File.WriteAllText(SavePath, JsonUtility.ToJson(_profile, true));
    
    public static void CompletePuzzle(string puzzleId, int score)
    {
        _profile.completedPuzzles++;
        _profile.totalScore += score;
        if (!_profile.completedPuzzleIds.Contains(puzzleId))
            _profile.completedPuzzleIds.Add(puzzleId);
        _profile.lastPlayed = System.DateTime.Now;
        Save();
    }
    
    public static void SetPlayerName(string name)
    {
        _profile.playerName = name;
        Save();
    }
}