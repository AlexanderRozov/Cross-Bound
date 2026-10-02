using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Headless smoke tests for the crossword game logic. Run from the menu
/// (CrossBound → Run Smoke Tests) or in batchmode via CrossBoundProjectSetup.BatchSetup.
/// Throws when any test fails so CI/batch runs exit with an error.
/// </summary>
public static class CrossBoundSmokeTests
{
    private static int _passed;
    private static int _failed;

    [MenuItem("CrossBound/Run Smoke Tests")]
    public static void RunAll()
    {
        _passed = 0;
        _failed = 0;

        Run("Crossword data loads and validates", TestDataValidates);
        Run("Answer grid builds with crossings", TestGridBuilds);
        Run("Cell numbers assigned to start cells", TestCellNumbers);
        Run("Full solve completes the puzzle", TestFullSolve);
        Run("Score cannot be farmed (correct cells locked)", TestScoreLock);
        Run("Delete clears input and steps back", TestDelete);
        Run("Hint reveals word with penalty", TestHintPenalty);
        Run("Check word distinguishes complete/incomplete", TestCheckWord);
        Run("Direction toggle on repeated cell click", TestDirectionToggle);
        Run("Validator detects crossing conflicts", TestValidatorDetectsConflicts);

        Debug.Log($"[CrossBound][Tests] Finished: {_passed} passed, {_failed} failed.");
        if (_failed > 0)
            throw new Exception($"[CrossBound][Tests] {_failed} smoke test(s) failed.");
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            _passed++;
            Debug.Log($"[CrossBound][Tests] PASS: {name}");
        }
        catch (Exception exception)
        {
            _failed++;
            Debug.LogError($"[CrossBound][Tests] FAIL: {name}\n{exception.Message}");
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static CrosswordData LoadData()
    {
        TextAsset asset = Resources.Load<TextAsset>("CrosswordQuestions");
        Assert(asset != null, "Resources/CrosswordQuestions.json not found.");
        CrosswordData data = JsonUtility.FromJson<CrosswordData>(asset.text);
        Assert(data != null, "Failed to parse crossword JSON.");
        data.NormalizeGeneratedLayout();
        return data;
    }

    private static CrosswordGameState CreateState(out CrosswordData data)
    {
        data = LoadData();
        CrosswordGameState state = new CrosswordGameState();
        state.Initialize(data);
        return state;
    }

    private static int CountAnswerCells(CrosswordGameState state)
    {
        int count = 0;
        for (int y = 0; y < state.Height; y++)
            for (int x = 0; x < state.Width; x++)
                if (state.Answers[x, y] != '\0') count++;
        return count;
    }

    private static void TestDataValidates()
    {
        List<string> errors = CrosswordDataValidator.Validate(LoadData());
        Assert(errors.Count == 0, "Validator errors:\n - " + string.Join("\n - ", errors));
    }

    private static void TestGridBuilds()
    {
        CrosswordGameState state = CreateState(out _);
        Assert(state.Answers[5, 2] == 'C', "Expected 'C' at (5,2).");
        Assert(state.Answers[7, 2] == 'T', "Expected 'T' at (7,2).");
        Assert(state.Answers[9, 2] == 'F', "Expected 'F' at (9,2).");
        Assert(state.Answers[5, 6] == 'S', "Expected 'S' at (5,6).");
        Assert(state.Answers[11, 2] == 'S', "Expected 'S' at (11,2).");
        Assert(!state.HasAnswer(0, 0), "Cell (0,0) should be empty.");
    }

    private static void TestCellNumbers()
    {
        CrosswordGameState state = CreateState(out _);
        Assert(state.GetCellNumber(5, 2) == 1, "Start cell (5,2) should be number 1.");
        Assert(state.GetCellNumber(9, 2) == 3, "Start cell (9,2) should be number 3.");
        Assert(state.GetCellNumber(6, 2) == 0, "Cell (6,2) is not a start cell.");
    }

    private static void TestFullSolve()
    {
        CrosswordGameState state = CreateState(out CrosswordData data);
        bool completed = false;
        int finalScore = 0;
        state.Completed += score => { completed = true; finalScore = score; };

        foreach (CrosswordQuestion question in data.Questions)
        {
            state.SelectQuestion(question);
            foreach (char letter in question.answer.ToUpperInvariant())
                state.InputLetter(letter);
        }

        Assert(completed, "Puzzle did not complete after solving all words.");
        int expected = CountAnswerCells(state) * CrosswordGameState.PointsPerLetter;
        Assert(finalScore == expected, $"Expected {expected} points, got {finalScore}.");
        Assert(state.Score == expected, $"State score mismatch: {state.Score}.");
    }

    private static void TestScoreLock()
    {
        CrosswordGameState state = CreateState(out CrosswordData data);
        CrosswordQuestion cat = data.Questions[0];
        Assert(cat.answer == "CAT", "First question should be CAT.");

        state.SelectQuestion(cat);
        state.InputLetter('C'); // moves to (6,2)
        Assert(state.Score == 10, $"Score after first correct letter should be 10, got {state.Score}.");

        state.SelectCell(5, 2);
        state.InputLetter('C'); // already correct: locked
        Assert(state.Score == 10, $"Score must not grow on locked cells, got {state.Score}.");

        state.InputLetter('X'); // wrong letter at (6,2), selection moves on
        Assert(state.Score == 10, "Wrong letter must not change score.");
        Assert(state.GetInput(6, 2) == 'X', "Wrong letter should be stored as incorrect.");

        state.SelectCell(6, 2);
        state.InputLetter('A'); // now correct the same cell
        Assert(state.Score == 20, $"Correcting a wrong cell should add 10, got {state.Score}.");
    }

    private static void TestDelete()
    {
        CrosswordGameState state = CreateState(out CrosswordData data);
        state.SelectQuestion(data.Questions[0]); // CAT at (5,2)
        state.InputLetter('X'); // wrong, selection moves to (6,2)

        state.DeleteLetter(); // (6,2) empty -> step back to (5,2) and clear
        Assert(state.GetInput(5, 2) == '\0', "Delete should clear the wrong letter at (5,2).");
        Assert(state.SelectedX == 5 && state.SelectedY == 2, "Selection should step back to (5,2).");

        state.InputLetter('C'); // correct, locked
        state.DeleteLetter(); // locked cell: must not clear
        Assert(state.GetInput(5, 2) == 'C', "Locked correct cell must not be deletable.");
    }

    private static void TestHintPenalty()
    {
        CrosswordGameState state = CreateState(out CrosswordData data);
        state.SelectQuestion(data.Questions[0]);
        state.InputLetter('C'); // +10
        state.SelectQuestion(data.Questions[0]);

        state.RevealCurrentWord(30);
        Assert(state.GetInput(5, 2) == 'C' && state.GetInput(6, 2) == 'A' && state.GetInput(7, 2) == 'T',
            "Hint should reveal the whole word.");
        Assert(state.Score == 0, $"Hint penalty should clamp score to 0, got {state.Score}.");
        Assert(!state.IsCompleted, "Single hint must not complete the puzzle.");
    }

    private static void TestCheckWord()
    {
        CrosswordGameState state = CreateState(out CrosswordData data);
        state.SelectQuestion(data.Questions[0]);
        Assert(!state.CheckCurrentWord(), "Empty word must not pass the check.");

        state.SelectQuestion(data.Questions[0]);
        state.InputLetter('C');
        state.SelectQuestion(data.Questions[0]);
        Assert(!state.CheckCurrentWord(), "Partially filled word must not pass the check.");

        foreach (char letter in "CAT")
        {
            state.SelectQuestion(data.Questions[0]);
            state.InputLetter(letter);
        }
        state.SelectQuestion(data.Questions[0]);
        Assert(state.CheckCurrentWord(), "Fully correct word should pass the check.");
    }

    private static void TestDirectionToggle()
    {
        CrosswordGameState state = CreateState(out CrosswordData data);
        CrosswordQuestion cat = data.Questions[0];  // CAT across at (5,2)
        CrosswordQuestion cow = data.Questions[1];  // COW down at (5,2)

        state.SelectCell(5, 2);
        Assert(state.CurrentQuestion == cat || state.CurrentQuestion == cow, "A question should be selected.");

        CrosswordQuestion first = state.CurrentQuestion;
        state.SelectCell(5, 2); // same cell again -> toggle direction
        Assert(state.CurrentQuestion != first && state.CurrentQuestion != null,
            "Repeated click on the same cell should toggle the question direction.");
    }

    private static void TestValidatorDetectsConflicts()
    {
        string json = "{\"questions\":[" +
            "{\"id\":\"a\",\"answer\":\"CAT\",\"question\":\"q\",\"startX\":0,\"startY\":0,\"isHorizontal\":true}," +
            "{\"id\":\"b\",\"answer\":\"DOG\",\"question\":\"q\",\"startX\":0,\"startY\":0,\"isHorizontal\":false}" +
            "],\"gridWidth\":15,\"gridHeight\":15}";
        CrosswordData data = JsonUtility.FromJson<CrosswordData>(json);
        List<string> errors = CrosswordDataValidator.Validate(data);
        Assert(errors.Exists(e => e.Contains("Crossing conflict")), "Validator should report the crossing conflict.");
    }
}
