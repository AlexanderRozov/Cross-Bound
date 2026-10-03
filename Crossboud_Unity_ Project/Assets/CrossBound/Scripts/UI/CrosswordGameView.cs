using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Newspaper-styled crossword screen. uGUI with two custom shaders: a paper
/// background (grain, fibers, stains, vignette) and cells with a rough ink
/// letterpress border. PT Serif (regular/bold) renders all text. The grid sits
/// next to newspaper-style clue columns and an on-screen letter keyboard, so the
/// puzzle is fully playable with the mouse; physical keyboard input goes through
/// InputController. All UI is placed with absolute RectTransform coordinates —
/// no flex layout — so the grid geometry is exact on any resolution and the
/// layout re-fits itself when the screen size or orientation changes.
/// </summary>
public sealed class CrosswordGameView : MonoBehaviour
{
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;
    private const float ChromeHeight = 250f;            // title + question block at the top
    private const float ButtonBarBottom = 44f;          // bottom bar of action buttons
    private const float ButtonBarHeight = 64f;
    private const float KeyboardGap = 14f;              // between buttons and the keyboard
    private const float FooterMargin = 16f;             // breathing room above the keyboard
    private const float CellOverlap = 4f;               // neighbours overdraw so shared borders merge
    private const float PanelMargin = 24f;

    private static readonly Color Ink = new Color32(0x20, 0x1A, 0x12, 0xFF);
    private static readonly Color InkSoft = new Color32(0x55, 0x4B, 0x3B, 0xFF);
    private static readonly Color CardPaper = new Color32(0xFB, 0xF7, 0xEA, 0xFF);
    private static readonly Color ButtonHover = new Color(0.90f, 0.87f, 0.79f, 1f);
    private static readonly Color ButtonPressed = new Color(0.82f, 0.79f, 0.70f, 1f);
    private static readonly Color InWordTint = new Color(1.00f, 0.93f, 0.74f, 1f);
    private static readonly Color CorrectTint = new Color(0.74f, 0.89f, 0.75f, 1f);
    private static readonly Color IncorrectTint = new Color(0.93f, 0.70f, 0.66f, 1f);
    private static readonly Color GoldInk = new Color32(0xA9, 0x7E, 0x2C, 0xFF);

    private CrosswordGameState _state;
    private Action _onRestart;
    private CanvasScaler _scaler;
    private RectTransform _canvasRect;
    private TextMeshProUGUI _number, _question, _score;
    private RectTransform _gridRoot;
    private Material _paperMat, _cellMat, _frameMat;
    private CrosswordClueListView _clues;
    private OnScreenKeyboardView _keyboard;
    private float _pitch = 54f;
    private Vector2 _lastScreenSize;
    private GameObject _winOverlay;

    private sealed class CellView
    {
        public int X, Y;
        public RectTransform Rect;
        public Image Background;
        public GameObject Frame;
        public TextMeshProUGUI Number;
        public TextMeshProUGUI Letter;
    }

    private readonly Dictionary<Vector2Int, CellView> _cells = new();

    /// <summary>Builds the whole screen. Returns false (and logs why) when the UI cannot be constructed.</summary>
    public bool Build(CrosswordGameState state, Action<int, int> select, Action delete, Action hint, Action check, Action restart)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _onRestart = restart;
        Debug.Log("[CrossBound][View] Build started (uGUI newspaper renderer).");

        _paperMat = CreateMaterial("Hidden/CrossBound/Paper");
        _cellMat = CreateMaterial("Hidden/CrossBound/Cell");
        if (_paperMat == null || _cellMat == null)
        {
            Debug.LogError("[CrossBound][View] Shaders not found (Hidden/CrossBound/Paper, Hidden/CrossBound/Cell) — check Resources/Shaders.");
            return false;
        }
        _frameMat = new Material(_cellMat);
        _frameMat.SetColor("_FillColor", new Color(0f, 0f, 0f, 0f));
        _frameMat.SetColor("_BorderColor", GoldInk);
        _frameMat.SetFloat("_BorderWidth", 0.11f);

