using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>Game state only; all rendering is delegated to CrosswordGameView (UI Toolkit).</summary>
public sealed class CrosswordGameController : MonoBehaviour
{
    private CrosswordGameView _view;
    private CrosswordData _data;
    private CrosswordQuestion _currentQuestion;
    private char[,] _answers;
    private char[,] _inputs;
    private int _selectedX = -1, _selectedY = -1, _score;
    private bool _completed;

    public void Configure(CrosswordGameView view) => _view = view;

    private void Awake()
    {
        _view ??= GetComponent<CrosswordGameView>();
        if (InputController.Instance == null) new GameObject("InputController").AddComponent<InputController>();
        InputController.Instance.OnLetterInput += InputLetter;
        InputController.Instance.OnDelete += DeleteLetter;
    }

    private async void Start() => await InitializeAsync();

    private async UniTask InitializeAsync()
    {
        _data = await new CrosswordContentLoader().LoadAsync("CrosswordQuestions", "CrosswordQuestions");
        _answers = new char[_data.gridWidth, _data.gridHeight];
        _inputs = new char[_data.gridWidth, _data.gridHeight];
        foreach (CrosswordQuestion question in _data.Questions)
        {
            int x = question.startX, y = question.startY;
            foreach (char letter in question.answer.ToUpperInvariant())
            {
                if (x < 0 || y < 0 || x >= _data.gridWidth || y >= _data.gridHeight) break;
                _answers[x, y] = letter;
                if (question.isHorizontal) x++; else y++;
            }
        }
        _view.Build(_data, SelectCell, DeleteLetter, RevealCurrentWord);
        if (_data.Questions.Count > 0) SelectQuestion(_data.Questions[0]);
        Debug.Log($"[CrossBound][Gameplay] Test crossword initialized: {_data.Questions.Count} questions, {_data.gridWidth}x{_data.gridHeight} grid.");
    }

    private void SelectCell(int x, int y)
    {
        if (_answers[x, y] == '\0') return;
        _selectedX = x; _selectedY = y;
        foreach (CrosswordQuestion question in _data.Questions)
            if (Contains(question, x, y)) { SelectQuestion(question); break; }
        _view.SetSelected(x, y);
    }

    private void SelectQuestion(CrosswordQuestion question)
    {
        _currentQuestion = question;
        _view.SetQuestion(question.number > 0 ? question.number : GetQuestionNumber(question), question.question, _score);
    }

    private void InputLetter(char letter)
    {
        if (_selectedX < 0 || _answers[_selectedX, _selectedY] == '\0' || _completed) return;
        _inputs[_selectedX, _selectedY] = char.ToUpperInvariant(letter);
        if (_inputs[_selectedX, _selectedY] == _answers[_selectedX, _selectedY]) _score += 10;
        _view.SetCell(_selectedX, _selectedY, _inputs[_selectedX, _selectedY], _inputs[_selectedX, _selectedY] == _answers[_selectedX, _selectedY]);
        _view.SetScore(_score);
        MoveForward(); CheckCompletion();
    }

    private void DeleteLetter()
    {
        if (_selectedX < 0 || _completed) return;
        _inputs[_selectedX, _selectedY] = '\0'; _view.SetCell(_selectedX, _selectedY, '\0', false);
    }

    private void RevealCurrentWord()
    {
        if (_currentQuestion == null || _completed) return;
        int x = _currentQuestion.startX, y = _currentQuestion.startY;
        foreach (char letter in _currentQuestion.answer.ToUpperInvariant())
        {
            _inputs[x, y] = letter; _view.SetCell(x, y, letter, true);
            if (_currentQuestion.isHorizontal) x++; else y++;
        }
        CheckCompletion();
    }

    private void MoveForward() { if (_currentQuestion == null) return; int x = _selectedX, y = _selectedY; if (_currentQuestion.isHorizontal) x++; else y++; if (x >= 0 && y >= 0 && x < _data.gridWidth && y < _data.gridHeight && _answers[x, y] != '\0') SelectCell(x, y); }
    private int GetQuestionNumber(CrosswordQuestion question) { for (int index = 0; index < _data.Questions.Count; index++) if (ReferenceEquals(_data.Questions[index], question)) return index + 1; return 1; }
    private bool Contains(CrosswordQuestion q, int x, int y) { int qx = q.startX, qy = q.startY; foreach (char _ in q.answer) { if (qx == x && qy == y) return true; if (q.isHorizontal) qx++; else qy++; } return false; }
    private void CheckCompletion() { for (int y = 0; y < _data.gridHeight; y++) for (int x = 0; x < _data.gridWidth; x++) if (_answers[x, y] != '\0' && _answers[x, y] != _inputs[x, y]) return; _completed = true; PlayerProfileManager.CompletePuzzle("crossword_01", _score); _view.ShowCompleted(_score); Debug.Log($"[CrossBound][Gameplay] Crossword completed. Score={_score}."); }
    private void OnDestroy() { if (InputController.Instance == null) return; InputController.Instance.OnLetterInput -= InputLetter; InputController.Instance.OnDelete -= DeleteLetter; }
}
