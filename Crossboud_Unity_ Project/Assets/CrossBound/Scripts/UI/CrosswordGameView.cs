using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit representation of a crossword. Renders <see cref="CrosswordGameState"/>
/// and forwards user gestures; it contains no content or platform dependencies.
/// </summary>
public sealed class CrosswordGameView : MonoBehaviour
{
    private const int CellPitch = 42; // cell 40px + 1px margin on each side, matches the USS.

    private UIDocument _document;
    private Label _number, _question, _score;
    private VisualElement _grid;
    private CrosswordGameState _state;
    private Action _onRestart;
    private VisualElement _winOverlay;
    private Label _winScore, _winBest;
    private readonly Dictionary<Vector2Int, VisualElement> _cells = new();
    private readonly Dictionary<Vector2Int, Label> _cellLetters = new();

    public void Configure(UIDocument document) => _document = document;

    /// <summary>Builds the whole screen. Returns false (and logs why) when the UI cannot be constructed.</summary>
    public bool Build(CrosswordGameState state, Action<int, int> select, Action delete, Action hint, Action check, Action restart)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _onRestart = restart;

        _document ??= GetComponent<UIDocument>();
        if (_document == null)
        {
            Debug.LogError("[CrossBound][View] UIDocument component is MISSING — cannot build UI.");
            return false;
        }
        VisualElement root = _document.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("[CrossBound][View] UIDocument.rootVisualElement is NULL — the panel was not initialized.");
            return false;
        }
        root.Clear();
        _winOverlay = null;

        VisualTreeAsset layout = Resources.Load<VisualTreeAsset>("UI/CrosswordGame");
        Debug.Log($"[CrossBound][View] UXML layout: {(layout != null ? "loaded" : "MISSING — building code fallback")}.");
        if (layout != null) layout.CloneTree(root); else CreateFallback(root);
        StyleSheet style = Resources.Load<StyleSheet>("UI/CrosswordGame");
        Debug.Log($"[CrossBound][View] USS style: {(style != null ? "loaded" : "MISSING")}.");
        if (style != null) root.styleSheets.Add(style);

        _number = root.Q<Label>("question-number");
        _question = root.Q<Label>("question");
        _score = root.Q<Label>("score");
        _grid = root.Q<VisualElement>("grid-container");
        Debug.Log($"[CrossBound][View] Elements: question-number={(_number != null ? "ok" : "NULL")}, question={(_question != null ? "ok" : "NULL")}, score={(_score != null ? "ok" : "NULL")}, grid-container={(_grid != null ? "ok" : "NULL")}.");
        if (_grid == null)
        {
            Debug.LogError("[CrossBound][View] 'grid-container' element not found — the crossword grid cannot be built.");
            return false;
        }

        Button deleteButton = root.Q<Button>("delete-button");
        Button hintButton = root.Q<Button>("hint-button");
        Button submitButton = root.Q<Button>("submit-button");
        Debug.Log($"[CrossBound][View] Buttons: delete={(deleteButton != null ? "ok" : "NULL")}, hint={(hintButton != null ? "ok" : "NULL")}, submit={(submitButton != null ? "ok" : "NULL")}.");
        if (deleteButton != null) deleteButton.clicked += () => delete?.Invoke();
        if (hintButton != null) hintButton.clicked += () => hint?.Invoke();
        if (submitButton != null) submitButton.clicked += () => check?.Invoke();

        BuildGrid(select);
        Debug.Log($"[CrossBound][View] Grid built: {_cells.Count} cells for {_state.Width}x{_state.Height}.");

        _state.CellUpdated += OnCellUpdated;
        _state.SelectionChanged += OnSelectionChanged;
        _state.ScoreChanged += OnScoreChanged;
        _state.QuestionChanged += OnQuestionChanged;

        RefreshAllCells();
        OnSelectionChanged();
        _score.text = _state.Score.ToString();
        Debug.Log("[CrossBound][View] Build finished.");
        return true;
    }

    // ----------------------------- Grid -----------------------------

    private void BuildGrid(Action<int, int> select)
    {
        _cells.Clear();
        _cellLetters.Clear();
        _grid.Clear();
        _grid.style.width = _state.Width * CellPitch;
        _grid.style.height = _state.Height * CellPitch;

        for (int y = 0; y < _state.Height; y++)
        for (int x = 0; x < _state.Width; x++)
        {
            VisualElement cell = new VisualElement { name = $"cell-{x}-{y}" };
            cell.AddToClassList("crossword-cell");

            if (!_state.HasAnswer(x, y))
            {
                cell.AddToClassList("blocked");
            }
            else
            {
                int number = _state.GetCellNumber(x, y);
                if (number > 0)
                    cell.Add(new Label(number.ToString()) { name = "cell-number" });
                Label letter = new Label { name = "cell-letter" };
                cell.Add(letter);
                _cellLetters[new Vector2Int(x, y)] = letter;

                int cx = x, cy = y;
                cell.RegisterCallback<ClickEvent>(_ => select?.Invoke(cx, cy));
            }

            _grid.Add(cell);
            _cells[new Vector2Int(x, y)] = cell;
        }
    }

    private void RefreshAllCells()
    {
        for (int y = 0; y < _state.Height; y++)
        for (int x = 0; x < _state.Width; x++)
            UpdateCell(x, y);
    }

    // ----------------------------- State subscriptions -----------------------------

    private void OnCellUpdated(int x, int y) => UpdateCell(x, y);

    private void UpdateCell(int x, int y)
    {
        Vector2Int key = new Vector2Int(x, y);
        if (!_cells.TryGetValue(key, out VisualElement cell)) return;

        char input = _state.GetInput(x, y);
        bool hasLetter = input != '\0';
        bool correct = _state.IsCorrect(x, y);

        if (_cellLetters.TryGetValue(key, out Label letter))
            letter.text = hasLetter ? input.ToString() : string.Empty;

        cell.EnableInClassList("correct", correct);
        cell.EnableInClassList("incorrect", hasLetter && !correct);
    }

    private void OnSelectionChanged()
    {
        foreach (VisualElement cell in _cells.Values)
            cell.RemoveFromClassList("selected");

        if (_state == null || _state.SelectedX < 0) return;

        for (int y = 0; y < _state.Height; y++)
        for (int x = 0; x < _state.Width; x++)
        {
            VisualElement cell = _cells[new Vector2Int(x, y)];
            bool inWord = _state.IsCellInCurrentWord(x, y);
            cell.EnableInClassList("in-word", inWord && !_state.IsSelected(x, y));
        }

        _cells[new Vector2Int(_state.SelectedX, _state.SelectedY)].AddToClassList("selected");
    }

    private void OnScoreChanged(int score) => _score.text = score.ToString();

    private void OnQuestionChanged(CrosswordQuestion question, int number)
    {
        _number.text = $"{number} {(question.isHorizontal ? "→" : "↓")}";
        _question.text = question.question;
    }

    // ----------------------------- Win overlay -----------------------------

    public void ShowCompleted(int score, int bestScore)
    {
        if (_winOverlay != null) return;

        VisualElement root = _document.rootVisualElement;
        _winOverlay = new VisualElement { name = "win-overlay" };
        _winOverlay.AddToClassList("win-overlay");

        VisualElement panel = new VisualElement();
        panel.AddToClassList("win-panel");
        _winOverlay.Add(panel);

        panel.Add(new Label("ПОЗДРАВЛЯЕМ!") { name = "win-title" });
        panel.Add(new Label("Кроссворд решён") { name = "win-text" });
        _winScore = new Label($"Очки: {score}") { name = "win-score" };
        _winBest = new Label($"Рекорд: {bestScore}") { name = "win-best" };
        panel.Add(_winScore);
        panel.Add(_winBest);

        Button restart = new Button(() => _onRestart?.Invoke()) { text = "ИГРАТЬ СНОВА" };
        restart.name = "restart-button";
        panel.Add(restart);

        root.Add(_winOverlay);

        // Small entrance animation.
        panel.AddToClassList("win-panel-hidden");
        panel.schedule.Execute(() => panel.RemoveFromClassList("win-panel-hidden")).StartingIn(50);
    }

    // ----------------------------- Fallback layout (no UXML) -----------------------------

    private static void CreateFallback(VisualElement root)
    {
        VisualElement screen = new VisualElement { name = "root" };
        screen.AddToClassList("game-screen");

        VisualElement topBar = new VisualElement();
        topBar.AddToClassList("top-bar");
        topBar.Add(new Label("1") { name = "question-number" });
        Label caption = new Label("ОЧКИ");
        caption.AddToClassList("score-caption");
        topBar.Add(caption);
        Label score = new Label("0") { name = "score" };
        score.AddToClassList("score");
        topBar.Add(score);
        screen.Add(topBar);

        VisualElement questionPanel = new VisualElement();
        questionPanel.AddToClassList("question-panel");
        questionPanel.Add(new Label("Загрузка кроссворда…") { name = "question" });
        screen.Add(questionPanel);

        screen.Add(new VisualElement { name = "grid-container" });

        VisualElement bottomBar = new VisualElement();
        bottomBar.AddToClassList("bottom-bar");
        bottomBar.Add(new Button { name = "delete-button", text = "УДАЛИТЬ" });
        bottomBar.Add(new Button { name = "hint-button", text = "ПОДСКАЗКА" });
        bottomBar.Add(new Button { name = "submit-button", text = "ПРОВЕРИТЬ" });
        screen.Add(bottomBar);

        root.Add(screen);
    }
}
