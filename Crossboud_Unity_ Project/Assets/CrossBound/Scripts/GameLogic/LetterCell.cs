using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class LetterCell : MonoBehaviour, IPointerClickHandler
{
    private TMP_Text _text;
    private Image _background;
    public int X { get; private set; }
    public int Y { get; private set; }
    public char Answer { get; private set; }
    public char Input { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsRevealed { get; private set; }
    public event Action<LetterCell> OnCellSelected;

    public void Configure(TMP_Text text, Image background) { _text = text; _background = background; }

    public void Initialize(int x, int y, char answer, bool isActive)
    {
        X = x; Y = y; Answer = char.ToUpperInvariant(answer); IsActive = isActive; Input = '\0'; IsRevealed = false;
        Refresh(false);
    }

    public void SetSelected(bool selected) => Refresh(selected);
    public void OnPointerClick(PointerEventData _) { if (IsActive) OnCellSelected?.Invoke(this); }
    public bool SetInput(char letter)
    {
        if (!IsActive || IsRevealed) return false;
        Input = char.ToUpperInvariant(letter); Refresh(false); return true;
    }
    public void ClearInput() { if (IsActive && !IsRevealed) { Input = '\0'; Refresh(false); } }
    public void Reveal() { IsRevealed = true; Input = Answer; Refresh(false); }
    public bool CheckCorrect() => IsActive && Input == Answer;

    private void Refresh(bool selected)
    {
        if (_text != null) _text.text = IsActive && Input != '\0' ? Input.ToString() : string.Empty;
        if (_background == null) return;
        _background.color = !IsActive ? new Color(0.10f, 0.14f, 0.22f) : selected ? new Color(1f, .79f, .24f) : IsRevealed || Input == Answer && Input != '\0' ? new Color(.47f, .82f, .60f) : Input != '\0' ? new Color(1f, .61f, .56f) : Color.white;
    }
}
