using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Persistent loader bridge: shows the loading screen while the game scene is prepared, then enters gameplay.</summary>
public sealed class ProjectStart : MonoBehaviour
{
    [SerializeField] private string _startSceneName = "MainGameScene";
    private bool _started;
    private ProjectLoadingScreen _loadingScreen;
    private static bool _forwardingException; // guards against recursion inside the log handler

    private void Awake()
    {
        // Scene callbacks occur after ProjectLoader is unloaded. Keeping this small
        // bridge alive guarantees that Yandex gameplay and the game loop are started.
        DontDestroyOnLoad(gameObject);
        _loadingScreen = ProjectLoadingScreen.Create();
        Application.logMessageReceived += ForwardUnhandledExceptions;
    }

    private void Start()
    {
        Debug.Log($"[CrossBound][Startup] ProjectLoader started. Target scene: '{_startSceneName}'. Active scene: '{SceneManager.GetActiveScene().name}'.");
        bool checksOk = CrossBoundStartupChecks.Run(_startSceneName);
        Debug.Log($"[CrossBound][Startup] Startup checks: {(checksOk ? "PASSED" : "FAILED (see errors above)")}.");
        StartGame();
    }

    private void StartGame()
    {
        if (_started) return;
        _started = true;
        if (!Application.CanStreamedLevelBeLoaded(_startSceneName))
        {
            Debug.LogError($"[CrossBound][Startup] Cannot start game: scene '{_startSceneName}' is unavailable.");
            _loadingScreen?.ShowError($"Сцена '{_startSceneName}' не добавлена в Build Settings.");
            return;
        }

        SceneManager.sceneLoaded += OnStartSceneLoaded;
        Debug.Log($"[CrossBound][Startup] Loading '{_startSceneName}'.");
        StartCoroutine(LoadGameRoutine());
    }

    private IEnumerator LoadGameRoutine()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(_startSceneName, LoadSceneMode.Single);
        if (operation == null)
        {
            Debug.LogError($"[CrossBound][Startup] LoadSceneAsync returned null for '{_startSceneName}'.");
            _loadingScreen?.ShowError("Не удалось запустить загрузку сцены.");
            yield break;
        }

        // Hold the scene at the activation gate so the loading screen is always
        // visible for at least MinDisplaySeconds and progress reaches 100% first.
        operation.allowSceneActivation = false;
        float elapsed = 0f;
        float loggedMilestone = 0f;
        while (!operation.isDone)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            _loadingScreen?.SetProgress(progress);
            if (progress >= loggedMilestone + 0.25f)
            {
                loggedMilestone = progress;
                Debug.Log($"[CrossBound][Startup] Scene load progress: {Mathf.RoundToInt(progress * 100f)}% (elapsed {elapsed:0.0}s).");
            }
            if (operation.progress >= 0.9f && elapsed >= ProjectLoadingScreen.MinDisplaySeconds)
            {
                Debug.Log($"[CrossBound][Startup] Activation gate passed at {elapsed:0.0}s — activating '{_startSceneName}'.");
                operation.allowSceneActivation = true;
            }
            yield return null;
        }
        Debug.Log("[CrossBound][Startup] LoadGameRoutine finished (scene active).");
    }

    private void OnStartSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != _startSceneName) return;
        SceneManager.sceneLoaded -= OnStartSceneLoaded;
        Debug.Log($"[CrossBound][Startup] '{scene.name}' loaded. Entering gameplay loop.");

        YandexGameService yandex = YandexGameInitializer.Instance?.GameService;
        yandex?.MarkGameReady();
        yandex?.StartGameplay();

        // 100% + "ГОТОВО"; the screen fades out once the crossword grid is built.
        _loadingScreen?.MarkSceneActivated();
        Destroy(gameObject);
    }

    /// <summary>Re-logs unhandled exceptions with the project prefix so a crash anywhere in the chain is impossible to miss.</summary>
    private static void ForwardUnhandledExceptions(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Exception || _forwardingException) return;
        _forwardingException = true;
        Debug.LogError($"[CrossBound][Unhandled] {condition}\n{stackTrace}");
        _forwardingException = false;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnStartSceneLoaded;
        Application.logMessageReceived -= ForwardUnhandledExceptions;
    }
}
