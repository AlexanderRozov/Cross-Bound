using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class InputController : MonoBehaviour
{
    public static InputController Instance { get; private set; }
    
    public event Action<char> OnLetterInput;
    public event Action OnSubmit;
    public event Action OnDelete;
    
    private InputActionMap _inputMap;
    private InputAction _submitAction;
    private InputAction _deleteAction;

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
        _submitAction = _inputMap.AddAction("Submit", InputActionType.Button, "<Keyboard>/enter");
        _deleteAction = _inputMap.AddAction("Delete", InputActionType.Button, "<Keyboard>/backspace");

        _submitAction.performed += _ => OnSubmit?.Invoke();
        _deleteAction.performed += _ => OnDelete?.Invoke();
        Keyboard.current.onTextInput += HandleTextInput;
        _inputMap.Enable();
    }

    private void HandleTextInput(char value)
    {
        if (char.IsLetter(value))
            OnLetterInput?.Invoke(char.ToUpperInvariant(value));
    }

    private void OnDestroy()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput -= HandleTextInput;
        _inputMap?.Dispose();
        _submitAction?.Dispose();
        _deleteAction?.Dispose();
    }
}
