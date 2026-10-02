using System.Collections.Generic;
using UnityEngine;

/// <summary>Small runtime smoke checks run before leaving ProjectLoader.</summary>
public static class CrossBoundStartupChecks
{
    public static bool Run(string gameSceneName)
    {
        var failures = new List<string>();
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
            if (data == null || data.Questions.Count == 0)
                failures.Add("The test crossword has no questions.");
            else
                ValidateQuestions(data, failures);
        }

        if (failures.Count == 0)
        {
            Debug.Log($"[CrossBound][Startup] Smoke checks passed. Scene={gameSceneName}; Input=ready; test crossword=ready.");
            return true;
        }

        foreach (string failure in failures)
            Debug.LogError($"[CrossBound][Startup] {failure}");
        return false;
    }

    private static void ValidateQuestions(CrosswordData data, ICollection<string> failures)
    {
        foreach (CrosswordQuestion question in data.Questions)
        {
            if (string.IsNullOrWhiteSpace(question.id) || string.IsNullOrWhiteSpace(question.answer) || string.IsNullOrWhiteSpace(question.question))
                failures.Add("A test crossword question is missing id, answer, or text.");
            if (question.startX < 0 || question.startY < 0 || question.startX >= data.gridWidth || question.startY >= data.gridHeight)
                failures.Add($"Question '{question.id}' starts outside the grid.");
        }
    }
}
