using System;
using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

public class CrosswordGrid : MonoBehaviour
{
    [SerializeField] private GameObject _letterPortPrefab;
    [SerializeField] private Transform _gridContainer;
    [SerializeField] private float _cellSize = 50f;
    
    private LetterCell[,] _cells;
    private CrosswordData _data;
    private LetterCell _selectedCell;
    private Dictionary<string, CrosswordQuestion> _questionsById;
    
    public event Action OnPuzzleCompleted;
    public event Action<CrosswordQuestion> OnQuestionSelected;

    public LetterCell GetCell(int x, int y) => _cells[x, y];

    public async UniTask Initialize(CrosswordData data)
    {
        _data = data;
        _questionsById = new();
        
        foreach (var q in data.questions)
            _questionsById[q.id] = q;
        
        _cells = new LetterCell[data.gridWidth, data.gridHeight];
        await BuildGrid();
    }
    
    private async UniTask BuildGrid()
    {
        var prefab = _letterPortPrefab;
        if (prefab == null)
        {
            var handle = Addressables.LoadAssetAsync<GameObject>("LetterPort");
            await handle.Task;
            prefab = handle.Result;
        }
        
        foreach (var q in _data.questions)
        {
            int x = q.startX;
            int y = q.startY;
            
            foreach (char c in q.answer)
            {
                if (x >= _data.gridWidth || y >= _data.gridHeight) break;
                
                if (_cells[x, y] == null)
                {
                    var go = Instantiate(prefab, _gridContainer);
                    go.transform.localPosition = new Vector3(x * _cellSize, -y * _cellSize, 0);
                    var cell = go.GetComponent<LetterCell>() ?? go.AddComponent<LetterCell>();
                    cell.Initialize(x, y, c, true);
                    cell.OnCellSelected += SelectCell;
                    _cells[x, y] = cell;
                }
                
                if (q.isHorizontal) x++; else y++;
            }
        }
        
        for (int x = 0; x < _data.gridWidth; x++)
        for (int y = 0; y < _data.gridHeight; y++)
            if (_cells[x, y] == null)
            {
                var go = Instantiate(prefab, _gridContainer);
                go.transform.localPosition = new Vector3(x * _cellSize, -y * _cellSize, 0);
                var cell = go.GetComponent<LetterCell>() ?? go.AddComponent<LetterCell>();
                cell.Initialize(x, y, ' ', false);
                _cells[x, y] = cell;
            }
    }
    
    private void SelectCell(LetterCell cell)
    {
        _selectedCell = cell;
        var q = GetQuestionAt(cell.X, cell.Y);
        if (q != null) OnQuestionSelected?.Invoke(q);
    }
    
    public bool TryInputLetter(char letter)
    {
        if (_selectedCell == null) return false;
        bool result = _selectedCell.SetInput(letter);
        if (result) MoveSelection(1, 0);
        return result;
    }
    
    public void DeleteLetter()
    {
        if (_selectedCell == null) return;
        _selectedCell.ClearInput();
        MoveSelection(-1, 0);
    }
    
    private void MoveSelection(int dx, int dy)
    {
        int x = _selectedCell.X + dx;
        int y = _selectedCell.Y + dy;
        
        while (x >= 0 && x < _data.gridWidth && y >= 0 && y < _data.gridHeight)
        {
            if (_cells[x, y].IsActive && !_cells[x, y].IsRevealed)
            {
                SelectCell(_cells[x, y]);
                return;
            }
            x += dx; y += dy;
        }
    }
    
    public void RevealWord(string questionId)
    {
        if (!_questionsById.TryGetValue(questionId, out var q)) return;
        
        int x = q.startX;
        int y = q.startY;
        
        foreach (char c in q.answer)
        {
            if (x >= _data.gridWidth || y >= _data.gridHeight) break;
            _cells[x, y]?.Reveal();
            if (q.isHorizontal) x++; else y++;
        }
    }
    
    public bool CheckCompletion()
    {
        foreach (var q in _data.questions)
        {
            int x = q.startX;
            int y = q.startY;
            
            foreach (char c in q.answer)
            {
                if (x >= _data.gridWidth || y >= _data.gridHeight) break;
                if (!_cells[x, y].CheckCorrect()) return false;
                if (q.isHorizontal) x++; else y++;
            }
        }
        
        OnPuzzleCompleted?.Invoke();
        return true;
    }
    
    public CrosswordQuestion GetQuestionAt(int x, int y)
    {
        foreach (var q in _data.questions)
        {
            int qx = q.startX;
            int qy = q.startY;
            
            foreach (char c in q.answer)
            {
                if (qx == x && qy == y) return q;
                if (q.isHorizontal) qx++; else qy++;
            }
        }
        return null;
    }
    
   
}