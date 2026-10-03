using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Newspaper-style clue columns («ПО ГОРИЗОНТАЛИ» / «ПО ВЕРТИКАЛИ») shown next to the
/// grid. Entries are clickable — selecting a clue moves the selection to its start
/// cell — the current question is highlighted, fully solved questions are struck
/// through. Mouse clicks go through uGUI, so they ride on the new Input System's
/// InputSystemUIInputModule like every other UI element.
/// </summary>
public sealed class CrosswordClueListView
{
    private static readonly Color Ink = new Color32(0x20, 0x1A, 0x12, 0xFF);
    private static readonly Color InkSoft = new Color32(0x55, 0x4B, 0x3B, 0xFF);
    private static readonly Color CurrentTint = new Color(1.00f, 0.93f, 0.74f, 1f);
    private static readonly Color EntryHover = new Color(0.90f, 0.87f, 0.79f, 1f);

    private const float HeaderHeight = 40f;
    private const float EntryHeight = 52f;

    private sealed class Entry
    {
        public CrosswordQuestion Question;
        public string PlainText;
        public Image Background;
        public TextMeshProUGUI Label;
    }

    private readonly CrosswordGameState _state;
    private readonly List<Entry> _entries = new();

    public RectTransform Root { get; }

    public CrosswordClueListView(Transform parent, CrosswordGameState state)
    {
        _state = state;
        Root = new GameObject("ClueLists", typeof(RectTransform)).GetComponent<RectTransform>();
        Root.SetParent(parent, false);

        BuildColumn("Across", "ПО ГОРИЗОНТАЛИ", horizontal: true, anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(0.5f, 1f));
        BuildColumn("Down", "ПО ВЕРТИКАЛИ", horizontal: false, anchorMin: new Vector2(0.5f, 0f), anchorMax: new Vector2(1f, 1f));

        _state.QuestionChanged += OnQuestionChanged;
        _state.CellUpdated += OnCellUpdated;

        RefreshCurrent();
        RefreshSolved();
    }

    /// <summary>Places the panel in the given zone (reference-units rectangle, canvas center origin).</summary>
    public void Layout(Vector2 center, Vector2 size)
    {
        Root.anchorMin = Root.anchorMax = new Vector2(0.5f, 0.5f);
        Root.pivot = new Vector2(0.5f, 0.5f);
        Root.anchoredPosition = center;
        Root.sizeDelta = size;
    }

    private void BuildColumn(string name, string title, bool horizontal, Vector2 anchorMin, Vector2 anchorMax)
    {
        RectTransform column = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        column.SetParent(Root, false);
        column.anchorMin = anchorMin;
        column.anchorMax = anchorMax;
        column.offsetMin = new Vector2(12f, 0f);
        column.offsetMax = new Vector2(-12f, 0f);

        TextMeshProUGUI header = CreateLabel("Header", column, title, 22, InkSoft, bold: true, wrap: false);
        header.alignment = TextAlignmentOptions.Left;
        header.rectTransform.anchorMin = new Vector2(0f, 1f);
        header.rectTransform.anchorMax = new Vector2(1f, 1f);
        header.rectTransform.pivot = new Vector2(0.5f, 1f);
        header.rectTransform.anchoredPosition = Vector2.zero;
        header.rectTransform.sizeDelta = new Vector2(0f, HeaderHeight);

        ScrollRect scroll = BuildScroll(column);

        List<CrosswordQuestion> questions = new();
        foreach (CrosswordQuestion question in _state.Data.Questions)
            if (question.isHorizontal == horizontal)
                questions.Add(question);
        questions.Sort((a, b) => _state.GetQuestionNumber(a).CompareTo(_state.GetQuestionNumber(b)));

        foreach (CrosswordQuestion question in questions)
            BuildEntry(scroll.content, question);
    }

    private ScrollRect BuildScroll(RectTransform column)
    {
        GameObject scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollGo.transform.SetParent(column, false);
        RectTransform scrollRect = (RectTransform)scrollGo.transform;
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = new Vector2(0f, -HeaderHeight);
        scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.06f);

        GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
        viewportGo.transform.SetParent(scrollRect, false);
        RectTransform viewport = (RectTransform)viewportGo.transform;
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = Vector2.zero;
        viewport.offsetMax = Vector2.zero;

        GameObject contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGo.transform.SetParent(viewport, false);
        RectTransform content = (RectTransform)contentGo.transform;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = new Vector2(0f, 0f);
        content.offsetMax = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = contentGo.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;
        return scroll;
    }

    private void BuildEntry(RectTransform content, CrosswordQuestion question)
    {
        GameObject go = new GameObject($"Clue-{question.number}-{question.direction}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(content, false);
        LayoutElement layout = go.GetComponent<LayoutElement>();
        layout.preferredHeight = EntryHeight;
        layout.flexibleWidth = 1f;

        Image background = go.GetComponent<Image>();
        background.color = Color.clear;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = background;
        ColorBlock colors = button.colors;
        colors.highlightedColor = EntryHover;
        colors.pressedColor = EntryHover;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(() => _state.SelectQuestion(question));

        TextMeshProUGUI label = CreateLabel("Label", (RectTransform)go.transform, string.Empty, 22, Ink, bold: false, wrap: true);
        label.alignment = TextAlignmentOptions.Left;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(12f, 2f);
        label.rectTransform.offsetMax = new Vector2(-8f, -2f);
        label.raycastTarget = false;

        Entry entry = new Entry
        {
            Question = question,
            PlainText = $"{_state.GetQuestionNumber(question)}. {question.question}",
            Background = background,
            Label = label,
        };
        entry.Label.text = entry.PlainText;
        _entries.Add(entry);
    }

    private void OnQuestionChanged(CrosswordQuestion question, int number) => RefreshCurrent();

    private void OnCellUpdated(int x, int y) => RefreshSolved();

    private void RefreshCurrent()
    {
        foreach (Entry entry in _entries)
            entry.Background.color = ReferenceEquals(entry.Question, _state.CurrentQuestion) ? CurrentTint : Color.clear;
    }

    private void RefreshSolved()
    {
        foreach (Entry entry in _entries)
        {
            bool solved = IsSolved(entry.Question);
            entry.Label.text = solved ? $"<s>{entry.PlainText}</s>" : entry.PlainText;
            entry.Label.color = solved ? InkSoft : Ink;
        }
    }

    private bool IsSolved(CrosswordQuestion question)
    {
        int x = question.startX, y = question.startY;
        foreach (char letter in question.answer.ToUpperInvariant())
        {
            if (x < 0 || y < 0 || x >= _state.Width || y >= _state.Height) return false;
            if (_state.Answers[x, y] != letter || _state.Inputs[x, y] != letter) return false;
            if (question.isHorizontal) x++; else y++;
        }
        return true;
    }

    private static TextMeshProUGUI CreateLabel(string name, Transform parent, string content, float fontSize, Color color, bool bold, bool wrap)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.font = bold ? NewspaperFonts.Bold : NewspaperFonts.Regular;
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }
}
