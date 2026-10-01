using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;

public class CrosswordGameController : MonoBehaviour
{
    [SerializeField] private CrosswordDataSO _dataSO;
    [SerializeField] private CrosswordGrid _grid;
    [SerializeField] private TMP_Text _questionText;
    [SerializeField] private TMP_Text _scoreText;
    
    private CrosswordData _currentData;
    private CrosswordQuestion _currentQuestion;
    private int _score = 0;

    public void Configure(CrosswordGrid grid, TMP_Text questionText, TMP_Text scoreText)
    {
        _grid = grid;
        _questionText = questionText;
        _scoreText = scoreText;
    }
    
    private void Awake()
    {
        if (InputController.Instance == null)
            new GameObject("InputController").AddComponent<InputController>();

        InputController.Instance.OnLetterInput += OnLetterInput;
        InputController.Instance.OnDelete += OnDelete;
        
        if (_grid == null) throw new System.InvalidOperationException("CrosswordGameController requires a CrosswordGrid.");
        _grid.OnQuestionSelected += OnQuestionSelected;
        _grid.OnPuzzleCompleted += OnPuzzleCompleted;
    }
    
    private async void Start()
    {
        await InitializeGame();
    }
    
    private async UniTask InitializeGame()
    {
        _currentData = _dataSO != null
            ? await _dataSO.LoadAsync()
            : await new CrosswordContentLoader().LoadAsync("CrosswordQuestions", "CrosswordQuestions");
        await _grid.Initialize(_currentData);
        
        _score = 0;
        UpdateScoreUI();
        
        if (_currentData.Questions.Count > 0)
            OnQuestionSelected(_currentData.Questions[0]);
    }
    
    private void OnQuestionSelected(CrosswordQuestion question)
    {
        _currentQuestion = question;
        if (_questionText != null) _questionText.text = question.question;
    }
    
    private void OnLetterInput(char letter)
    {
        if (_grid.TryInputLetter(letter))
            _score += 10;
        UpdateScoreUI();
    }
    
    private void OnDelete()
    {
        _grid.DeleteLetter();
    }
    
    private void OnSubmit()
    {
        // Enter checks the current progress. A reveal is deliberately not bound to
        // a keyboard key so answers cannot be exposed accidentally.
    }
    
    private void OnPuzzleCompleted()
    {
        PlayerProfileManager.CompletePuzzle("crossword_01", _score);
        if (_questionText != null) _questionText.text = "Поздравляем! Кроссворд решен!";
    }
    
    private void UpdateScoreUI()
    {
        if (_scoreText != null) _scoreText.text = $"Очки: {_score}";
    }
    
    private void OnDestroy()
    {
        if (InputController.Instance != null)
        {
            InputController.Instance.OnLetterInput -= OnLetterInput;
            InputController.Instance.OnDelete -= OnDelete;
            InputController.Instance.OnSubmit -= OnSubmit;
        }
        
        _grid.OnQuestionSelected -= OnQuestionSelected;
        _grid.OnPuzzleCompleted -= OnPuzzleCompleted;
    }
}
