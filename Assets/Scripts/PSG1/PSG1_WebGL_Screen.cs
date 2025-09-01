using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;

// PSG1 Screen for WebGL - Shows in browser instead of game
public class PSG1_WebGL_Screen : MonoBehaviour
{
    [Header("PSG1 WebGL Screen")]
    public Canvas psg1Canvas;
    public GameObject gameObjects; // Your existing game UI/objects
    
    [Header("PSG1 UI Elements")]
    public Text titleText;
    public Text statusText;
    public Text walletText;
    public Text instructionsText;
    public Button startGameButton;
    public Button testConnectionButton;
    
    [Header("PSG1 Settings")]
    public bool startWithPSG1Screen = true;
    public Color backgroundColor = new Color(0.05f, 0.05f, 0.15f, 1f);
    
    // JavaScript bridge for WebGL
    [DllImport("__Internal")]
    private static extern void LogToJS(string message);
    
    [DllImport("__Internal")]
    private static extern string GetWalletAddress();
    
    private PSG1_Manager psg1Manager;
    private bool isPSG1ScreenActive = true;
    private string currentWalletAddress = "";
    
    void Start()
    {
        Debug.Log("🌐 PSG1 WebGL Screen: Starting...");
        
        // Find PSG1 manager
        psg1Manager = FindObjectOfType<PSG1_Manager>();
        
        // Setup PSG1 screen
        CreatePSG1WebGLScreen();
        
        if (startWithPSG1Screen)
        {
            ShowPSG1Screen();
        }
        else
        {
            ShowGame();
        }
        
        #if UNITY_WEBGL && !UNITY_EDITOR
            LogToJS("PSG1_WEBGL_SCREEN_LOADED");
        #endif
    }
    
    void Update()
    {
        // Update wallet info
        UpdateWalletDisplay();
        
        // Handle ESC key to toggle screens
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleScreens();
        }
        
