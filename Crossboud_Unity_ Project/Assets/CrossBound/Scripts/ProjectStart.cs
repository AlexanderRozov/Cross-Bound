using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Persistent loader bridge: verifies bootstrap dependencies and enters the game scene once.</summary>
public sealed class ProjectStart : MonoBehaviour
{
    [SerializeField] private string _startSceneName = "MainGameScene";
    private bool _started;

    private void Awake()
    {
        // Scene callbacks occur after ProjectLoader is unloaded. Keeping this small
        // bridge alive guarantees that Yandex gameplay and the game loop are started.
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Debug.Log($"[CrossBound][Startup] ProjectLoader started. Target scene: {_startSceneName}.");
        CrossBoundStartupChecks.Run(_startSceneName);
        StartGame();
    }

    private void StartGame()
    {
        if (_started) return;
        _started = true;
        if (!Application.CanStreamedLevelBeLoaded(_startSceneName))
        {
            Debug.LogError($"[CrossBound][Startup] Cannot start game: scene '{_startSceneName}' is unavailable.");
            return;
        }

        SceneManager.sceneLoaded += OnStartSceneLoaded;
        Debug.Log($"[CrossBound][Startup] Loading '{_startSceneName}'.");
        SceneManager.LoadSceneAsync(_startSceneName, LoadSceneMode.Single);
    }

    private void OnStartSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != _startSceneName) return;
        SceneManager.sceneLoaded -= OnStartSceneLoaded;
        Debug.Log($"[CrossBound][Startup] '{scene.name}' loaded. Entering gameplay loop.");

        YandexGameService yandex = YandexGameInitializer.Instance?.GameService;
        yandex?.MarkGameReady();
        yandex?.StartGameplay();
        Destroy(gameObject);
    }

    private void OnDestroy() => SceneManager.sceneLoaded -= OnStartSceneLoaded;
}
