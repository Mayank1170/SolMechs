using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// PSG Welcome Screen - Shows before main game
public class PSG_WelcomeScreen : MonoBehaviour
{
    [Header("PSG Welcome Screen")]
    public GameObject psgWelcomePanel;
    public GameObject gameUI;
    public Text welcomeText;
    public Text instructionText;
    public Text walletInfoText;
    public Button continueButton;
    
    [Header("PSG Integration")]
    public PSG1_Manager psg1Manager;
    
    [Header("Settings")]
    public float autoHideDelay = 5f; // Auto hide after 5 seconds
    public bool showWelcomeScreen = true;
    
    private bool hasShownWelcome = false;
    
    void Start()
    {
        Debug.Log("🎮 PSG Welcome Screen: Starting...");
        
        // Find PSG1 Manager if not assigned
        if (psg1Manager == null)
        {
            psg1Manager = FindObjectOfType<PSG1_Manager>();
        }
        
        // Setup UI
        SetupWelcomeScreen();
        
        if (showWelcomeScreen && !hasShownWelcome)
        {
            ShowPSGWelcome();
        }
        else
        {
            ShowGameUI();
        }
    }
    
    void SetupWelcomeScreen()
    {
        // Create welcome panel if it doesn't exist
        if (psgWelcomePanel == null)
        {
            CreateWelcomeScreenUI();
        }
        
        // Setup continue button
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        
        // Update welcome text
        UpdateWelcomeText();
    }
    
    void CreateWelcomeScreenUI()
    {
        Debug.Log("🎨 Creating PSG Welcome Screen UI...");
        
        // Create main welcome panel
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj;
            canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<GraphicRaycaster>();
        }
        
