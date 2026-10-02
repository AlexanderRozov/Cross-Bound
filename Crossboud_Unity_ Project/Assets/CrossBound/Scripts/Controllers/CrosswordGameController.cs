using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Thin adapter between input, the pure <see cref="CrosswordGameState"/> and the <see cref="CrosswordGameView"/>.</summary>
public sealed class CrosswordGameController : MonoBehaviour
{
    private const string PuzzleId = "crossword_01";

    private CrosswordGameView _view;
    private CrosswordGameState _state;

    public void Configure(CrosswordGameView view) => _view = view;

    private void Awake()
    {
        _view ??= GetComponent<CrosswordGameView>();
        if (InputController.Instance == null) new GameObject("InputController").AddComponent<InputController>();
        InputController.Instance.OnLetterInput += HandleLetterInput;
        InputController.Instance.OnDelete += HandleDelete;
        InputController.Instance.OnSubmit += HandleCheck;
        InputController.Instance.OnNavigate += HandleNavigate;
    }

    private async void Start() => await InitializeAsync();

    private async UniTask InitializeAsync()
    {
        CrosswordData data = await new CrosswordContentLoader().LoadAsync("CrosswordQuestions", "CrosswordQuestions");

        List<string> errors = CrosswordDataValidator.Validate(data);
        if (errors.Count > 0)
            Debug.LogError("[CrossBound][Gameplay] Invalid crossword data:\n - " + string.Join("\n - ", errors));

        _state = new CrosswordGameState();
        _state.Initialize(data);
        _state.Completed += OnPuzzleCompleted;

        _view.Build(_state,
            (x, y) => _state.SelectCell(x, y),
            () => _state.DeleteLetter(),
            () => _state.RevealCurrentWord(),
            () => HandleCheck(),
            RestartPuzzle);

        if (data.Questions.Count > 0)
            _state.SelectQuestion(data.Questions[0]);

        Debug.Log($"[CrossBound][Gameplay] Crossword initialized: {data.Questions.Count} questions, {_state.Width}x{_state.Height} grid.");
    }

    private void HandleLetterInput(char letter) => _state?.InputLetter(letter);
    private void HandleDelete() => _state?.DeleteLetter();
    private void HandleCheck() => _state?.CheckCurrentWord();

    private void HandleNavigate(int dx, int dy)
    {
        if (_state == null || _state.SelectedX < 0) return;
        int x = _state.SelectedX + dx, y = _state.SelectedY + dy;
        while (x >= 0 && y >= 0 && x < _state.Width && y < _state.Height)
        {
            if (_state.HasAnswer(x, y)) { _state.SelectCell(x, y); return; }
            x += dx; y += dy;
        }
    }

    private void OnPuzzleCompleted(int score)
    {
        PlayerProfileManager.CompletePuzzle(PuzzleId, score);
        int bestScore = PlayerProfileManager.Profile.bestScore;
        _view.ShowCompleted(score, bestScore);
        Debug.Log($"[CrossBound][Gameplay] Puzzle completed with {score} points (best: {bestScore}).");
    }

    private void RestartPuzzle()
    {
        // ProjectStart no longer exists after the initial load, so gameplay must be re-reported here.
        YandexGameInitializer.Instance?.GameService?.StartGameplay();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (InputController.Instance == null) return;
        InputController.Instance.OnLetterInput -= HandleLetterInput;
        InputController.Instance.OnDelete -= HandleDelete;
        InputController.Instance.OnSubmit -= HandleCheck;
        InputController.Instance.OnNavigate -= HandleNavigate;
    }
}
