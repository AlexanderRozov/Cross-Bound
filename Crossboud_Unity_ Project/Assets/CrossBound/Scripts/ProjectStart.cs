using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ProjectStart : MonoBehaviour
{
    [SerializeField] private string _startSceneName = "MainGameScene";

    private bool _started;

    private void Start()
    {
        StartGame();
    }

    private void StartGame()
    {
        if (_started)
            return;

        _started = true;
        SceneManager.sceneLoaded += OnStartSceneLoaded;
        SceneController.Instance.LoadSceneByName(_startSceneName);
    }

    private void OnStartSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != _startSceneName)
            return;

        SceneManager.sceneLoaded -= OnStartSceneLoaded;

        YandexGameService yandex = YandexGameInitializer.Instance?.GameService;
        yandex?.MarkGameReady();
        yandex?.StartGameplay();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnStartSceneLoaded;
    }
}
