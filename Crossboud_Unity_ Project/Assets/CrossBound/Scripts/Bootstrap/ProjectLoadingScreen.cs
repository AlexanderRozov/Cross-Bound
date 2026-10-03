using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Loading screen shown while the game scene is being prepared. The whole layout
/// is built from code so the ProjectLoader scene stays asset-free; the palette
/// mirrors CrosswordGame.uss. Keeps itself visible until
/// <see cref="CrosswordGameController"/> reports the puzzle is built, then fades
/// out and disposes itself.
/// </summary>
public sealed class ProjectLoadingScreen : MonoBehaviour
{
    public const float MinDisplaySeconds = 1.6f; // never flash by faster than this

    private const float FadeSeconds = 0.5f;
    private const float RevealTimeoutSeconds = 8f; // fades anyway if GameReady never arrives
    private const float TipRotationSeconds = 3.5f;
    private const float BarWidth = 560f;

    private static readonly Color BackgroundColor = new Color32(0x12, 0x1B, 0x2D, 0xFF);
    private static readonly Color TrackColor = new Color32(0x0D, 0x15, 0x24, 0xFF);
    private static readonly Color AccentColor = new Color32(0xFF, 0xCF, 0x50, 0xFF);
    private static readonly Color TextColor = Color.white;
    private static readonly Color CellLetterColor = new Color32(0x16, 0x22, 0x37, 0xFF);
    private static readonly Color SecondaryColor = new Color32(0xAE, 0xBE, 0xD9, 0xFF);
    private static readonly Color ErrorColor = new Color32(0xFF, 0x8A, 0x80, 0xFF);

    private static readonly string[] Tips =
    {
        "Нажмите на клетку ещё раз, чтобы сменить направление слова",
        "Стрелки на клавиатуре перемещают выделение по сетке",
        "Подсказка открывает слово целиком, но стоит 30 очков",
        "Верные буквы фиксируются — их нельзя стереть",
        "Клавиша Enter проверяет текущее слово",
    };

    private CanvasGroup _canvasGroup;
    private Text _status, _percent, _tip;
    private CanvasGroup _tipGroup;
    private RectTransform _barFill;
    private float _dotTimer;
    private int _dotCount;
    private float _tipTimer; // first rotation after a full interval; Tips[0] is already the initial text
    private int _tipIndex;
    private bool _error, _activated, _fading;
    private float _revealTimer;
    private int _tipVersion; // bumped per rotation so an in-flight fade cancels itself
    private float _loggedMilestone;

    public static ProjectLoadingScreen Create()
    {
        GameObject holder = new GameObject("ProjectLoadingScreen");
        DontDestroyOnLoad(holder);
        ProjectLoadingScreen screen = holder.AddComponent<ProjectLoadingScreen>();
        Debug.Log("[CrossBound][Loader] Loading screen created.");
        return screen;
    }

    private void Awake()
    {
        BuildUi();
        CrosswordGameController.GameReady += HandleGameReady;
    }

    /// <summary>0..1; drives the bar fill and the percent label.</summary>
    public void SetProgress(float value)
    {
        if (_error) return;
        value = Mathf.Clamp01(value);
        _barFill.sizeDelta = new Vector2(BarWidth * value, _barFill.sizeDelta.y);
        _percent.text = Mathf.RoundToInt(value * 100f) + "%";
        if (value >= _loggedMilestone + 0.25f)
        {
            _loggedMilestone = value;
            Debug.Log($"[CrossBound][Loader] Progress: {Mathf.RoundToInt(value * 100f)}%");
        }
    }

    /// <summary>Called once the game scene is activated; waits for the puzzle to build, then fades.</summary>
    public void MarkSceneActivated()
    {
        if (_activated || _error) return;
        _activated = true;
        SetProgress(1f);
        _status.text = "ГОТОВО";
        _revealTimer = RevealTimeoutSeconds;
        Debug.Log("[CrossBound][Loader] Game scene activated. Waiting for the crossword to build (failsafe fade in 8s)...");
    }

    /// <summary>Fatal startup failure: stop progress animation and show the reason.</summary>
    public void ShowError(string message)
    {
        _error = true;
        _status.color = ErrorColor;
        _status.text = "ОШИБКА ЗАГРУЗКИ";
        _percent.text = string.Empty;
        _tip.text = message;
        _tipGroup.alpha = 1f;
        Debug.LogError($"[CrossBound][Loader] {message}");
    }

    private void Update()
    {
        if (_fading || _error) return;

        if (!_activated)
        {
            _dotTimer += Time.unscaledDeltaTime;
            if (_dotTimer >= 0.4f)
            {
                _dotTimer = 0f;
                _dotCount = (_dotCount + 1) % 4;
                _status.text = "ЗАГРУЗКА" + new string('.', _dotCount);
            }
        }
        else
        {
            _revealTimer -= Time.unscaledDeltaTime;
            if (_revealTimer <= 0f)
            {
                Debug.LogWarning("[CrossBound][Loader] GameReady never arrived — fading out via failsafe timeout (the game scene may be broken).");
                BeginFadeOut();
            }
        }

        _tipTimer += Time.unscaledDeltaTime;
        if (_tipTimer >= TipRotationSeconds)
        {
            _tipTimer = 0f;
            _tipIndex = (_tipIndex + 1) % Tips.Length;
            RotateTip(Tips[_tipIndex]);
        }
    }

