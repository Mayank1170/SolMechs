using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple integration that extends the official Play Solana SDK
/// Connects existing PSG system to ButtonPress and Stick components
/// </summary>
public class SimplePSGIntegration : MonoBehaviour
{
    [Header("Official PSG Components")]
    public PSG_KeyBindings psgKeyBindings;
    public PSG1_Manager psg1Manager;
    public PSG1_WebGL_Screen webglScreen;

    [Header("Your UI Components")]
    public ButtonPress buttonA;
    public ButtonPress buttonB;
    public ButtonPress buttonX;
    public ButtonPress buttonY;
    public ButtonPress buttonL1;
    public ButtonPress buttonR1;
    public Stick leftStick;
    public Stick rightStick;

    [Header("Integration Settings")]
    public bool autoFindComponents = true;
    public bool showDebugLogs = true;

    // Button visual state management
    private bool[] buttonPressed = new bool[8];

    void Start()
    {
        InitializeIntegration();
    }

    void Update()
    {
        // Monitor PSG inputs and update UI accordingly
        HandlePSGInputToUI();
    }

    /// <summary>
    /// Initialize integration with existing PSG system
    /// </summary>
    void InitializeIntegration()
    {
        LogDebug("SimplePSGIntegration: Starting integration with official PSG SDK...");

        // Auto-find official PSG components
        if (psgKeyBindings == null)
            psgKeyBindings = FindObjectOfType<PSG_KeyBindings>();

        if (psg1Manager == null)
            psg1Manager = FindObjectOfType<PSG1_Manager>();

        if (webglScreen == null)
            webglScreen = FindObjectOfType<PSG1_WebGL_Screen>();

        // Auto-find UI components if enabled
        if (autoFindComponents)
        {
            AutoFindUIComponents();
        }

        // Add custom key bindings for Sol Mechs
        AddSolMechsKeyBindings();

        LogDebug("SimplePSGIntegration: Integration complete!");
    }

    /// <summary>
    /// Auto-find ButtonPress and Stick components in scene
    /// </summary>
    void AutoFindUIComponents()
    {
        // Find buttons by name or component
        if (buttonA == null) buttonA = FindButtonByName("A");
        if (buttonB == null) buttonB = FindButtonByName("B");
        if (buttonX == null) buttonX = FindButtonByName("X");
        if (buttonY == null) buttonY = FindButtonByName("Y");
        if (buttonL1 == null) buttonL1 = FindButtonByName("L1");
        if (buttonR1 == null) buttonR1 = FindButtonByName("R1");

        // Find sticks
        if (leftStick == null) leftStick = FindStickByName("L3");
        if (rightStick == null) rightStick = FindStickByName("R3");
    }

    /// <summary>
    /// Find ButtonPress component by GameObject name
    /// </summary>
    ButtonPress FindButtonByName(string buttonName)
    {
        GameObject buttonObj = GameObject.Find(buttonName);
        if (buttonObj == null)
        {
            // Try common variations
            buttonObj = GameObject.Find($"Button{buttonName}") ??
                       GameObject.Find($"PSG_{buttonName}") ??
                       GameObject.Find($"btn{buttonName}");
        }

        if (buttonObj != null)
        {
            ButtonPress buttonPress = buttonObj.GetComponent<ButtonPress>();
            if (buttonPress != null)
            {
                LogDebug($"Found ButtonPress: {buttonName}");
                return buttonPress;
            }
        }
        return null;
    }

    /// <summary>
    /// Find Stick component by GameObject name
    /// </summary>
    Stick FindStickByName(string stickName)
    {
        GameObject stickObj = GameObject.Find(stickName);
        if (stickObj == null)
        {
            // Try common variations
            stickObj = GameObject.Find($"Stick{stickName}") ??
                      GameObject.Find($"PSG_{stickName}") ??
                      GameObject.Find($"{stickName}_Stick");
        }

        if (stickObj != null)
        {
            Stick stick = stickObj.GetComponent<Stick>();
            if (stick != null)
            {
                LogDebug($"Found Stick: {stickName}");
                return stick;
            }
        }
        return null;
    }

    /// <summary>
    /// Add Sol Mechs specific key bindings to existing PSG system
    /// </summary>
    void AddSolMechsKeyBindings()
    {
        if (psgKeyBindings == null) return;

        // Add custom Sol Mechs actions to existing PSG key bindings
        var connectBinding = psgKeyBindings.GetKeyBinding("PSG_Connect");
        if (connectBinding != null)
        {
            connectBinding.onKeyPressed.AddListener(() => OnSolMechsConnect());
        }

        var walletBinding = psgKeyBindings.GetKeyBinding("PSG_Wallet");
        if (walletBinding != null)
        {
            walletBinding.onKeyPressed.AddListener(() => OnSolMechsWallet());
        }

        var mintBinding = psgKeyBindings.GetKeyBinding("PSG_Mint_NFT");
        if (mintBinding != null)
        {
            mintBinding.onKeyPressed.AddListener(() => OnSolMechsMintNFT());
        }

        LogDebug("Sol Mechs key bindings added to PSG system");
    }

    /// <summary>
    /// Handle PSG input and update UI components accordingly
    /// </summary>
    void HandlePSGInputToUI()
    {
        // Monitor keyboard input and map to UI
        HandleButtonMapping();
        HandleStickMapping();
    }

