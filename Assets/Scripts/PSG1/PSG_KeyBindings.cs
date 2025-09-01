using UnityEngine;
using UnityEngine.Events;

// Play Solana Key Bindings System
public class PSG_KeyBindings : MonoBehaviour
{
    [System.Serializable]
    public class PSGKeyBinding
    {
        public string actionName;
        public KeyCode primaryKey;
        public KeyCode secondaryKey = KeyCode.None;
        public UnityEvent onKeyPressed;
        public bool isActive = true;
    }
    
    [Header("Play Solana Standard Key Bindings")]
    [SerializeField] private PSGKeyBinding[] psgBindings = new PSGKeyBinding[]
    {
        new PSGKeyBinding() { actionName = "PSG_Connect", primaryKey = KeyCode.C },
        new PSGKeyBinding() { actionName = "PSG_Wallet", primaryKey = KeyCode.W },
        new PSGKeyBinding() { actionName = "PSG_Mint_NFT", primaryKey = KeyCode.N },
        new PSGKeyBinding() { actionName = "PSG_Trade", primaryKey = KeyCode.T },
        new PSGKeyBinding() { actionName = "PSG_Inventory", primaryKey = KeyCode.I },
        new PSGKeyBinding() { actionName = "PSG_Rewards", primaryKey = KeyCode.R },
        new PSGKeyBinding() { actionName = "PSG_Leaderboard", primaryKey = KeyCode.L },
        new PSGKeyBinding() { actionName = "PSG_Settings", primaryKey = KeyCode.S },
        new PSGKeyBinding() { actionName = "PSG_Help", primaryKey = KeyCode.H, secondaryKey = KeyCode.F1 },
        new PSGKeyBinding() { actionName = "PSG_Menu", primaryKey = KeyCode.Escape, secondaryKey = KeyCode.M }
    };
    
    [Header("Game Integration")]
    public PSG1_Manager psg1Manager;
    public PSG1_WebGL_Screen webglScreen;
    
    [Header("UI Feedback")]
    public bool showKeyPressedFeedback = true;
    public float feedbackDuration = 2f;
    
    [Header("Debug")]
    public bool logKeyPresses = true;
    
    private void Start()
    {
        Debug.Log("🕹️ PSG Key Bindings System: Initializing...");
        
        // Find references if not assigned
        if (psg1Manager == null)
            psg1Manager = FindObjectOfType<PSG1_Manager>();
            
        if (webglScreen == null)
            webglScreen = FindObjectOfType<PSG1_WebGL_Screen>();
        
        // Setup default key binding events
        SetupDefaultKeyBindings();
        
        Debug.Log("✅ PSG Key Bindings System: Ready!");
        LogAvailableKeybindings();
    }
    
    private void Update()
    {
        // Check all PSG key bindings
        foreach (var binding in psgBindings)
        {
            if (!binding.isActive) continue;
            
            bool primaryPressed = Input.GetKeyDown(binding.primaryKey);
            bool secondaryPressed = binding.secondaryKey != KeyCode.None && Input.GetKeyDown(binding.secondaryKey);
            
            if (primaryPressed || secondaryPressed)
            {
                HandleKeyBinding(binding, primaryPressed ? binding.primaryKey : binding.secondaryKey);
            }
        }
    }
    
    private void SetupDefaultKeyBindings()
    {
        Debug.Log("⚙️ Setting up default PSG key bindings...");
        
        foreach (var binding in psgBindings)
        {
            switch (binding.actionName)
            {
                case "PSG_Connect":
                    binding.onKeyPressed.AddListener(() => HandlePSGConnect());
                    break;
                    
                case "PSG_Wallet":
                    binding.onKeyPressed.AddListener(() => HandlePSGWallet());
                    break;
                    
                case "PSG_Mint_NFT":
                    binding.onKeyPressed.AddListener(() => HandlePSGMintNFT());
                    break;
                    
                case "PSG_Trade":
                    binding.onKeyPressed.AddListener(() => HandlePSGTrade());
                    break;
                    
                case "PSG_Inventory":
                    binding.onKeyPressed.AddListener(() => HandlePSGInventory());
                    break;
                    
                case "PSG_Rewards":
                    binding.onKeyPressed.AddListener(() => HandlePSGRewards());
                    break;
                    
                case "PSG_Leaderboard":
                    binding.onKeyPressed.AddListener(() => HandlePSGLeaderboard());
                    break;
                    
                case "PSG_Settings":
                    binding.onKeyPressed.AddListener(() => HandlePSGSettings());
                    break;
                    
                case "PSG_Help":
                    binding.onKeyPressed.AddListener(() => HandlePSGHelp());
                    break;
                    
                case "PSG_Menu":
                    binding.onKeyPressed.AddListener(() => HandlePSGMenu());
                    break;
            }
        }
    }
    
