using System.Collections.Generic;
using UnityEngine;

/// <summary>Small runtime smoke checks run before leaving ProjectLoader.</summary>
public static class CrossBoundStartupChecks
{
    public static bool Run(string gameSceneName)
    {
        List<string> failures = new List<string>();
        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
            failures.Add($"Scene '{gameSceneName}' is not included in Build Settings.");
        if (InputController.Instance == null)
            failures.Add("InputController is missing from ProjectLoader.");

        TextAsset crossword = Resources.Load<TextAsset>("CrosswordQuestions");
        if (crossword == null)
        {
            failures.Add("Resources/CrosswordQuestions.json was not found.");
        }
        else
        {
            CrosswordData data = JsonUtility.FromJson<CrosswordData>(crossword.text);
            failures.AddRange(CrosswordDataValidator.Validate(data));
        }

        if (failures.Count == 0)
        {
            Debug.Log($"[CrossBound][Startup] Smoke checks passed. Scene={gameSceneName}; Input=ready; crossword=ready.");
            return true;
        }

        foreach (string failure in failures)
            Debug.LogError($"[CrossBound][Startup] {failure}");
        return false;
    }
}
