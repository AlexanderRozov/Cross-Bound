using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Global keyboard input for the crossword: Latin letters (Russian lookalike
/// letters such as А/В/Е/К/М/Н/О/Р/С/Т/У/Х are transliterated automatically),
/// Backspace deletes, Enter checks the word, arrow keys move the selection.
/// </summary>
public class InputController : MonoBehaviour
{
    public static InputController Instance { get; private set; }

    public event Action<char> OnLetterInput;
    public event Action OnSubmit;
    public event Action OnDelete;
    public event Action<int, int> OnNavigate;

    private InputActionMap _inputMap;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeInputActions();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeInputActions()
    {
        _inputMap = new InputActionMap("Crossword");

        InputAction submitAction = _inputMap.AddAction("Submit", InputActionType.Button, "<Keyboard>/enter");
        InputAction deleteAction = _inputMap.AddAction("Delete", InputActionType.Button, "<Keyboard>/backspace");
        submitAction.performed += _ => OnSubmit?.Invoke();
        deleteAction.performed += _ => OnDelete?.Invoke();

        BindNavigation("NavigateLeft", "<Keyboard>/leftArrow", -1, 0);
        BindNavigation("NavigateRight", "<Keyboard>/rightArrow", 1, 0);
        BindNavigation("NavigateUp", "<Keyboard>/upArrow", 0, -1);
        BindNavigation("NavigateDown", "<Keyboard>/downArrow", 0, 1);

        if (Keyboard.current != null)
            Keyboard.current.onTextInput += HandleTextInput;

        _inputMap.Enable();
    }

    private void BindNavigation(string actionName, string binding, int dx, int dy)
    {
        InputAction action = _inputMap.AddAction(actionName, InputActionType.Button, binding);
        action.performed += _ => OnNavigate?.Invoke(dx, dy);
    }

    private void HandleTextInput(char value)
    {
        char letter = TransliterateToLatin(value);
        if (letter >= 'A' && letter <= 'Z')
            OnLetterInput?.Invoke(letter);
    }

    /// <summary>Maps Cyrillic lookalike letters to Latin so Russian-layout typing still fills Latin answers.</summary>
    private static char TransliterateToLatin(char value)
    {
        char upper = char.ToUpperInvariant(value);
        if (upper >= 'A' && upper <= 'Z') return upper;

        switch (upper)
        {
            case 'А': return 'A';
            case 'В': return 'B';
            case 'Е': return 'E';
            case 'Ё': return 'E';
            case 'К': return 'K';
            case 'М': return 'M';
            case 'Н': return 'H';
            case 'О': return 'O';
            case 'Р': return 'P';
            case 'С': return 'C';
            case 'Т': return 'T';
            case 'У': return 'Y';
            case 'Х': return 'X';
            default: return '\0';
        }
    }

    private void OnDestroy()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput -= HandleTextInput;
        _inputMap?.Dispose();
    }
}
