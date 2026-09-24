using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

public class CrosswordGameController : MonoBehaviour
{
    [SerializeField] private CrosswordDataSO _dataSO;
    [SerializeField] private CrosswordGrid _grid;
    [SerializeField] private TMP_Text _questionText;
    [SerializeField] private TMP_Text _scoreText;
    
    private CrosswordData _currentData;
    private CrosswordQuestion _currentQuestion;
    private int _score = 0;
    
    private void Awake()
    {
        InputController.Instance.OnLetterInput += OnLetterInput;
        InputController.Instance.OnDelete += OnDelete;
        InputController.Instance.OnSubmit += OnSubmit;
        
        _grid.OnQuestionSelected += OnQuestionSelected;
        _grid.OnPuzzleCompleted += OnPuzzleCompleted;
    }
    
    private async void Start()
    {
        await InitializeGame();
    }
    
    private async UniTask InitializeGame()
    {
        _currentData = await _dataSO.LoadAsync();
        await _grid.Initialize(_currentData);
        
        _score = PlayerProfileManager.Profile.totalScore;
        UpdateScoreUI();
        
        if (_currentData.questions.Count > 0)
            OnQuestionSelected(_currentData.questions[0]);
    }
    
    private void OnQuestionSelected(CrosswordQuestion question)
    {
        _currentQuestion = question;
        _questionText.text = question.question;
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
        if (_currentQuestion != null)
        {
            _grid.RevealWord(_currentQuestion.id);
            _score += 50;
            UpdateScoreUI();
        }
    }
    
    private void OnPuzzleCompleted()
    {
        PlayerProfileManager.CompletePuzzle("crossword_01", _score);
        _questionText.text = "Поздравляем! Кроссворд решен!";
    }
    
    private void UpdateScoreUI()
    {
        _scoreText.text = $"Очки: {_score}";
    }
    
    private void OnDestroy()
    {
        InputController.Instance.OnLetterInput -= OnLetterInput;
        InputController.Instance.OnDelete -= OnDelete;
        InputController.Instance.OnSubmit -= OnSubmit;
        
        _grid.OnQuestionSelected -= OnQuestionSelected;
        _grid.OnPuzzleCompleted -= OnPuzzleCompleted;
    }
}