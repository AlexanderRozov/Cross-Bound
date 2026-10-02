using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Thin adapter between input, the pure <see cref="CrosswordGameState"/> and the <see cref="CrosswordGameView"/>.</summary>
public sealed class CrosswordGameController : MonoBehaviour
{
    private const string PuzzleId = "crossword_01";

    /// <summary>Raised once per scene entry after the puzzle is loaded and the grid is built; the loading screen listens to reveal the game.</summary>
    public static event Action GameReady;

    private CrosswordGameView _view;
    private CrosswordGameState _state;

    public void Configure(CrosswordGameView view) => _view = view;

    private void Awake()
    {
        _view ??= GetComponent<CrosswordGameView>();
        Debug.Log($"[CrossBound][Gameplay] Controller Awake on '{gameObject.name}'. View: {(_view != null ? "found" : "MISSING")}. InputController.Instance: {(InputController.Instance != null ? "ready" : "MISSING")}.");
        if (InputController.Instance == null) new GameObject("InputController").AddComponent<InputController>();
        InputController.Instance.OnLetterInput += HandleLetterInput;
        InputController.Instance.OnDelete += HandleDelete;
        InputController.Instance.OnSubmit += HandleCheck;
        InputController.Instance.OnNavigate += HandleNavigate;
    }

    // async void Start silently swallows exceptions — route through a guarded runner instead.
    private void Start() => RunInitializationAsync().Forget();

    private async UniTask RunInitializationAsync()
    {
        try
        {
            await InitializeAsync();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[CrossBound][Gameplay] FATAL: crossword initialization crashed: {exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}");
        }
    }

    private async UniTask InitializeAsync()
    {
        Debug.Log("[CrossBound][Gameplay] Step 1/6: loading crossword content...");
        CrosswordData data = await new CrosswordContentLoader().LoadAsync("CrosswordQuestions", "CrosswordQuestions");
        Debug.Log($"[CrossBound][Gameplay] Step 2/6: content loaded. Questions: {(data?.Questions?.Count ?? -1)}, grid: {data?.gridWidth ?? -1}x{data?.gridHeight ?? -1}.");

        List<string> errors = CrosswordDataValidator.Validate(data);
        if (errors.Count > 0)
            Debug.LogError("[CrossBound][Gameplay] Step 3/6: data validation FAILED:\n - " + string.Join("\n - ", errors));
        else
            Debug.Log("[CrossBound][Gameplay] Step 3/6: data validation passed.");

        _state = new CrosswordGameState();
        _state.Initialize(data);
        _state.Completed += OnPuzzleCompleted;
        Debug.Log($"[CrossBound][Gameplay] Step 4/6: game state initialized ({_state.Width}x{_state.Height}).");

        Debug.Log("[CrossBound][Gameplay] Step 5/6: building view...");
        bool built = _view.Build(_state,
            (x, y) => _state.SelectCell(x, y),
            () => _state.DeleteLetter(),
            () => _state.RevealCurrentWord(),
            () => HandleCheck(),
            RestartPuzzle);
        if (!built)
        {
            Debug.LogError("[CrossBound][Gameplay] FATAL: view build failed — the crossword cannot be shown.");
            return;
        }
        Debug.Log("[CrossBound][Gameplay] Step 6/6: view built.");

        if (data.Questions.Count > 0)
            _state.SelectQuestion(data.Questions[0]);

        Debug.Log($"[CrossBound][Gameplay] Crossword initialized: {data.Questions.Count} questions, {_state.Width}x{_state.Height} grid.");
        GameReady?.Invoke();
        Debug.Log("[CrossBound][Gameplay] GameReady fired — the loading screen will fade out.");
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
