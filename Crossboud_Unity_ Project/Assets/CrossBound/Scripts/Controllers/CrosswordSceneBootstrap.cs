using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>Creates the UI Toolkit document for the crossword scene.</summary>
public static class CrosswordSceneBootstrap
{
    public const string GameSceneName = "MainGameScene";

    // RuntimeInitializeOnLoadMethod fires only once per play session — for the
    // initial scene. It covers pressing Play directly in MainGameScene; entering
    // via ProjectLoader (and scene restarts) is handled by CrosswordSceneWatcher.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateGameScreen() => EnsureGameScreen();

    /// <summary>Creates the game screen for the active scene when it is the game scene. Idempotent.</summary>
    public static void EnsureGameScreen()
    {
        string activeScene = SceneManager.GetActiveScene().name;
        Debug.Log($"[CrossBound][Bootstrap] EnsureGameScreen called. Active scene: '{activeScene}'.");
        if (activeScene != GameSceneName)
        {
            Debug.Log($"[CrossBound][Bootstrap] Active scene is not '{GameSceneName}' — nothing to do.");
            return;
        }
        if (Object.FindAnyObjectByType<CrosswordGameController>() != null)
        {
            // Expected when both the scene watcher and ProjectStart ask on the same load.
            Debug.Log("[CrossBound][Bootstrap] CrosswordGameController already exists — skipping screen creation.");
            return;
        }

        // MainGameScene previously contained a prototype uGUI Canvas. The playable
        // screen is exclusively UI Toolkit, so do not render that legacy placeholder.
        // Only canvases owned by the active scene are touched: overlays that survive
        // scene loads (the loading screen) live in the DontDestroyOnLoad scene and
        // must stay alive — deactivating one kills its timers and fade-out.
        Scene active = SceneManager.GetActiveScene();
        int disabled = 0;
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (canvas.gameObject.scene != active)
                continue;
            canvas.gameObject.SetActive(false);
            disabled++;
        }
        Debug.Log($"[CrossBound][Bootstrap] Disabled {disabled} legacy Canvas(es).");

        GameObject screen = new GameObject("Crossword UI Toolkit", typeof(UIDocument), typeof(CrosswordGameView), typeof(CrosswordGameController));
        UIDocument document = screen.GetComponent<UIDocument>();
        PanelSettings panelSettings = Resources.Load<PanelSettings>("UI/CrossBoundPanelSettings");
        if (panelSettings == null)
            Debug.LogWarning("[CrossBound][Bootstrap] PanelSettings asset NOT found in Resources/UI — creating a blank one at runtime; the UI will likely NOT render. Run CrossBound → Setup Project.");
        document.panelSettings = panelSettings ?? ScriptableObject.CreateInstance<PanelSettings>();
        screen.GetComponent<CrosswordGameView>().Configure(document);
        Debug.Log($"[CrossBound][Bootstrap] Game screen created on '{screen.name}' (PanelSettings: {(panelSettings != null ? "from Resources" : "blank runtime instance")}).");
    }
}
