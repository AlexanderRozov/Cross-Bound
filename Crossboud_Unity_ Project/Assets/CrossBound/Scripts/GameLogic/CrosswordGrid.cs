using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public sealed class CrosswordGrid : MonoBehaviour
{
    private RectTransform _container;
    private LetterCell[,] _cells;
    private CrosswordData _data;
    private LetterCell _selectedCell;
    private Dictionary<string, CrosswordQuestion> _questionsById;
    private bool _completed;
    public event Action OnPuzzleCompleted;
    public event Action<CrosswordQuestion> OnQuestionSelected;

    public void Configure(RectTransform container) => _container = container;
    public async UniTask Initialize(CrosswordData data)
    {
        _data = data ?? new CrosswordData();
        _questionsById = new Dictionary<string, CrosswordQuestion>();
        foreach (CrosswordQuestion question in _data.Questions) _questionsById[question.id] = question;
        _cells = new LetterCell[_data.gridWidth, _data.gridHeight];
        BuildGrid();
        await UniTask.CompletedTask;
    }

    private void BuildGrid()
    {
        if (_container == null) throw new InvalidOperationException("CrosswordGrid requires a UI container.");
        foreach (Transform child in _container) Destroy(child.gameObject);
        GridLayoutGroup layout = _container.GetComponent<GridLayoutGroup>() ?? _container.gameObject.AddComponent<GridLayoutGroup>();
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount = _data.gridWidth;
        layout.spacing = new Vector2(3, 3); layout.childAlignment = TextAnchor.MiddleCenter;
        float size = Mathf.Min(54f, 620f / Mathf.Max(_data.gridWidth, _data.gridHeight));
        layout.cellSize = new Vector2(size, size);

        var answers = new Dictionary<(int, int), char>();
        foreach (CrosswordQuestion question in _data.Questions)
        {
            int x = question.startX, y = question.startY;
            foreach (char letter in question.answer.ToUpperInvariant())
            {
                if (x < 0 || y < 0 || x >= _data.gridWidth || y >= _data.gridHeight) break;
                if (answers.TryGetValue((x, y), out char existing) && existing != letter)
                    throw new InvalidOperationException($"Crossword has conflicting letters at {x}, {y}.");
                answers[(x, y)] = letter; if (question.isHorizontal) x++; else y++;
            }
        }
        for (int y = 0; y < _data.gridHeight; y++) for (int x = 0; x < _data.gridWidth; x++)
        {
            bool active = answers.TryGetValue((x, y), out char answer);
            LetterCell cell = CreateCell(x, y, answer, active); _cells[x, y] = cell;
        }
    }

    private LetterCell CreateCell(int x, int y, char answer, bool active)
    {
        GameObject go = new GameObject($"Cell_{x}_{y}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LetterCell));
        go.transform.SetParent(_container, false);
        Image image = go.GetComponent<Image>(); image.raycastTarget = active;
        Button button = go.GetComponent<Button>(); button.interactable = active;
        GameObject label = new GameObject("Letter", typeof(RectTransform), typeof(TextMeshProUGUI)); label.transform.SetParent(go.transform, false);
        RectTransform rect = label.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset; text.alignment = TextAlignmentOptions.Center; text.fontSize = 28; text.color = new Color(.08f, .12f, .20f); text.raycastTarget = false;
        LetterCell cell = go.GetComponent<LetterCell>(); cell.Configure(text, image); cell.Initialize(x, y, answer, active); cell.OnCellSelected += SelectCell;
        return cell;
    }

    private void SelectCell(LetterCell cell)
    {
        if (_selectedCell != null) _selectedCell.SetSelected(false);
        _selectedCell = cell; _selectedCell.SetSelected(true);
        CrosswordQuestion question = GetQuestionAt(cell.X, cell.Y); if (question != null) OnQuestionSelected?.Invoke(question);
    }
    public bool TryInputLetter(char letter) { if (_selectedCell == null) return false; bool entered = _selectedCell.SetInput(letter); if (entered) { CheckCompletion(); MoveSelection(1, 0); } return entered; }
    public void DeleteLetter() { if (_selectedCell == null) return; _selectedCell.ClearInput(); MoveSelection(-1, 0); }
    private void MoveSelection(int dx, int dy)
    {
        int x = _selectedCell.X + dx, y = _selectedCell.Y + dy;
        while (x >= 0 && y >= 0 && x < _data.gridWidth && y < _data.gridHeight) { if (_cells[x, y].IsActive && !_cells[x, y].IsRevealed) { SelectCell(_cells[x, y]); return; } x += dx; y += dy; }
    }
    public void RevealWord(string id)
    {
        if (!_questionsById.TryGetValue(id, out CrosswordQuestion question)) return;
        int x = question.startX, y = question.startY; foreach (char _ in question.answer) { if (x < 0 || y < 0 || x >= _data.gridWidth || y >= _data.gridHeight) break; _cells[x, y].Reveal(); if (question.isHorizontal) x++; else y++; } CheckCompletion();
    }
    private void CheckCompletion()
    {
        if (_completed) return; foreach (LetterCell cell in _cells) if (cell != null && cell.IsActive && !cell.CheckCorrect()) return; _completed = true; OnPuzzleCompleted?.Invoke();
    }
    public CrosswordQuestion GetQuestionAt(int x, int y) { foreach (CrosswordQuestion question in _data.Questions) { int qx = question.startX, qy = question.startY; foreach (char _ in question.answer) { if (qx == x && qy == y) return question; if (question.isHorizontal) qx++; else qy++; } } return null; }
}
