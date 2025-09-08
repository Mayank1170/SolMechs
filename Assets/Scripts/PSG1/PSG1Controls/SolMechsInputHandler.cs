using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Handles Sol Mechs input actions and connects them to UI buttons
/// Works with the Input Actions asset you created
/// </summary>
public class SolMechsInputHandler : MonoBehaviour
{
    [Header("UI Button References")]
    public ButtonPress buttonA;
    public ButtonPress buttonB;
    public ButtonPress buttonX;
    public ButtonPress buttonY;
    public ButtonPress buttonL1;
    public ButtonPress buttonR1;

    [Header("Stick References")]
    public Stick leftStick;
    public Stick rightStick;

    [Header("Input Actions")]
    public InputActionReference confirmAction;
    public InputActionReference cancelAction;
    public InputActionReference fireAction;
    public InputActionReference moveAction;
    public InputActionReference lookAction;
    public InputActionReference menuAction;

    [Header("Settings")]
    public bool showInputDebug = true;

    // Input state tracking
    private bool[] buttonPressed = new bool[6];

    void OnEnable()
    {
        // Subscribe to input actions
        if (confirmAction != null)
        {
            confirmAction.action.performed += OnConfirmPressed;
            confirmAction.action.canceled += OnConfirmReleased;
        }

        if (cancelAction != null)
        {
            cancelAction.action.performed += OnCancelPressed;
            cancelAction.action.canceled += OnCancelReleased;
        }

        if (fireAction != null)
        {
            fireAction.action.performed += OnFirePressed;
            fireAction.action.canceled += OnFireReleased;
        }

        if (moveAction != null)
        {
            moveAction.action.performed += OnMovePerformed;
            moveAction.action.canceled += OnMoveCanceled;
        }

        if (lookAction != null)
        {
            lookAction.action.performed += OnLookPerformed;
            lookAction.action.canceled += OnLookCanceled;
        }

        if (menuAction != null)
        {
            menuAction.action.performed += OnMenuPressed;
        }
    }

    void OnDisable()
    {
        // Unsubscribe from input actions
        if (confirmAction != null)
        {
            confirmAction.action.performed -= OnConfirmPressed;
            confirmAction.action.canceled -= OnConfirmReleased;
        }

        if (cancelAction != null)
        {
            cancelAction.action.performed -= OnCancelPressed;
            cancelAction.action.canceled -= OnCancelReleased;
        }

        if (fireAction != null)
        {
            fireAction.action.performed -= OnFirePressed;
            fireAction.action.canceled -= OnFireReleased;
        }

        if (moveAction != null)
        {
            moveAction.action.performed -= OnMovePerformed;
            moveAction.action.canceled -= OnMoveCanceled;
        }

        if (lookAction != null)
        {
            lookAction.action.performed -= OnLookPerformed;
            lookAction.action.canceled -= OnLookCanceled;
        }

        if (menuAction != null)
        {
            menuAction.action.performed -= OnMenuPressed;
        }
    }

    // Button input handlers
    void OnConfirmPressed(InputAction.CallbackContext context)
    {
        UpdateButtonVisual(buttonA, true, 0);
        LogInput("Confirm/A Button Pressed");

        // Add your Sol Mechs confirm logic here
        OnSolMechsConfirm();
    }

    void OnConfirmReleased(InputAction.CallbackContext context)
    {
        UpdateButtonVisual(buttonA, false, 0);
        LogInput("Confirm/A Button Released");
    }

    void OnCancelPressed(InputAction.CallbackContext context)
    {
        UpdateButtonVisual(buttonB, true, 1);
        LogInput("Cancel/B Button Pressed");

        // Add your Sol Mechs cancel logic here
        OnSolMechsCancel();
    }

    void OnCancelReleased(InputAction.CallbackContext context)
    {
        UpdateButtonVisual(buttonB, false, 1);
        LogInput("Cancel/B Button Released");
    }

    void OnFirePressed(InputAction.CallbackContext context)
    {
        UpdateButtonVisual(buttonX, true, 2);
        LogInput("Fire/X Button Pressed");

        // Add your Sol Mechs fire/attack logic here
        OnSolMechsFire();
    }

    void OnFireReleased(InputAction.CallbackContext context)
    {
        UpdateButtonVisual(buttonX, false, 2);
        LogInput("Fire/X Button Released");
    }