        // Handle SPACE to start game from PSG1 screen
        if (isPSG1ScreenActive && Input.GetKeyDown(KeyCode.Space))
        {
            StartGame();
        }
    }
    
    void CreatePSG1WebGLScreen()
    {
        Debug.Log("🎨 Creating PSG1 WebGL Screen...");
        
        // Create PSG1 Canvas if doesn't exist
        if (psg1Canvas == null)
        {
            GameObject canvasObj = new GameObject("PSG1_Canvas");
            psg1Canvas = canvasObj.AddComponent<Canvas>();
            psg1Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            psg1Canvas.sortingOrder = 100; // Above everything else
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Create background
        CreateBackground();
        
        // Create PSG1 UI elements
        CreatePSG1UI();
        
        Debug.Log("✅ PSG1 WebGL Screen created!");
    }
    
    void CreateBackground()
    {
        GameObject bgObj = new GameObject("PSG1_Background");
        bgObj.transform.SetParent(psg1Canvas.transform, false);
        
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = backgroundColor;
    }
    
    void CreatePSG1UI()
    {
        // Title
        CreateTitleText();
        
        // Status text
        CreateStatusText();
        
        // Wallet info
        CreateWalletText();
        
        // Instructions
        CreateInstructionsText();
        
        // Buttons
        CreateButtons();
    }
    
    void CreateTitleText()
    {
        GameObject titleObj = new GameObject("PSG1_Title");
        titleObj.transform.SetParent(psg1Canvas.transform, false);
        
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.8f);
        titleRect.anchorMax = new Vector2(0.5f, 0.8f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(1000, 100);
        
        titleText = titleObj.AddComponent<Text>();
        titleText.text = "🎮 SOLMECHS × PLAY SOLANA";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 48;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontStyle = FontStyle.Bold;
    }
    
    void CreateStatusText()
    {
        GameObject statusObj = new GameObject("PSG1_Status");
        statusObj.transform.SetParent(psg1Canvas.transform, false);
        
        RectTransform statusRect = statusObj.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0.65f);
        statusRect.anchorMax = new Vector2(0.5f, 0.65f);
        statusRect.pivot = new Vector2(0.5f, 0.5f);
        statusRect.sizeDelta = new Vector2(800, 60);
        
        statusText = statusObj.AddComponent<Text>();
        statusText.text = "🚀 Initializing Play Solana Gaming...";
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 24;
        statusText.color = Color.cyan;
        statusText.alignment = TextAnchor.MiddleCenter;
    }
    
    void CreateWalletText()
    {
        GameObject walletObj = new GameObject("PSG1_Wallet");
        walletObj.transform.SetParent(psg1Canvas.transform, false);
        
        RectTransform walletRect = walletObj.AddComponent<RectTransform>();
        walletRect.anchorMin = new Vector2(0.5f, 0.5f);
        walletRect.anchorMax = new Vector2(0.5f, 0.5f);
        walletRect.pivot = new Vector2(0.5f, 0.5f);
        walletRect.sizeDelta = new Vector2(700, 50);
        
        walletText = walletObj.AddComponent<Text>();
        walletText.text = "🔗 Connecting to wallet...";
        walletText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        walletText.fontSize = 20;
        walletText.color = Color.yellow;
        walletText.alignment = TextAnchor.MiddleCenter;
    }
    
    void CreateInstructionsText()
    {
        GameObject instructionsObj = new GameObject("PSG1_Instructions");
        instructionsObj.transform.SetParent(psg1Canvas.transform, false);
        
        RectTransform instructionsRect = instructionsObj.AddComponent<RectTransform>();
        instructionsRect.anchorMin = new Vector2(0.5f, 0.25f);
        instructionsRect.anchorMax = new Vector2(0.5f, 0.25f);
        instructionsRect.pivot = new Vector2(0.5f, 0.5f);
        instructionsRect.sizeDelta = new Vector2(900, 150);
        
        instructionsText = instructionsObj.AddComponent<Text>();
        instructionsText.text = "🕹️ PLAY SOLANA CONTROLS:\n\n" +
                               "SPACE - Start SolMechs Game\n" +
                               "C - Test PSG1 Connection\n" +
                               "W - Check Wallet Status\n" +
                               "ESC - Toggle PSG1/Game View";
        instructionsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        instructionsText.fontSize = 18;
        instructionsText.color = Color.white;
        instructionsText.alignment = TextAnchor.MiddleCenter;
    }
    
    void CreateButtons()
    {
        // Start Game Button
        GameObject startBtnObj = new GameObject("PSG1_StartButton");
        startBtnObj.transform.SetParent(psg1Canvas.transform, false);
        
        RectTransform startBtnRect = startBtnObj.AddComponent<RectTransform>();
        startBtnRect.anchorMin = new Vector2(0.35f, 0.1f);
        startBtnRect.anchorMax = new Vector2(0.35f, 0.1f);
        startBtnRect.pivot = new Vector2(0.5f, 0.5f);
        startBtnRect.sizeDelta = new Vector2(200, 50);
        
        Image startBtnImg = startBtnObj.AddComponent<Image>();
        startBtnImg.color = new Color(0.2f, 0.7f, 0.3f, 1f);
        
        startGameButton = startBtnObj.AddComponent<Button>();
        startGameButton.onClick.AddListener(StartGame);
        
        GameObject startBtnTextObj = new GameObject("StartButtonText");
        startBtnTextObj.transform.SetParent(startBtnObj.transform, false);
        RectTransform startBtnTextRect = startBtnTextObj.AddComponent<RectTransform>();
        startBtnTextRect.anchorMin = Vector2.zero;
        startBtnTextRect.anchorMax = Vector2.one;
        startBtnTextRect.offsetMin = Vector2.zero;
        startBtnTextRect.offsetMax = Vector2.zero;
        
        Text startBtnText = startBtnTextObj.AddComponent<Text>();
        startBtnText.text = "🚀 START GAME";
        startBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        startBtnText.fontSize = 16;
        startBtnText.color = Color.white;
        startBtnText.alignment = TextAnchor.MiddleCenter;
        startBtnText.fontStyle = FontStyle.Bold;
        
        // Test Connection Button
        GameObject testBtnObj = new GameObject("PSG1_TestButton");
        testBtnObj.transform.SetParent(psg1Canvas.transform, false);
        
        RectTransform testBtnRect = testBtnObj.AddComponent<RectTransform>();
        testBtnRect.anchorMin = new Vector2(0.65f, 0.1f);
        testBtnRect.anchorMax = new Vector2(0.65f, 0.1f);
        testBtnRect.pivot = new Vector2(0.5f, 0.5f);
        testBtnRect.sizeDelta = new Vector2(200, 50);
        
        Image testBtnImg = testBtnObj.AddComponent<Image>();
        testBtnImg.color = new Color(0.2f, 0.3f, 0.7f, 1f);
        
        testConnectionButton = testBtnObj.AddComponent<Button>();
        testConnectionButton.onClick.AddListener(TestConnection);
        
        GameObject testBtnTextObj = new GameObject("TestButtonText");
        testBtnTextObj.transform.SetParent(testBtnObj.transform, false);
        RectTransform testBtnTextRect = testBtnTextObj.AddComponent<RectTransform>();
        testBtnTextRect.anchorMin = Vector2.zero;
        testBtnTextRect.anchorMax = Vector2.one;
        testBtnTextRect.offsetMin = Vector2.zero;
        testBtnTextRect.offsetMax = Vector2.zero;
        
        Text testBtnText = testBtnTextObj.AddComponent<Text>();
        testBtnText.text = "🔧 TEST PSG1";
        testBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        testBtnText.fontSize = 16;
        testBtnText.color = Color.white;
        testBtnText.alignment = TextAnchor.MiddleCenter;
        testBtnText.fontStyle = FontStyle.Bold;
    }
    
    void UpdateWalletDisplay()
    {
        if (walletText == null) return;
        
        #if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                string wallet = GetWalletAddress();
                if (!string.IsNullOrEmpty(wallet) && wallet != currentWalletAddress)
                {
                    currentWalletAddress = wallet;
                    string displayWallet = wallet.Length > 16 ? 
                        wallet.Substring(0, 8) + "..." + wallet.Substring(wallet.Length - 8) : wallet;
                    
                    walletText.text = $"🔗 Connected: {displayWallet}";
                    walletText.color = Color.green;
                    
                    if (statusText != null)
                    {
                        statusText.text = "✅ Play Solana Ready - Wallet Connected!";
                        statusText.color = Color.green;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error getting wallet: {e.Message}");
            }
        #else
            // Editor mode
            if (walletText.text.Contains("Connecting"))
            {
                walletText.text = "🔗 Connected: DEMO...WALLET";
                walletText.color = Color.green;
                statusText.text = "✅ Play Solana Ready (Editor Mode)";
                statusText.color = Color.green;
            }
        #endif
    }
    
    public void ShowPSG1Screen()
    {
        Debug.Log("🌐 Showing PSG1 WebGL Screen");
        isPSG1ScreenActive = true;
        
        if (psg1Canvas != null)
            psg1Canvas.gameObject.SetActive(true);
            
        if (gameObjects != null)
            gameObjects.SetActive(false);
            
        #if UNITY_WEBGL && !UNITY_EDITOR
            LogToJS("PSG1_SCREEN_ACTIVE");
        #endif
    }
    
    public void ShowGame()
    {
        Debug.Log("🎮 Showing SolMechs Game");
        isPSG1ScreenActive = false;
        
        if (psg1Canvas != null)
            psg1Canvas.gameObject.SetActive(false);
            
        if (gameObjects != null)
            gameObjects.SetActive(true);
            
        #if UNITY_WEBGL && !UNITY_EDITOR
            LogToJS("SOLMECHS_GAME_ACTIVE");
        #endif
    }
    
    public void ToggleScreens()
    {
        if (isPSG1ScreenActive)
            ShowGame();
        else
            ShowPSG1Screen();
    }
    
    public void StartGame()
    {
        Debug.Log("🚀 Starting SolMechs from PSG1 screen");
        ShowGame();
    }
    
    public void TestConnection()
    {
        Debug.Log("🔧 Testing PSG1 connection");
        
        if (psg1Manager != null)
        {
            psg1Manager.TestPSG1Connection();
        }
        
        #if UNITY_WEBGL && !UNITY_EDITOR
            LogToJS("PSG1_CONNECTION_TEST");
        #endif
    }
}