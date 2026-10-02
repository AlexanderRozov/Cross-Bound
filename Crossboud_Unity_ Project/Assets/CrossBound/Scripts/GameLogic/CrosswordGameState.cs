using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure crossword game state: no UI and no platform dependencies.
/// The controller feeds player input in, the view renders state out.
/// Scoring rules: +10 for each newly correct letter; already correct
/// cells are locked so score cannot be farmed; hint reveals a word for
/// a fixed penalty.
/// </summary>
public sealed class CrosswordGameState
{
    public const int PointsPerLetter = 10;
    public const int DefaultHintPenalty = 30;

    public event Action<int, int> CellUpdated;                     // (x, y) letter or style changed
    public event Action SelectionChanged;                          // selected cell / highlight changed
    public event Action<int> ScoreChanged;                         // new score
    public event Action<CrosswordQuestion, int> QuestionChanged;   // current question + display number
    public event Action<int> Completed;                            // final score

    public CrosswordData Data { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public char[,] Answers { get; private set; }
    public char[,] Inputs { get; private set; }
    public int SelectedX { get; private set; } = -1;
    public int SelectedY { get; private set; } = -1;
    public CrosswordQuestion CurrentQuestion { get; private set; }
    public int Score { get; private set; }
    public bool IsCompleted { get; private set; }

    private readonly Dictionary<Vector2Int, int> _cellNumbers = new();
    private readonly Dictionary<Vector2Int, List<CrosswordQuestion>> _questionsAtCell = new();

    public void Initialize(CrosswordData data)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Data.NormalizeGeneratedLayout();
        Width = Mathf.Max(1, Data.gridWidth);
        Height = Mathf.Max(1, Data.gridHeight);
        Answers = new char[Width, Height];
        Inputs = new char[Width, Height];
        Score = 0;
        IsCompleted = false;

        foreach (CrosswordQuestion question in Data.Questions)
        {
            foreach ((int x, int y, char letter) in Walk(question))
            {
                if (InBounds(x, y))
                    Answers[x, y] = letter;
            }
        }

        IndexQuestions();
    }

    // ----------------------------- Queries used by the view -----------------------------

    public bool HasAnswer(int x, int y) => InBounds(x, y) && Answers[x, y] != '\0';
    public char GetInput(int x, int y) => InBounds(x, y) ? Inputs[x, y] : '\0';
    public bool IsCorrect(int x, int y) => HasAnswer(x, y) && Inputs[x, y] == Answers[x, y];
    public bool IsSelected(int x, int y) => x == SelectedX && y == SelectedY;

    /// <summary>Clue number shown in the top-left corner of a start cell, or 0 for non-start cells.</summary>
    public int GetCellNumber(int x, int y) => _cellNumbers.TryGetValue(new Vector2Int(x, y), out int number) ? number : 0;

    public int GetQuestionNumber(CrosswordQuestion question)
    {
        if (question == null) return 0;
        if (question.number > 0) return question.number;
        IReadOnlyList<CrosswordQuestion> questions = Data.Questions;
        for (int i = 0; i < questions.Count; i++)
            if (ReferenceEquals(questions[i], question)) return i + 1;
        return 1;
    }

    public bool IsCellInCurrentWord(int x, int y) => CurrentQuestion != null && Contains(CurrentQuestion, x, y);

    public IReadOnlyList<CrosswordQuestion> GetQuestionsAt(int x, int y) =>
        _questionsAtCell.TryGetValue(new Vector2Int(x, y), out List<CrosswordQuestion> list) ? list : EmptyList;

    private static readonly List<CrosswordQuestion> EmptyList = new();

    // ----------------------------- Player actions -----------------------------

    /// <summary>Selects a cell; clicking the already selected cell again toggles across/down when both exist.</summary>
    public bool SelectCell(int x, int y)
    {
        if (!HasAnswer(x, y)) return false;

        bool sameCell = x == SelectedX && y == SelectedY;
        SelectedX = x;
        SelectedY = y;

        IReadOnlyList<CrosswordQuestion> questions = GetQuestionsAt(x, y);
        CrosswordQuestion question = null;
        if (questions.Count > 0)
        {
            if (CurrentQuestion != null && Contains(CurrentQuestion, x, y))
                question = CurrentQuestion;
            else
                question = questions[0];

            if (sameCell && questions.Count > 1)
            {
                int index = 0;
                for (int i = 0; i < questions.Count; i++)
                    if (ReferenceEquals(questions[i], question)) { index = i; break; }
                question = questions[(index + 1) % questions.Count];
            }
        }

        SetCurrentQuestion(question);
        return true;
    }

    public void SelectQuestion(CrosswordQuestion question)
    {
        if (question == null) return;
        SelectedX = question.startX;
        SelectedY = question.startY;
        SetCurrentQuestion(question);
    }

