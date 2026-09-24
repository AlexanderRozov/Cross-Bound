using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using System;

public class InputController : MonoBehaviour
{
    public static InputController Instance { get; private set; }
    
    public event Action<char> OnLetterInput;
    public event Action OnSubmit;
    public event Action OnDelete;
    
    private InputAction _letterAction;
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
        var inputMap = new InputActionMap("Crossword");
        
        _letterAction = inputMap.AddAction("Letter", InputActionType.Value, "<Keyboard>/a");
        _submitAction = inputMap.AddAction("Submit", InputActionType.Button, "<Keyboard>/enter");
        _deleteAction = inputMap.AddAction("Delete", InputActionType.Button, "<Keyboard>/backspace");
        
        _letterAction.performed += HandleLetterInput;
        _submitAction.performed += _ => OnSubmit?.Invoke();
        _deleteAction.performed += _ => OnDelete?.Invoke();
        
        inputMap.Enable();
    }

    private void HandleLetterInput(InputAction.CallbackContext context)
    {
        string value = context.control.name;
        if (value.Length == 1 && char.IsLetter(value[0]))
        {
            OnLetterInput?.Invoke(char.ToUpper(value[0]));
        }
    }

    private void OnDestroy()
    {
        _letterAction?.Dispose();
        _submitAction?.Dispose();
        _deleteAction?.Dispose();
    }
}