    private void HandleGameReady()
    {
        if (this == null || _error) return;
        Debug.Log("[CrossBound][Loader] GameReady received — fading out.");
        BeginFadeOut();
    }

    private void BeginFadeOut()
    {
        if (_fading) return;
        _fading = true;
        _ = FadeOutAsync();
    }

    private async UniTaskVoid FadeOutAsync()
    {
        for (float t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime)
        {
            _canvasGroup.alpha = 1f - t / FadeSeconds;
            await UniTask.Yield();
        }
        _canvasGroup.alpha = 0f;
        Debug.Log("[CrossBound][Loader] Loading screen destroyed — the game scene should be visible now.");
        Destroy(gameObject);
    }

    private void RotateTip(string text) => RotateTipAsync(text, ++_tipVersion).Forget();

    private async UniTask RotateTipAsync(string text, int version)
    {
        await FadeTip(0f, version);
        if (version != _tipVersion) return;
        _tip.text = text;
        await FadeTip(1f, version);
    }

    private async UniTask FadeTip(float target, int version)
    {
        float start = _tipGroup.alpha;
        for (float t = 0f; t < 0.15f && version == _tipVersion; t += Time.unscaledDeltaTime)
        {
            _tipGroup.alpha = Mathf.Lerp(start, target, t / 0.15f);
            await UniTask.Yield();
        }
        if (version == _tipVersion) _tipGroup.alpha = target;
    }

    private void OnDestroy()
    {
        CrosswordGameController.GameReady -= HandleGameReady;
    }

    // ----------------------------- Layout -----------------------------

    private void BuildUi()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        Image background = CreateImage("Background", transform, BackgroundColor);
        Stretch(background.rectTransform);

        BuildMiniGrid(font);
        CreateText("Title", transform, font, "CROSSBOUND", 64, TextColor, true, new Vector2(0f, 40f), new Vector2(1200f, 90f));
        _status = CreateText("Status", transform, font, "ЗАГРУЗКА", 26, SecondaryColor, false, new Vector2(0f, -30f), new Vector2(800f, 40f));

        RectTransform track = CreateImage("ProgressTrack", transform, TrackColor).rectTransform;
        track.anchorMin = track.anchorMax = new Vector2(0.5f, 0.5f);
        track.pivot = new Vector2(0.5f, 0.5f);
        track.anchoredPosition = new Vector2(0f, -100f);
        track.sizeDelta = new Vector2(BarWidth, 22f);

        _barFill = CreateImage("ProgressFill", track, AccentColor).rectTransform;
        _barFill.anchorMin = new Vector2(0f, 0f);
        _barFill.anchorMax = new Vector2(0f, 1f);
        _barFill.pivot = new Vector2(0f, 0.5f);
        _barFill.sizeDelta = new Vector2(0f, 0f);

        _percent = CreateText("Percent", transform, font, "0%", 24, AccentColor, true, new Vector2(0f, -150f), new Vector2(200f, 36f));

        _tip = CreateText("Tip", transform, font, Tips[0], 20, SecondaryColor, false, new Vector2(0f, 0f), new Vector2(1500f, 60f));
        RectTransform tipRect = _tip.rectTransform;
        tipRect.anchorMin = new Vector2(0.5f, 0f);
        tipRect.anchorMax = new Vector2(0.5f, 0f);
        tipRect.pivot = new Vector2(0.5f, 0f);
        tipRect.anchoredPosition = new Vector2(0f, 48f);
        _tipGroup = tipRect.gameObject.AddComponent<CanvasGroup>();

        SetProgress(0f);
    }

    /// <summary>Five crossword cells spelling C-R-O-S-S, like the game grid.</summary>
    private void BuildMiniGrid(Font font)
    {
        RectTransform grid = new GameObject("MiniGrid", typeof(RectTransform)).GetComponent<RectTransform>();
        grid.SetParent(transform, false);
        grid.anchorMin = grid.anchorMax = new Vector2(0.5f, 0.5f);
        grid.pivot = new Vector2(0.5f, 0.5f);
        grid.anchoredPosition = new Vector2(0f, 170f);
        grid.sizeDelta = new Vector2(5f * 68f, 64f);

        const string letters = "CROSS";
        for (int i = 0; i < letters.Length; i++)
        {
            float x = (i - (letters.Length - 1) / 2f) * 68f;
            if (i == 2) // middle cell framed with the accent, like the selected cell in game
            {
                RectTransform outline = CreateImage("CellOutline", grid, AccentColor).rectTransform;
                outline.anchorMin = outline.anchorMax = new Vector2(0.5f, 0.5f);
                outline.pivot = new Vector2(0.5f, 0.5f);
                outline.anchoredPosition = new Vector2(x, 0f);
                outline.sizeDelta = new Vector2(72f, 72f);
            }
            RectTransform cell = CreateImage("Cell", grid, TextColor).rectTransform;
            cell.anchorMin = cell.anchorMax = new Vector2(0.5f, 0.5f);
            cell.pivot = new Vector2(0.5f, 0.5f);
            cell.anchoredPosition = new Vector2(x, 0f);
            cell.sizeDelta = new Vector2(64f, 64f);
            Text letter = CreateText("Letter", cell, font, letters[i].ToString(), 38, CellLetterColor, true, Vector2.zero, new Vector2(64f, 64f));
            Stretch(letter.rectTransform);
        }
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(string name, Transform parent, Font font, string content, int fontSize, Color color, bool bold, Vector2 position, Vector2 rectSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = rectSize;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
