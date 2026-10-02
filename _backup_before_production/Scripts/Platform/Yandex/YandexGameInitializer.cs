using UnityEngine;

/// <summary>
/// Creates the Yandex platform facade once for the whole application.
/// Add this component to the bootstrap scene before scenes that use gameplay.
/// </summary>
public sealed class YandexGameInitializer : MonoBehaviour
{
    private bool _resumeGameplayAfterPause;

    public static YandexGameInitializer Instance { get; private set; }
    public YandexGameService GameService { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        GameService = new YandexGameService();
        GameService.Initialize();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            _resumeGameplayAfterPause = GameService.IsGameplayActive;
            GameService.StopGameplay();
        }
        else if (_resumeGameplayAfterPause)
        {
            GameService.StartGameplay();
            _resumeGameplayAfterPause = false;
        }
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        GameService.Dispose();
        Instance = null;
    }
}
