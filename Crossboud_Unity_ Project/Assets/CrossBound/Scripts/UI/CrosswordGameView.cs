using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>UI Toolkit representation of a crossword. It contains no content or platform dependencies.</summary>
public sealed class CrosswordGameView : MonoBehaviour
{
    private UIDocument _document;
    private Label _number, _question, _score;
    private VisualElement _grid;
    private readonly Dictionary<Vector2Int, Button> _cells = new();
    public void Configure(UIDocument document) => _document = document;

    public void Build(CrosswordData data, Action<int, int> select, Action delete, Action hint)
    {
        _document ??= GetComponent<UIDocument>();
        VisualElement root = _document.rootVisualElement; root.Clear();
        VisualTreeAsset layout = Resources.Load<VisualTreeAsset>("UI/CrosswordGame");
        if (layout != null) layout.CloneTree(root); else CreateFallback(root);
        StyleSheet style = Resources.Load<StyleSheet>("UI/CrosswordGame"); if (style != null) root.styleSheets.Add(style);
        _number = root.Q<Label>("question-number"); _question = root.Q<Label>("question"); _score = root.Q<Label>("score"); _grid = root.Q<VisualElement>("grid-container");
        root.Q<Button>("delete-button").clicked += delete; root.Q<Button>("hint-button").clicked += hint; root.Q<Button>("submit-button").clicked += hint;
        _grid.style.width = data.gridWidth * 42; _grid.style.height = data.gridHeight * 42;
        for (int y = 0; y < data.gridHeight; y++) for (int x = 0; x < data.gridWidth; x++)
        {
            int cx = x, cy = y; Button cell = new Button { name = $"cell-{x}-{y}" }; cell.AddToClassList("crossword-cell");
            if (!HasLetter(data, x, y)) cell.AddToClassList("blocked"); else cell.clicked += () => select(cx, cy);
            _grid.Add(cell); _cells[new Vector2Int(x, y)] = cell;
        }
    }
    public void SetQuestion(int number, string question, int score) { _number.text = number.ToString(); _question.text = question; SetScore(score); }
    public void SetScore(int score) => _score.text = score.ToString();
    public void SetSelected(int x, int y) { foreach (Button cell in _cells.Values) cell.RemoveFromClassList("selected"); _cells[new Vector2Int(x, y)].AddToClassList("selected"); }
    public void SetCell(int x, int y, char letter, bool correct) { Button cell = _cells[new Vector2Int(x, y)]; cell.text = letter == '\0' ? string.Empty : letter.ToString(); cell.EnableInClassList("correct", correct); cell.EnableInClassList("incorrect", letter != '\0' && !correct); }
    public void ShowCompleted(int score) { _question.text = $"Поздравляем! Кроссворд решён. Очки: {score}"; }
    private static bool HasLetter(CrosswordData data, int x, int y) { foreach (CrosswordQuestion q in data.Questions) { int qx = q.startX, qy = q.startY; foreach (char _ in q.answer) { if (qx == x && qy == y) return true; if (q.isHorizontal) qx++; else qy++; } } return false; }
    private static void CreateFallback(VisualElement root) { root.Add(new Label("Cross-Bound") { name = "question-number" }); root.Add(new Label { name = "score" }); root.Add(new Label { name = "question" }); root.Add(new VisualElement { name = "grid-container" }); root.Add(new Button { name = "delete-button", text = "Удалить" }); root.Add(new Button { name = "hint-button", text = "Подсказка" }); root.Add(new Button { name = "submit-button", text = "Проверить" }); }
}
