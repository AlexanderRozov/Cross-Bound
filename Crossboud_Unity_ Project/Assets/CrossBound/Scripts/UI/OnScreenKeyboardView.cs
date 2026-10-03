using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// On-screen letter keyboard so the crossword can be played entirely with the mouse
/// (useful on WebGL/touch where no physical keyboard may exist). Keys are uGUI
/// buttons — pointer input arrives through the new Input System's
/// InputSystemUIInputModule — and feed the same InputController events as physical
/// typing, so the game logic cannot tell the difference.
/// </summary>
public sealed class OnScreenKeyboardView
{
    private static readonly Color Ink = new Color32(0x20, 0x1A, 0x12, 0xFF);
    private static readonly Color CardPaper = new Color32(0xFB, 0xF7, 0xEA, 0xFF);
    private static readonly Color KeyHover = new Color(0.90f, 0.87f, 0.79f, 1f);
    private static readonly Color KeyPressed = new Color(0.82f, 0.79f, 0.70f, 1f);

    private const string RowTop = "ABCDEFGHIJKLM";
    private const string RowBottom = "NOPQRSTUVWXYZ";
    private const float Gap = 6f;
    private const float MinKeySize = 34f;
    private const float MaxKeySize = 58f;

    private readonly Material _keyMaterial;
    private readonly List<KeyView> _keys = new();

    public RectTransform Root { get; }
    public float Height => 2f * KeySize + Gap;

    private float KeySize { get; set; } = 48f;

    private sealed class KeyView
    {
        public RectTransform Rect;
        public TextMeshProUGUI Label;
        public int Index;
    }

    public OnScreenKeyboardView(Transform parent, Material keyMaterial)
    {
        _keyMaterial = keyMaterial;
        Root = new GameObject("OnScreenKeyboard", typeof(RectTransform)).GetComponent<RectTransform>();
        Root.SetParent(parent, false);
        Root.anchorMin = Root.anchorMax = new Vector2(0.5f, 0f);
        Root.pivot = new Vector2(0.5f, 0f);

        BuildRow(RowTop, 0);
        BuildRow(RowBottom, 1);
    }

    /// <summary>Re-fits the key size to the available width and anchors the bar above the given bottom offset; returns the bar height.</summary>
    public float Layout(float maxWidth, float bottomY)
    {
        KeySize = Mathf.Clamp(Mathf.Floor((maxWidth - (RowTop.Length - 1) * Gap) / RowTop.Length), MinKeySize, MaxKeySize);
        float rowWidth = RowTop.Length * KeySize + (RowTop.Length - 1) * Gap;
        Root.sizeDelta = new Vector2(rowWidth, Height);
        Root.anchoredPosition = new Vector2(0f, bottomY);

        for (int rowIndex = 0; rowIndex < Root.childCount; rowIndex++)
        {
            RectTransform row = (RectTransform)Root.GetChild(rowIndex);
            row.anchoredPosition = new Vector2(0f, -rowIndex * (KeySize + Gap));
            row.sizeDelta = new Vector2(rowWidth, KeySize);
        }

        foreach (KeyView key in _keys)
        {
            key.Rect.anchoredPosition = new Vector2((key.Index - (RowTop.Length - 1) / 2f) * (KeySize + Gap), 0f);
            key.Rect.sizeDelta = new Vector2(KeySize, KeySize);
            key.Label.fontSize = KeySize * 0.52f;
        }

        return Height;
    }

    private void BuildRow(string letters, int rowIndex)
    {
        RectTransform row = new GameObject($"Row{rowIndex}", typeof(RectTransform)).GetComponent<RectTransform>();
        row.SetParent(Root, false);
        row.anchorMin = row.anchorMax = new Vector2(0.5f, 1f);
        row.pivot = new Vector2(0.5f, 1f);
        row.anchoredPosition = new Vector2(0f, -rowIndex * (KeySize + Gap));
        row.sizeDelta = new Vector2(RowTop.Length * KeySize + (RowTop.Length - 1) * Gap, KeySize);

        for (int i = 0; i < letters.Length; i++)
        {
            char letter = letters[i];
            BuildKey(row, letter, i);
        }
    }

    private void BuildKey(RectTransform row, char letter, int index)
    {
        GameObject go = new GameObject($"Key-{letter}", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(row, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2((index - (RowTop.Length - 1) / 2f) * (KeySize + Gap), 0f);
        rect.sizeDelta = new Vector2(KeySize, KeySize);

        Image image = go.GetComponent<Image>();
        image.material = _keyMaterial;
        image.color = CardPaper;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = KeyHover;
        colors.pressedColor = KeyPressed;
        colors.fadeDuration = 0.06f;
        button.colors = colors;
        button.onClick.AddListener(() => InputController.Instance?.FeedLetter(letter));

        TextMeshProUGUI label = CreateLabel("Label", rect, letter.ToString(), KeySize * 0.52f, Ink);
        label.raycastTarget = false;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;

        _keys.Add(new KeyView { Rect = rect, Label = label, Index = index });
    }

    private static TextMeshProUGUI CreateLabel(string name, Transform parent, string content, float fontSize, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.font = NewspaperFonts.Bold;
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.margin = Vector4.zero;
        return text;
    }
}
