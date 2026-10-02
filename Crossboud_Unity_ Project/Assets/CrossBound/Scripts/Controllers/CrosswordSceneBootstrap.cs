using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>Creates the UI Toolkit document for the crossword scene.</summary>
public static class CrosswordSceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateGameScreen()
    {
        string activeScene = SceneManager.GetActiveScene().name;
        Debug.Log($"[CrossBound][Bootstrap] RuntimeInitializeOnLoadMethod fired. Active scene: '{activeScene}'.");
        if (activeScene != "MainGameScene")
            return;
        if (Object.FindAnyObjectByType<CrosswordGameController>() != null)
        {
            Debug.LogWarning("[CrossBound][Bootstrap] CrosswordGameController already exists — skipping screen creation.");
            return;
        }

        // MainGameScene previously contained a prototype uGUI Canvas. The playable
        // screen is exclusively UI Toolkit, so do not render that legacy placeholder.
        Canvas[] legacyCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (Canvas canvas in legacyCanvases)
            canvas.gameObject.SetActive(false);
        Debug.Log($"[CrossBound][Bootstrap] Disabled {legacyCanvases.Length} legacy Canvas(es).");

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