        // Create welcome panel
        GameObject welcomePanel = new GameObject("PSG_WelcomePanel");
        welcomePanel.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = welcomePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        Image panelImage = welcomePanel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.2f, 0.95f); // Dark blue background
        
        psgWelcomePanel = welcomePanel;
        
        // Create welcome text
        CreateWelcomeText(welcomePanel);
        
        Debug.Log("✅ PSG Welcome Screen UI created!");
    }
    
    void CreateWelcomeText(GameObject parent)
    {
        // Main welcome text
        GameObject welcomeTextObj = new GameObject("WelcomeText");
        welcomeTextObj.transform.SetParent(parent.transform, false);
        
        RectTransform welcomeRect = welcomeTextObj.AddComponent<RectTransform>();
        welcomeRect.anchorMin = new Vector2(0.5f, 0.7f);
        welcomeRect.anchorMax = new Vector2(0.5f, 0.7f);
        welcomeRect.pivot = new Vector2(0.5f, 0.5f);
        welcomeRect.sizeDelta = new Vector2(800, 100);
        
        Text welcomeTextComp = welcomeTextObj.AddComponent<Text>();
        welcomeTextComp.text = "🎮 WELCOME TO SOLMECHS PSG";
        welcomeTextComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        welcomeTextComp.fontSize = 32;
        welcomeTextComp.color = Color.white;
        welcomeTextComp.alignment = TextAnchor.MiddleCenter;
        welcomeText = welcomeTextComp;
        
        // Instruction text
        GameObject instructionTextObj = new GameObject("InstructionText");
        instructionTextObj.transform.SetParent(parent.transform, false);
        
        RectTransform instructionRect = instructionTextObj.AddComponent<RectTransform>();
        instructionRect.anchorMin = new Vector2(0.5f, 0.5f);
        instructionRect.anchorMax = new Vector2(0.5f, 0.5f);
        instructionRect.pivot = new Vector2(0.5f, 0.5f);
        instructionRect.sizeDelta = new Vector2(600, 200);
        
        Text instructionTextComp = instructionTextObj.AddComponent<Text>();
        instructionTextComp.text = "🕹️ Play Solana Integration Active\n\n" +
                                   "Press SPACE to continue to game\n" +
                                   "Press P to test PSG1 connection\n" +
                                   "Press ESC to show/hide this screen";
        instructionTextComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        instructionTextComp.fontSize = 18;
        instructionTextComp.color = Color.cyan;
        instructionTextComp.alignment = TextAnchor.MiddleCenter;
        instructionText = instructionTextComp;
        
        // Wallet info text
        GameObject walletTextObj = new GameObject("WalletInfoText");
        walletTextObj.transform.SetParent(parent.transform, false);
        
        RectTransform walletRect = walletTextObj.AddComponent<RectTransform>();
        walletRect.anchorMin = new Vector2(0.5f, 0.3f);
        walletRect.anchorMax = new Vector2(0.5f, 0.3f);
        walletRect.pivot = new Vector2(0.5f, 0.5f);
        walletRect.sizeDelta = new Vector2(700, 60);
        
        Text walletTextComp = walletTextObj.AddComponent<Text>();
        walletTextComp.text = "🔗 Connecting to wallet...";
        walletTextComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        walletTextComp.fontSize = 16;
        walletTextComp.color = Color.yellow;
        walletTextComp.alignment = TextAnchor.MiddleCenter;
        walletInfoText = walletTextComp;
    }
    
    void Update()
    {
        // Handle input
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            OnContinueClicked();
        }
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePSGScreen();
        }
        
        if (Input.GetKeyDown(KeyCode.P))
        {
            TestPSG1Connection();
        }
        
        // Update wallet info
        UpdateWalletInfo();
    }
    
    void UpdateWelcomeText()
    {
        if (welcomeText != null)
        {
            welcomeText.text = "🎮 WELCOME TO SOLMECHS PSG\nPlay Solana Gaming Platform";
        }
    }
    
    void UpdateWalletInfo()
    {
        if (walletInfoText == null) return;
        
        if (psg1Manager != null && psg1Manager.IsPSG1Ready())
        {
            string wallet = psg1Manager.GetWalletAddress();
            string displayWallet = wallet.Length > 16 ? 
                wallet.Substring(0, 8) + "..." + wallet.Substring(wallet.Length - 8) : wallet;
            walletInfoText.text = $"🔗 Wallet Connected: {displayWallet}\n✅ PSG1 Ready!";
            walletInfoText.color = Color.green;
        }
        else
        {
            walletInfoText.text = "🔗 Connecting to Play Solana...";
            walletInfoText.color = Color.yellow;
        }
    }
    
    public void ShowPSGWelcome()
    {
        Debug.Log("🎮 Showing PSG Welcome Screen");
        
        if (psgWelcomePanel != null)
        {
            psgWelcomePanel.SetActive(true);
        }
        
        if (gameUI != null)
        {
            gameUI.SetActive(false);
        }
        
        hasShownWelcome = true;
        
        // Auto-hide after delay if no interaction
        if (autoHideDelay > 0)
        {
            Invoke(nameof(AutoHideWelcome), autoHideDelay);
        }
    }
    
    public void ShowGameUI()
    {
        Debug.Log("🎮 Showing Game UI");
        
        if (psgWelcomePanel != null)
        {
            psgWelcomePanel.SetActive(false);
        }
        
        if (gameUI != null)
        {
            gameUI.SetActive(true);
        }
    }
    
    public void TogglePSGScreen()
    {
        if (psgWelcomePanel != null)
        {
            bool isActive = psgWelcomePanel.activeSelf;
            if (isActive)
            {
                ShowGameUI();
            }
            else
            {
                ShowPSGWelcome();
            }
        }
    }
    
    void AutoHideWelcome()
    {
        Debug.Log("🕐 Auto-hiding PSG welcome screen");
        ShowGameUI();
    }
    
    public void OnContinueClicked()
    {
        Debug.Log("🎮 Continue clicked - starting game");
        ShowGameUI();
    }
    
    void TestPSG1Connection()
    {
        Debug.Log("🧪 Testing PSG1 from welcome screen");
        if (psg1Manager != null)
        {
            psg1Manager.TestPSG1Connection();
        }
        else
        {
            Debug.LogWarning("⚠️ PSG1 Manager not found");
        }
    }
    
    // Public methods for external control
    public void SetWelcomeScreenEnabled(bool enabled)
    {
        showWelcomeScreen = enabled;
    }
    
    public bool IsWelcomeScreenActive()
    {
        return psgWelcomePanel != null && psgWelcomePanel.activeSelf;
    }
}