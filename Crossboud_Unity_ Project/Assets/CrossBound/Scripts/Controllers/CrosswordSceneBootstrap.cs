using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>Creates the UI Toolkit document for the crossword scene.</summary>
public static class CrosswordSceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateGameScreen()
    {
        if (SceneManager.GetActiveScene().name != "MainGameScene" || Object.FindFirstObjectByType<CrosswordGameController>() != null)
            return;

        // MainGameScene previously contained a prototype uGUI Canvas. The playable
        // screen is exclusively UI Toolkit, so do not render that legacy placeholder.
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            canvas.gameObject.SetActive(false);

        GameObject screen = new GameObject("Crossword UI Toolkit", typeof(UIDocument), typeof(CrosswordGameView), typeof(CrosswordGameController));
        UIDocument document = screen.GetComponent<UIDocument>();
        document.panelSettings = Resources.Load<PanelSettings>("UI/CrossBoundPanelSettings") ?? ScriptableObject.CreateInstance<PanelSettings>();
        screen.GetComponent<CrosswordGameView>().Configure(document);
        Debug.Log("[CrossBound][Gameplay] UI Toolkit document created.");
    }
}