    public void InputLetter(char letter)
    {
        if (IsCompleted || SelectedX < 0 || !HasAnswer(SelectedX, SelectedY)) return;

        letter = char.ToUpperInvariant(letter);
        if (letter < 'A' || letter > 'Z') return;

        // Correct cells are locked: retyping them neither changes the cell nor the score.
        if (Inputs[SelectedX, SelectedY] == Answers[SelectedX, SelectedY])
        {
            MoveForward();
            return;
        }

        bool wasCorrect = Inputs[SelectedX, SelectedY] != '\0' && Inputs[SelectedX, SelectedY] == Answers[SelectedX, SelectedY];
        Inputs[SelectedX, SelectedY] = letter;
        if (letter == Answers[SelectedX, SelectedY] && !wasCorrect)
        {
            Score += PointsPerLetter;
            ScoreChanged?.Invoke(Score);
        }
        CellUpdated?.Invoke(SelectedX, SelectedY);
        MoveForward();
        CheckCompletion();
    }

    /// <summary>Clears the current cell; when it is empty (or locked correct) steps back and clears there instead.</summary>
    public void DeleteLetter()
    {
        if (IsCompleted || SelectedX < 0) return;

        if (TryClearCell(SelectedX, SelectedY))
            return;

        MoveBackward();
        if (SelectedX >= 0)
            TryClearCell(SelectedX, SelectedY);
    }

    /// <summary>Checks the current word: true when every letter is filled and correct.</summary>
    public bool CheckCurrentWord()
    {
        if (IsCompleted || CurrentQuestion == null) return false;

        bool complete = true;
        foreach ((int x, int y, char letter) in Walk(CurrentQuestion))
        {
            if (!InBounds(x, y)) continue;
            if (Inputs[x, y] != letter) complete = false;
            CellUpdated?.Invoke(x, y); // refresh correct/incorrect styling
        }
        return complete;
    }

    /// <summary>Reveals the current word. Costs <paramref name="penalty"/> points (never below zero) and awards no points.</summary>
    public void RevealCurrentWord(int penalty = DefaultHintPenalty)
    {
        if (IsCompleted || CurrentQuestion == null) return;

        foreach ((int x, int y, char letter) in Walk(CurrentQuestion))
        {
            if (!InBounds(x, y) || Inputs[x, y] == letter) continue;
            Inputs[x, y] = letter;
            CellUpdated?.Invoke(x, y);
        }

        if (penalty > 0 && Score > 0)
        {
            Score = Mathf.Max(0, Score - penalty);
            ScoreChanged?.Invoke(Score);
        }
        CheckCompletion();
    }

    // ----------------------------- Internals -----------------------------

    private void SetCurrentQuestion(CrosswordQuestion question)
    {
        CurrentQuestion = question;
        SelectionChanged?.Invoke();
        if (question != null)
            QuestionChanged?.Invoke(question, GetQuestionNumber(question));
    }

    private bool TryClearCell(int x, int y)
    {
        if (!InBounds(x, y)) return false;
        if (Inputs[x, y] == '\0' || Inputs[x, y] == Answers[x, y]) return false; // never clear locked correct cells
        Inputs[x, y] = '\0';
        CellUpdated?.Invoke(x, y);
        return true;
    }

    private void MoveForward() => Step(+1);
    private void MoveBackward() => Step(-1);

    private void Step(int delta)
    {
        if (CurrentQuestion == null || SelectedX < 0) return;
        int x = SelectedX, y = SelectedY;
        int limit = CurrentQuestion.isHorizontal ? Width : Height;
        for (int i = 0; i < limit; i++)
        {
            if (CurrentQuestion.isHorizontal) x += delta; else y += delta;
            if (!InBounds(x, y)) break;
            if (Contains(CurrentQuestion, x, y))
            {
                SelectedX = x;
                SelectedY = y;
                SelectionChanged?.Invoke();
                return;
            }
        }
    }

    private void CheckCompletion()
    {
        if (IsCompleted) return;
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                if (Answers[x, y] != '\0' && Answers[x, y] != Inputs[x, y])
                    return;

        IsCompleted = true;
        Completed?.Invoke(Score);
    }

    private bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    private static bool Contains(CrosswordQuestion question, int x, int y)
    {
        foreach ((int qx, int qy, char _) in Walk(question))
            if (qx == x && qy == y) return true;
        return false;
    }

    private static IEnumerable<(int x, int y, char letter)> Walk(CrosswordQuestion question)
    {
        int x = question.startX, y = question.startY;
        foreach (char letter in question.answer.ToUpperInvariant())
        {
            yield return (x, y, letter);
            if (question.isHorizontal) x++; else y++;
        }
    }

    private void IndexQuestions()
    {
        _cellNumbers.Clear();
        _questionsAtCell.Clear();

        foreach (CrosswordQuestion question in Data.Questions)
        {
            foreach ((int x, int y, char _) in Walk(question))
            {
                Vector2Int key = new Vector2Int(x, y);
                if (!_questionsAtCell.TryGetValue(key, out List<CrosswordQuestion> list))
                {
                    list = new List<CrosswordQuestion>();
                    _questionsAtCell[key] = list;
                }
                if (!list.Contains(question))
                    list.Add(question);
            }
        }

        List<CrosswordQuestion> starts = new List<CrosswordQuestion>(Data.Questions);
        starts.Sort((a, b) => a.startY != b.startY ? a.startY.CompareTo(b.startY) : a.startX.CompareTo(b.startX));
        foreach (CrosswordQuestion question in starts)
            _cellNumbers.TryAdd(new Vector2Int(question.startX, question.startY), GetQuestionNumber(question));
    }
}