        EnsureEventSystem();
        BuildCanvas();
        BuildChrome();
        BuildBottomBar(delete, hint, check);
        BuildGrid(select);
        BuildClues();
        BuildKeyboard();
        _lastScreenSize = Vector2.zero; // forces a full layout pass below
        ApplyLayout();

        _state.CellUpdated += OnCellUpdated;
        _state.SelectionChanged += OnSelectionChanged;
        _state.ScoreChanged += OnScoreChanged;
        _state.QuestionChanged += OnQuestionChanged;

        RefreshAllCells();
        _score.text = _state.Score.ToString();
        Debug.Log($"[CrossBound][View] Build finished: {_cells.Count} cells for {_state.Width}x{_state.Height}.");
        return true;
    }

    private static Material CreateMaterial(string shaderName)
    {
        Shader shader = Shader.Find(shaderName);
        if (shader == null)
            Debug.LogError($"[CrossBound][View] Shader '{shaderName}' not found.");
        return shader != null ? new Material(shader) : null;
    }

    // The project runs on the new Input System only; a scene StandaloneInputModule
    // cannot feed uGUI there, so wire up the module that actually works.
    private static void EnsureEventSystem()
    {
        EventSystem es = EventSystem.current;
        if (es == null)
            es = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        StandaloneInputModule legacy = es.GetComponent<StandaloneInputModule>();
        if (legacy != null)
            legacy.enabled = false;
        if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
            es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    // ----------------------------- Static layout -----------------------------

    private void BuildCanvas()
    {
        GameObject canvasGo = new GameObject("CrosswordCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        _scaler = canvasGo.GetComponent<CanvasScaler>();
        _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        _canvasRect = (RectTransform)canvasGo.transform;

        Image paper = CreateImage("Paper", _canvasRect, Color.white);
        paper.material = _paperMat;
        Stretch(paper.rectTransform);
    }

    private void BuildChrome()
    {
        CreateText("Title", _canvasRect, "К Р О С С В О Р Д", 46, Ink, true,
            new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(1200f, 64f));

        Image rule = CreateImage("TitleRule", _canvasRect, Ink);
        rule.raycastTarget = false;
        RectTransform ruleRect = rule.rectTransform;
        ruleRect.anchorMin = ruleRect.anchorMax = new Vector2(0.5f, 1f);
        ruleRect.pivot = new Vector2(0.5f, 0.5f);
        ruleRect.anchoredPosition = new Vector2(0f, -98f);
        ruleRect.sizeDelta = new Vector2(700f, 3f);

        TextMeshProUGUI scoreCaption = CreateText("ScoreCaption", _canvasRect, "ОЧКИ", 20, InkSoft, false,
            new Vector2(1f, 1f), new Vector2(-70f, -36f), new Vector2(120f, 30f));
        scoreCaption.alignment = TextAlignmentOptions.Right;
        _score = CreateText("Score", _canvasRect, "0", 34, Ink, true,
            new Vector2(1f, 1f), new Vector2(-70f, -72f), new Vector2(120f, 44f));
        _score.alignment = TextAlignmentOptions.Right;

        _number = CreateText("QuestionNumber", _canvasRect, string.Empty, 24, InkSoft, true,
            new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(900f, 34f));
        _question = CreateText("Question", _canvasRect, "Загрузка кроссворда…", 28, Ink, false,
            new Vector2(0.5f, 1f), new Vector2(0f, -156f), new Vector2(1500f, 84f));
        _question.textWrappingMode = TextWrappingModes.Normal;
    }

    private void BuildBottomBar(Action delete, Action hint, Action check)
    {
        CreateButton("DeleteButton", _canvasRect, "УДАЛИТЬ", -284f, delete);
        CreateButton("HintButton", _canvasRect, "ПОДСКАЗКА", 0f, hint);
        CreateButton("SubmitButton", _canvasRect, "ПРОВЕРИТЬ", 284f, check);
    }

    private void CreateButton(string name, Transform parent, string label, float x, Action onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(x, ButtonBarBottom);
        rect.sizeDelta = new Vector2(250f, ButtonBarHeight);

        Image image = go.GetComponent<Image>();
        image.material = _cellMat;
        image.color = CardPaper;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = ButtonHover;
        colors.pressedColor = ButtonPressed;
        colors.selectedColor = Color.white;
        button.colors = colors;
        button.onClick.AddListener(() => onClick?.Invoke());

        TextMeshProUGUI text = CreateText("Label", rect, label, 22, Ink, true, new Vector2(0.5f, 0.5f), Vector2.zero, rect.sizeDelta);
        text.raycastTarget = false;
        Stretch(text.rectTransform);
    }

    // ----------------------------- Grid -----------------------------

    private void BuildGrid(Action<int, int> select)
    {
        _cells.Clear();
        GameObject gridGo = new GameObject("Grid", typeof(RectTransform));
        _gridRoot = (RectTransform)gridGo.transform;
        _gridRoot.SetParent(_canvasRect, false);
        _gridRoot.anchorMin = _gridRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _gridRoot.pivot = new Vector2(0.5f, 0.5f);

        for (int y = 0; y < _state.Height; y++)
        for (int x = 0; x < _state.Width; x++)
        {
            if (!_state.HasAnswer(x, y))
                continue;

            GameObject cellGo = new GameObject($"Cell-{x}-{y}", typeof(RectTransform), typeof(Image), typeof(Button));
            cellGo.transform.SetParent(_gridRoot, false);
            CellView view = new CellView { X = x, Y = y, Rect = (RectTransform)cellGo.transform };

            view.Background = cellGo.GetComponent<Image>();
            view.Background.material = _cellMat;
            view.Background.color = Color.white;

            Button button = cellGo.GetComponent<Button>();
            button.targetGraphic = view.Background;
            button.transition = Selectable.Transition.None;
            int cx = x, cy = y;
            button.onClick.AddListener(() => select?.Invoke(cx, cy));

            Image frame = CreateImage("Frame", view.Rect, Color.white);
            frame.material = _frameMat;
            frame.raycastTarget = false;
            Stretch(frame.rectTransform);
            view.Frame = frame.gameObject;
            view.Frame.SetActive(false);

            int number = _state.GetCellNumber(x, y);
            if (number > 0)
            {
                view.Number = CreateText("Number", view.Rect, number.ToString(), 12, InkSoft, false,
                    new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
                view.Number.alignment = TextAlignmentOptions.TopLeft;
                view.Number.raycastTarget = false;
            }

            view.Letter = CreateText("Letter", view.Rect, string.Empty, 28, Ink, true,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            view.Letter.raycastTarget = false;
            Stretch(view.Letter.rectTransform);

            _cells[new Vector2Int(x, y)] = view;
        }
    }

    private void BuildClues()
    {
        _clues = new CrosswordClueListView(_canvasRect, _state);
    }

    private void BuildKeyboard()
    {
        _keyboard = new OnScreenKeyboardView(_canvasRect, _cellMat);
    }

    // ----------------------------- Adaptive layout -----------------------------

    private void Update()
    {
        Vector2 size = new Vector2(Screen.width, Screen.height);
        if (size != _lastScreenSize)
        {
            _lastScreenSize = size;
            ApplyLayout();
        }
    }

    /// <summary>Re-fits canvas scaling and all panels for the current resolution and orientation.</summary>
    private void ApplyLayout()
    {
        if (_state == null || _gridRoot == null) return;

        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        bool landscape = aspect >= 1f;
        // Landscape screens are height-constrained, portrait width-constrained.
        _scaler.matchWidthOrHeight = landscape ? 1f : 0f;

        float unitsW = landscape ? ReferenceHeight * aspect : ReferenceWidth;
        float unitsH = landscape ? ReferenceHeight : ReferenceWidth / aspect;

        // Footer: action buttons hug the bottom, the keyboard floats above them.
        float keyboardBottom = ButtonBarBottom + ButtonBarHeight + KeyboardGap;
        float keyboardHeight = _keyboard != null ? _keyboard.Layout(unitsW * 0.92f, keyboardBottom) : 0f;
        float footerHeight = keyboardBottom + keyboardHeight + FooterMargin;

        float zoneTop = unitsH / 2f - ChromeHeight;
        float zoneBottom = -unitsH / 2f + footerHeight;
        float zoneHeight = zoneTop - zoneBottom;
        float zoneCenterY = (zoneTop + zoneBottom) / 2f;

        if (landscape)
        {
            // Grid on the left, newspaper clue columns on the right.
            float clueWidth = Mathf.Clamp(unitsW * 0.36f, 340f, 640f);
            float gridAvailW = unitsW - clueWidth - 3f * PanelMargin;
            _pitch = Mathf.Clamp(Mathf.Floor(Mathf.Min(gridAvailW / _state.Width, zoneHeight * 0.96f / _state.Height)), 20f, 90f);
            // Center the grid inside the left zone (it spans -unitsW/2 + margin .. unitsW/2 - clueWidth - 2*margin).
            float gridCenterX = ((-unitsW / 2f + PanelMargin) + (unitsW / 2f - clueWidth - 2f * PanelMargin)) / 2f;
            _gridRoot.anchoredPosition = new Vector2(gridCenterX, zoneCenterY);
            _clues?.Layout(
                new Vector2(unitsW / 2f - clueWidth / 2f - PanelMargin, zoneCenterY),
                new Vector2(clueWidth, zoneHeight * 0.96f));
        }
        else
        {
            // Portrait: grid under the question block, clue columns below it.
            _pitch = Mathf.Clamp(Mathf.Floor(unitsW * 0.94f / _state.Width), 20f, 90f);
            float maxGridH = zoneHeight * 0.55f;
            if (_pitch * _state.Height > maxGridH)
                _pitch = Mathf.Max(20f, Mathf.Floor(maxGridH / _state.Height));
            float gridH = _pitch * _state.Height;
            float gridTop = unitsH / 2f - ChromeHeight - 12f;
            _gridRoot.anchoredPosition = new Vector2(0f, gridTop - gridH / 2f);

            float clueTop = gridTop - gridH - 16f;
            float clueBottom = zoneBottom + 8f;
            float clueH = Mathf.Max(160f, clueTop - clueBottom);
            _clues?.Layout(
                new Vector2(0f, (clueTop + clueBottom) / 2f),
                new Vector2(unitsW * 0.94f, clueH));
        }

        _gridRoot.sizeDelta = new Vector2(_pitch * _state.Width, _pitch * _state.Height);

        foreach (CellView cell in _cells.Values)
        {
            cell.Rect.pivot = new Vector2(0.5f, 0.5f);
            cell.Rect.anchorMin = cell.Rect.anchorMax = new Vector2(0f, 1f);
            cell.Rect.anchoredPosition = new Vector2(cell.X * _pitch + _pitch / 2f, -(cell.Y * _pitch + _pitch / 2f));
            cell.Rect.sizeDelta = new Vector2(_pitch + CellOverlap, _pitch + CellOverlap);
            cell.Letter.fontSize = Mathf.RoundToInt(_pitch * 0.52f);
            if (cell.Number != null)
            {
                cell.Number.fontSize = Mathf.RoundToInt(_pitch * 0.20f);
                cell.Number.rectTransform.pivot = new Vector2(0f, 1f);
                cell.Number.rectTransform.anchorMin = cell.Number.rectTransform.anchorMax = new Vector2(0f, 1f);
                cell.Number.rectTransform.anchoredPosition = new Vector2(_pitch * 0.06f, -_pitch * 0.02f);
                cell.Number.rectTransform.sizeDelta = new Vector2(_pitch * 0.5f, _pitch * 0.32f);
            }
        }
    }

    // ----------------------------- State subscriptions -----------------------------

    private void OnCellUpdated(int x, int y) => RefreshCell(x, y);
    private void OnSelectionChanged() => RefreshAllCells();
    private void OnScoreChanged(int score) => _score.text = score.ToString();

    private void OnQuestionChanged(CrosswordQuestion question, int number)
    {
        _number.text = $"{number} {(question.isHorizontal ? "→" : "↓")}";
        _question.text = question.question;
    }

    private void RefreshAllCells()
    {
        foreach (CellView cell in _cells.Values)
            RefreshCell(cell.X, cell.Y);
    }

    private void RefreshCell(int x, int y)
    {
        if (!_cells.TryGetValue(new Vector2Int(x, y), out CellView cell)) return;

        char input = _state.GetInput(x, y);
        cell.Letter.text = input != '\0' ? input.ToString() : string.Empty;

        bool selected = _state.IsSelected(x, y);
        cell.Frame.SetActive(selected);

        Color tint = Color.white;
        if (_state.IsCorrect(x, y)) tint = CorrectTint;
        else if (input != '\0') tint = IncorrectTint;
        else if (selected || _state.IsCellInCurrentWord(x, y)) tint = InWordTint;
        cell.Background.color = tint;
    }

    // ----------------------------- Win overlay -----------------------------

    public void ShowCompleted(int score, int bestScore)
    {
        if (_winOverlay != null) return;

        _winOverlay = new GameObject("WinOverlay", typeof(RectTransform), typeof(Image));
        _winOverlay.transform.SetParent(_canvasRect, false);
        Stretch((RectTransform)_winOverlay.transform);
        Image scrim = _winOverlay.GetComponent<Image>();
        scrim.color = new Color(0.12f, 0.10f, 0.06f, 0.72f);

        GameObject card = new GameObject("Card", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(_winOverlay.transform, false);
        RectTransform cardRect = (RectTransform)card.transform;
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(560f, 420f);
        Image cardImage = card.GetComponent<Image>();
        cardImage.material = _cellMat;
        cardImage.color = CardPaper;
        cardImage.raycastTarget = true;

        CreateText("WinTitle", cardRect, "ПОЗДРАВЛЯЕМ!", 40, Ink, true,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 130f), new Vector2(500f, 56f));
        CreateText("WinText", cardRect, "Кроссворд решён", 24, InkSoft, false,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 82f), new Vector2(500f, 36f));
        CreateText("WinScore", cardRect, $"Очки: {score}", 30, Ink, true,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 26f), new Vector2(500f, 44f));
        CreateText("WinBest", cardRect, $"Рекорд: {bestScore}", 20, InkSoft, false,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), new Vector2(500f, 30f));

        GameObject restart = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
        restart.transform.SetParent(cardRect, false);
        RectTransform restartRect = (RectTransform)restart.transform;
        restartRect.anchorMin = restartRect.anchorMax = new Vector2(0.5f, 0.5f);
        restartRect.pivot = new Vector2(0.5f, 0.5f);
        restartRect.anchoredPosition = new Vector2(0f, -118f);
        restartRect.sizeDelta = new Vector2(280f, 66f);
        Image restartImage = restart.GetComponent<Image>();
        restartImage.material = _cellMat;
        restartImage.color = CardPaper;
        Button restartButton = restart.GetComponent<Button>();
        restartButton.targetGraphic = restartImage;
        ColorBlock colors = restartButton.colors;
        colors.highlightedColor = ButtonHover;
        colors.pressedColor = ButtonPressed;
        restartButton.colors = colors;
        restartButton.onClick.AddListener(() => _onRestart?.Invoke());
        TextMeshProUGUI restartText = CreateText("Label", restartRect, "ИГРАТЬ СНОВА", 22, Ink, true,
            new Vector2(0.5f, 0.5f), Vector2.zero, restartRect.sizeDelta);
        restartText.raycastTarget = false;
        Stretch(restartText.rectTransform);

        Debug.Log($"[CrossBound][View] Win overlay shown (score {score}, best {bestScore}).");
    }

    // ----------------------------- Helpers -----------------------------

    private Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string content, float fontSize, Color color, bool bold, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.font = bold ? NewspaperFonts.Bold : NewspaperFonts.Regular;
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.margin = Vector4.zero;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
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
