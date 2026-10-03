using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// RuntimeInitializeOnLoadMethod never fires for scenes loaded later in the
/// session, so this watcher covers entering MainGameScene through the loader and
/// every restart. Created once by ProjectStart and kept across scene loads.
/// </summary>
public sealed class CrosswordSceneWatcher : MonoBehaviour
{
    public static CrosswordSceneWatcher Create()
    {
        if (Object.FindAnyObjectByType<CrosswordSceneWatcher>() != null)
            return null;
        return new GameObject("CrossBoundSceneWatcher").AddComponent<CrosswordSceneWatcher>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == CrosswordSceneBootstrap.GameSceneName)
            CrosswordSceneBootstrap.EnsureGameScreen();
    }

    private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;
}