    void OnMenuPressed(InputAction.CallbackContext context)
    {
        UpdateButtonVisual(buttonL1, true, 4);
        LogInput("Menu/L1 Button Pressed");

        // Add your Sol Mechs menu logic here
        OnSolMechsMenu();

        // Auto-release menu button after short delay
        Invoke(nameof(ReleaseMenuButton), 0.1f);
    }

    void ReleaseMenuButton()
    {
        UpdateButtonVisual(buttonL1, false, 4);
    }

    // Stick input handlers
    void OnMovePerformed(InputAction.CallbackContext context)
    {
        Vector2 moveInput = context.ReadValue<Vector2>();
        UpdateStickVisual(leftStick, moveInput);
        LogInput($"Move/Left Stick: {moveInput}");

        // Add your Sol Mechs movement logic here
        OnSolMechsMove(moveInput);
    }

    void OnMoveCanceled(InputAction.CallbackContext context)
    {
        UpdateStickVisual(leftStick, Vector2.zero);
    }

    void OnLookPerformed(InputAction.CallbackContext context)
    {
        Vector2 lookInput = context.ReadValue<Vector2>();
        UpdateStickVisual(rightStick, lookInput);
        LogInput($"Look/Right Stick: {lookInput}");

        // Add your Sol Mechs camera/look logic here
        OnSolMechsLook(lookInput);
    }

    void OnLookCanceled(InputAction.CallbackContext context)
    {
        UpdateStickVisual(rightStick, Vector2.zero);
    }

    // Visual update methods
    void UpdateButtonVisual(ButtonPress button, bool isPressed, int buttonIndex)
    {
        if (button == null) return;

        buttonPressed[buttonIndex] = isPressed;

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            if (isPressed)
            {
                buttonImage.color = Color.red;
                buttonImage.pixelsPerUnitMultiplier = 0.10f;
            }
            else
            {
                buttonImage.color = Color.white;
                buttonImage.pixelsPerUnitMultiplier = 0.13f;
            }
        }
    }

    void UpdateStickVisual(Stick stick, Vector2 input)
    {
        if (stick == null) return;

        // Move stick visual based on input
        stick.transform.localPosition = new Vector3(input.x * 30, input.y * 30, 0);
    }

    // Sol Mechs specific game logic methods
    void OnSolMechsConfirm()
    {
        // Add your confirm logic here
        // Example: Select mech part, confirm battle action, etc.
        LogInput("Sol Mechs: Confirm action executed");
    }

    void OnSolMechsCancel()
    {
        // Add your cancel logic here
        // Example: Go back in menus, cancel selection, etc.
        LogInput("Sol Mechs: Cancel action executed");
    }

    void OnSolMechsFire()
    {
        // Add your fire/attack logic here
        // Example: Fire mech weapon, execute attack, etc.
        LogInput("Sol Mechs: Fire action executed");
    }

    void OnSolMechsMenu()
    {
        // Add your menu logic here
        // Example: Open pause menu, toggle UI, etc.
        LogInput("Sol Mechs: Menu action executed");
    }

    void OnSolMechsMove(Vector2 moveInput)
    {
        // Add your movement logic here
        // Example: Move mech, navigate UI, etc.
        // LogInput($"Sol Mechs: Move executed: {moveInput}");
    }

    void OnSolMechsLook(Vector2 lookInput)
    {
        // Add your look/camera logic here
        // Example: Camera control, aim mech weapons, etc.
        // LogInput($"Sol Mechs: Look executed: {lookInput}");
    }

    // Utility methods
    void LogInput(string message)
    {
        if (showInputDebug)
        {
            Debug.Log($"[SolMechsInput] {message}");
        }
    }

    // Public API for external scripts
    public bool IsButtonPressed(int buttonIndex)
    {
        if (buttonIndex >= 0 && buttonIndex < buttonPressed.Length)
            return buttonPressed[buttonIndex];
        return false;
    }

    public Vector2 GetCurrentMoveInput()
    {
        if (moveAction != null)
            return moveAction.action.ReadValue<Vector2>();
        return Vector2.zero;
    }

    public Vector2 GetCurrentLookInput()
    {
        if (lookAction != null)
            return lookAction.action.ReadValue<Vector2>();
        return Vector2.zero;
    }
}