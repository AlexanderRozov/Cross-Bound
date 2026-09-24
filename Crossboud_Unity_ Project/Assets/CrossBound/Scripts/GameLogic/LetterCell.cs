using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System;

public class LetterCell : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _selectedColor = Color.yellow;
    [SerializeField] private Color _correctColor = Color.green;
    [SerializeField] private Color _incorrectColor = Color.red;
    
    public int X { get; private set; }
    public int Y { get; private set; }
    public char Answer { get; private set; }
    public char Input { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsRevealed { get; private set; }
    
    public event Action<LetterCell> OnCellSelected;
    
    public void Initialize(int x, int y, char answer, bool isActive)
    {
        X = x;
        Y = y;
        Answer = char.ToUpper(answer);
        IsActive = isActive;
        Input = '\0';
        IsRevealed = false;
        _text.text = "";
        UpdateVisual();
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsActive)
            OnCellSelected?.Invoke(this);
    }
    
    public bool SetInput(char letter)
    {
        if (!IsActive || IsRevealed) return false;
        Input = char.ToUpper(letter);
        _text.text = Input.ToString();
        UpdateVisual();
        return true;
    }
    
    public void ClearInput()
    {
        if (!IsActive || IsRevealed) return;
        Input = '\0';
        _text.text = "";
        UpdateVisual();
    }
    
    public void Reveal()
    {
        IsRevealed = true;
        Input = Answer;
        _text.text = Answer.ToString();
        UpdateVisual();
    }
    
    public bool CheckCorrect() => IsActive && Input == Answer;
    
    private void UpdateVisual()
    {
        if (!IsActive)
        {
            _text.color = Color.gray;
            return;
        }
        
        if (IsRevealed)
            _text.color = _correctColor;
        else if (Input != '\0')
            _text.color = Input == Answer ? _correctColor : _incorrectColor;
        else
            _text.color = _normalColor;
    }
}