    private void HandleKeyBinding(PSGKeyBinding binding, KeyCode pressedKey)
    {
        if (logKeyPresses)
        {
            Debug.Log($"🕹️ PSG Key Pressed: {binding.actionName} ({pressedKey})");
        }
        
        // Show feedback if enabled
        if (showKeyPressedFeedback)
        {
            ShowKeyFeedback(binding.actionName, pressedKey);
        }
        
        // Invoke the binding event
        binding.onKeyPressed?.Invoke();
    }
    
    private void ShowKeyFeedback(string actionName, KeyCode key)
    {
        // Visual feedback for key press
        Debug.Log($"🎮 PSG Action: {actionName} [{key}]");
        
        // You can add UI feedback here (like showing a popup or highlight)
        // For now, we'll just log it
    }
    
    // PSG Action Handlers
    private void HandlePSGConnect()
    {
        Debug.Log("🔗 PSG Action: Connect to Play Solana");
        
        if (psg1Manager != null)
        {
            psg1Manager.TestPSG1Connection();
        }
        else
        {
            Debug.LogWarning("⚠️ PSG1 Manager not found for connect action");
        }
    }
    
    private void HandlePSGWallet()
    {
        Debug.Log("💳 PSG Action: Wallet Information");
        
        if (psg1Manager != null)
        {
            psg1Manager.TestWalletConnection();
        }
        else
        {
            Debug.LogWarning("⚠️ PSG1 Manager not found for wallet action");
        }
    }
    
    private void HandlePSGMintNFT()
    {
        Debug.Log("🎨 PSG Action: Mint NFT");
        
        if (psg1Manager != null)
        {
            psg1Manager.TestNFTMinting();
        }
        else
        {
            Debug.LogWarning("⚠️ PSG1 Manager not found for NFT minting");
        }
    }
    
    private void HandlePSGTrade()
    {
        Debug.Log("💱 PSG Action: Trade/Marketplace");
        // TODO: Implement trading functionality
        LogNotImplemented("Trading/Marketplace");
    }
    
    private void HandlePSGInventory()
    {
        Debug.Log("🎒 PSG Action: Inventory");
        // TODO: Implement inventory system
        LogNotImplemented("Inventory System");
    }
    
    private void HandlePSGRewards()
    {
        Debug.Log("🏆 PSG Action: Rewards");
        
        if (psg1Manager != null)
        {
            // Trigger a test reward
            psg1Manager.OnGameWin();
        }
        else
        {
            LogNotImplemented("Rewards System");
        }
    }
    
    private void HandlePSGLeaderboard()
    {
        Debug.Log("🏅 PSG Action: Leaderboard");
        // TODO: Implement leaderboard system
        LogNotImplemented("Leaderboard System");
    }
    
    private void HandlePSGSettings()
    {
        Debug.Log("⚙️ PSG Action: Settings");
        // TODO: Implement settings menu
        LogNotImplemented("Settings Menu");
    }
    
    private void HandlePSGHelp()
    {
        Debug.Log("❓ PSG Action: Help/Tutorial");
        LogAvailableKeybindings();
    }
    
    private void HandlePSGMenu()
    {
        Debug.Log("📋 PSG Action: Menu");
        
        if (webglScreen != null)
        {
            webglScreen.ToggleScreens();
        }
        else
        {
            LogNotImplemented("PSG Menu");
        }
    }
    
    private void LogNotImplemented(string feature)
    {
        Debug.LogWarning($"⚠️ PSG Feature not yet implemented: {feature}");
    }
    
    private void LogAvailableKeybindings()
    {
        Debug.Log("🕹️ === PSG KEY BINDINGS HELP ===");
        foreach (var binding in psgBindings)
        {
            if (!binding.isActive) continue;
            
            string keyInfo = binding.primaryKey.ToString();
            if (binding.secondaryKey != KeyCode.None)
            {
                keyInfo += $" or {binding.secondaryKey}";
            }
            
            Debug.Log($"🔑 {binding.actionName}: {keyInfo}");
        }
        Debug.Log("🕹️ === END KEY BINDINGS ===");
    }
    
    // Public methods for external control
    public void SetKeyBindingActive(string actionName, bool active)
    {
        var binding = System.Array.Find(psgBindings, b => b.actionName == actionName);
        if (binding != null)
        {
            binding.isActive = active;
            Debug.Log($"🔑 PSG Key binding '{actionName}' set to {(active ? "active" : "inactive")}");
        }
    }
    
    public void TriggerPSGAction(string actionName)
    {
        var binding = System.Array.Find(psgBindings, b => b.actionName == actionName);
        if (binding != null && binding.isActive)
        {
            Debug.Log($"🕹️ Manually triggering PSG action: {actionName}");
            binding.onKeyPressed?.Invoke();
        }
    }
    
    public PSGKeyBinding GetKeyBinding(string actionName)
    {
        return System.Array.Find(psgBindings, b => b.actionName == actionName);
    }
}