    /// <summary>
    /// Map keyboard/PSG input to button visuals
    /// </summary>
    void HandleButtonMapping()
    {
        // Map PSG keys to button visuals
        UpdateButtonVisual(buttonA, Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.JoystickButton0), 0);
        UpdateButtonVisual(buttonB, Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.JoystickButton1), 1);
        UpdateButtonVisual(buttonX, Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.JoystickButton2), 2);
        UpdateButtonVisual(buttonY, Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.JoystickButton3), 3);
        UpdateButtonVisual(buttonL1, Input.GetKey(KeyCode.Tab) || Input.GetKey(KeyCode.JoystickButton4), 4);
        UpdateButtonVisual(buttonR1, Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.JoystickButton5), 5);
    }

    /// <summary>
    /// Update individual button visual state
    /// </summary>
    void UpdateButtonVisual(ButtonPress button, bool isPressed, int buttonIndex)
    {
        if (button == null) return;

        if (isPressed && !buttonPressed[buttonIndex])
        {
            // Button just pressed
            buttonPressed[buttonIndex] = true;
            ApplyPressedVisual(button);
            LogDebug($"Button {buttonIndex} pressed");
        }
        else if (!isPressed && buttonPressed[buttonIndex])
        {
            // Button just released
            buttonPressed[buttonIndex] = false;
            ApplyNormalVisual(button);
            LogDebug($"Button {buttonIndex} released");
        }
    }

    /// <summary>
    /// Apply pressed visual state to button
    /// </summary>
    void ApplyPressedVisual(ButtonPress button)
    {
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = Color.red;
            buttonImage.pixelsPerUnitMultiplier = 0.10f;
        }
    }

    /// <summary>
    /// Apply normal visual state to button
    /// </summary>
    void ApplyNormalVisual(ButtonPress button)
    {
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = Color.white;
            buttonImage.pixelsPerUnitMultiplier = 0.13f;
        }
    }

    /// <summary>
    /// Handle stick input mapping
    /// </summary>
    void HandleStickMapping()
    {
        // Left stick - WASD or gamepad left stick
        Vector2 leftInput = Vector2.zero;
        if (Input.GetKey(KeyCode.W)) leftInput.y += 1;
        if (Input.GetKey(KeyCode.S)) leftInput.y -= 1;
        if (Input.GetKey(KeyCode.A)) leftInput.x -= 1;
        if (Input.GetKey(KeyCode.D)) leftInput.x += 1;

        // Add gamepad input if available
        leftInput += new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        UpdateStickVisual(leftStick, leftInput);

        // Right stick - Arrow keys or gamepad right stick
        Vector2 rightInput = Vector2.zero;
        if (Input.GetKey(KeyCode.UpArrow)) rightInput.y += 1;
        if (Input.GetKey(KeyCode.DownArrow)) rightInput.y -= 1;
        if (Input.GetKey(KeyCode.LeftArrow)) rightInput.x -= 1;
        if (Input.GetKey(KeyCode.RightArrow)) rightInput.x += 1;

        UpdateStickVisual(rightStick, rightInput);
    }

    /// <summary>
    /// Update stick visual position
    /// </summary>
    void UpdateStickVisual(Stick stick, Vector2 input)
    {
        if (stick == null) return;

        // Clamp input and apply to stick position
        input = Vector2.ClampMagnitude(input, 1.0f);
        stick.transform.localPosition = new Vector3(input.x * 30, input.y * 30, 0);
    }

    // Sol Mechs specific PSG action handlers

    /// <summary>
    /// Handle PSG Connect action for Sol Mechs
    /// </summary>
    void OnSolMechsConnect()
    {
        LogDebug("Sol Mechs: PSG Connect action triggered");

        if (psg1Manager != null)
        {
            // Trigger PSG1 connection specific to Sol Mechs
            psg1Manager.TestPSG1Connection();
        }

        // Add Sol Mechs specific connection logic here
        // e.g., load mech data, connect to battle servers, etc.
    }

    /// <summary>
    /// Handle PSG Wallet action for Sol Mechs
    /// </summary>
    void OnSolMechsWallet()
    {
        LogDebug("Sol Mechs: Wallet action triggered");

        if (psg1Manager != null)
        {
            psg1Manager.TestWalletConnection();
        }

        // Add Sol Mechs specific wallet logic here
        // e.g., show mech NFTs, check SOL balance for battles, etc.
    }

    /// <summary>
    /// Handle PSG Mint NFT action for Sol Mechs
    /// </summary>
    void OnSolMechsMintNFT()
    {
        LogDebug("Sol Mechs: Mint NFT action triggered");

        if (psg1Manager != null)
        {
            psg1Manager.TestNFTMinting();
        }

        // Add Sol Mechs specific NFT minting here
        // e.g., mint new mech parts, victory NFTs, etc.
    }

    /// <summary>
    /// Debug logging with toggle control
    /// </summary>
    void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[PSGIntegration] {message}");
        }
    }

    // Public API for Sol Mechs

    /// <summary>
    /// Trigger specific PSG action programmatically
    /// </summary>
    public void TriggerPSGAction(string actionName)
    {
        if (psgKeyBindings != null)
        {
            psgKeyBindings.TriggerPSGAction(actionName);
        }
    }

    /// <summary>
    /// Check if PSG system is ready
    /// </summary>
    public bool IsPSGReady()
    {
        return psgKeyBindings != null && psg1Manager != null;
    }

    /// <summary>
    /// Get current PSG connection status
    /// </summary>
    public string GetPSGStatus()
    {
        if (!IsPSGReady()) return "PSG System not ready";

        // You can extend this based on what status info PSG1_Manager provides
        return "PSG System ready";
    }
}