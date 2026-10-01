using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Builds the responsive, UGUI-based crossword scene so it works in a clean checkout and WebGL.</summary>
public static class CrosswordSceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateGameScreen()
    {
        if (SceneManager.GetActiveScene().name != "MainGameScene" || Object.FindFirstObjectByType<CrosswordGameController>() != null) return;

        GameObject canvasObject = new GameObject("Crossword UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
        Image backdrop = canvasObject.AddComponent<Image>(); backdrop.color = new Color(.07f, .10f, .17f);

        RectTransform root = CreatePanel("Content", canvasObject.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(1120, 650));
        TextMeshProUGUI title = CreateText("Title", root, "CROSS-BOUND", 42, TextAlignmentOptions.Center); SetRect(title.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -42), new Vector2(700, 60));
        TextMeshProUGUI score = CreateText("Score", root, "Очки: 0", 26, TextAlignmentOptions.Right); SetRect(score.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -42), new Vector2(240, 50));
        TextMeshProUGUI question = CreateText("Question", root, "Загрузка кроссворда…", 26, TextAlignmentOptions.Left); question.enableWordWrapping = true; SetRect(question.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 66), new Vector2(980, 80));
        TextMeshProUGUI help = CreateText("Help", root, "Выберите клетку и введите буквы. Backspace — удалить.", 18, TextAlignmentOptions.Center); help.color = new Color(.62f, .70f, .82f); SetRect(help.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 24), new Vector2(980, 34));
        RectTransform gridPanel = CreatePanel("Grid", root, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(650, 500)); gridPanel.anchoredPosition = new Vector2(0, -10);

        GameObject game = new GameObject("Crossword Game", typeof(CrosswordGrid), typeof(CrosswordGameController)); game.transform.SetParent(root, false);
        CrosswordGrid grid = game.GetComponent<CrosswordGrid>(); grid.Configure(gridPanel);
        game.GetComponent<CrosswordGameController>().Configure(grid, question, score);
    }

    private static RectTransform CreatePanel(string name, Transform parent, Vector2 min, Vector2 max, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); RectTransform rect = go.GetComponent<RectTransform>(); SetRect(rect, min, max, Vector2.zero, size); return rect;
    }
    private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float size, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false); TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset; text.text = value; text.fontSize = size; text.alignment = alignment; text.color = Color.white; return text;
    }
    private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 position, Vector2 size) { rect.anchorMin = min; rect.anchorMax = max; rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = size; }
}
