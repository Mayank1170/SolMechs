using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices;

// PSG1 WebGL Controller - Handles PSG1 screen and scene management
public class PSG1_WebGL_Controller : MonoBehaviour
{
    [Header("PSG1 WebGL Setup")]
    public bool enablePSG1InWebGL = true;
    public bool forceStartWithPSG1 = true;
    
    [Header("Scene Management")]
    public string gameSceneName = "3_PilotMechBattleV1"; // Your main game scene
    public string menuSceneName = "1_Main_Menu"; // Your main menu scene
    
    [Header("UI References")]
    public Canvas mainGameCanvas;
    public GameObject[] gameUIObjects;
    
    private GameObject psg1Screen;
    private bool isPSG1Active = false;
    private PSG1_Manager psg1Manager;
    
    // JavaScript bridge for WebGL
    [DllImport("__Internal")]
    private static extern void LogToJS(string message);
    
    [DllImport("__Internal")]
    private static extern string GetWalletAddress();
    
    void Awake()
    {
        // This runs before Start() to ensure PSG1 is set up first
        Debug.Log("🌐 PSG1 WebGL Controller: Initializing...");
        
        #if UNITY_WEBGL && !UNITY_EDITOR
            if (enablePSG1InWebGL)
            {
                SetupPSG1ForWebGL();
            }
        #else
            Debug.Log("🖥️ PSG1 Controller: Running in Editor - PSG1 disabled");
            enablePSG1InWebGL = false;
        #endif
    }
    
    void Start()
    {
        // Find PSG1 manager
        psg1Manager = FindObjectOfType<PSG1_Manager>();
        
        #if UNITY_WEBGL && !UNITY_EDITOR
            if (enablePSG1InWebGL && forceStartWithPSG1)
            {
                ShowPSG1Screen();
                LogToJS("PSG1_WEBGL_SCREEN_LOADED");
            }
            else
            {
                ShowGameContent();
            }
        #else
            // In Editor, always show game content
            ShowGameContent();
        #endif
    }
    
    void Update()
    {
        // Handle input for PSG1
        HandlePSG1Input();
    }
    
    void HandlePSG1Input()
    {
        if (!enablePSG1InWebGL) return;
        
        // Toggle PSG1 screen with ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePSG1Screen();
        }
        
        // Start game with SPACE when PSG1 is active
        if (isPSG1Active && Input.GetKeyDown(KeyCode.Space))
        {
            StartGame();
        }
        
        // Test PSG1 connection with C
        if (isPSG1Active && Input.GetKeyDown(KeyCode.C))
        {
            TestPSG1Connection();
        }
    }
    
    void SetupPSG1ForWebGL()
    {
        Debug.Log("🎮 Setting up PSG1 screen for WebGL...");
        
        // Create PSG1 screen UI
        CreatePSG1Screen();
        
        // Hide game content initially
        if (forceStartWithPSG1)
        {
            HideGameContent();
        }
    }
    
    void CreatePSG1Screen()
    {
        // Create PSG1 Canvas
        GameObject canvasObj = new GameObject("PSG1_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // Above everything
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        psg1Screen = canvasObj;
        
        // Create background
        CreatePSG1Background(canvasObj);
        
        // Create UI elements
        CreatePSG1UI(canvasObj);
        
        Debug.Log("✅ PSG1 Screen created for WebGL");
    }
    
    void CreatePSG1Background(GameObject parent)
    {
        GameObject bgObj = new GameObject("PSG1_Background");
        bgObj.transform.SetParent(parent.transform, false);
        
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.05f, 0.15f, 0.95f); // Dark blue background
    }
    
    void CreatePSG1UI(GameObject parent)
    {
        // Title
        CreateUIText(parent, "PSG1_Title", "🎮 SOLMECHS × PLAY SOLANA", 
                    new Vector2(0.5f, 0.8f), new Vector2(1000, 100), 48, Color.white, FontStyle.Bold);
        
        // Status
        CreateUIText(parent, "PSG1_Status", "🚀 Play Solana Gaming Platform Ready!", 
                    new Vector2(0.5f, 0.65f), new Vector2(800, 60), 24, Color.cyan, FontStyle.Normal);
        
        // Wallet info
        CreateUIText(parent, "PSG1_Wallet", "🔗 Connecting to wallet...", 
                    new Vector2(0.5f, 0.5f), new Vector2(700, 50), 20, Color.yellow, FontStyle.Normal);
        
        // Instructions
        CreateUIText(parent, "PSG1_Instructions", 
                    "🕹️ CONTROLS:\n\nSPACE - Start SolMechs Game\nC - Test PSG1 Connection\nW - Check Wallet\nESC - Toggle View", 
                    new Vector2(0.5f, 0.3f), new Vector2(900, 150), 18, Color.white, FontStyle.Normal);
        
        // Start Game Button
        CreatePSG1Button(parent, "🚀 START GAME", new Vector2(0.35f, 0.1f), Color.green, () => StartGame());
        
        // Test Button
        CreatePSG1Button(parent, "🔧 TEST PSG1", new Vector2(0.65f, 0.1f), Color.blue, () => TestPSG1Connection());
    }
    
    GameObject CreateUIText(GameObject parent, string name, string text, Vector2 anchorPos, Vector2 size, int fontSize, Color color, FontStyle style)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent.transform, false);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorPos;
        rect.anchorMax = anchorPos;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        
        Text textComp = textObj.AddComponent<Text>();
        textComp.text = text;
        textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComp.fontSize = fontSize;
        textComp.color = color;
        textComp.alignment = TextAnchor.MiddleCenter;
        textComp.fontStyle = style;
        
        return textObj;
    }
    
    void CreatePSG1Button(GameObject parent, string buttonText, Vector2 anchorPos, Color color, System.Action onClick)
    {
        GameObject btnObj = new GameObject("PSG1_Button_" + buttonText.Replace(" ", ""));
        btnObj.transform.SetParent(parent.transform, false);
        
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = anchorPos;
        btnRect.anchorMax = anchorPos;
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.sizeDelta = new Vector2(200, 50);
        
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = color;
        
        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick());
        
        // Button text
        GameObject btnTextObj = new GameObject("ButtonText");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        
        RectTransform btnTextRect = btnTextObj.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;
        
        Text btnTextComp = btnTextObj.AddComponent<Text>();
        btnTextComp.text = buttonText;
        btnTextComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnTextComp.fontSize = 16;
        btnTextComp.color = Color.white;
        btnTextComp.alignment = TextAnchor.MiddleCenter;
        btnTextComp.fontStyle = FontStyle.Bold;
    }
    
    public void ShowPSG1Screen()
    {
        Debug.Log("🌐 Showing PSG1 Screen");
        
        if (psg1Screen != null)
        {
            psg1Screen.SetActive(true);
            isPSG1Active = true;
            
            #if UNITY_WEBGL && !UNITY_EDITOR
                LogToJS("PSG1_SCREEN_ACTIVE");
            #endif
        }
        
        HideGameContent();
    }
    
    public void ShowGameContent()
    {
        Debug.Log("🎮 Showing Game Content");
        
        if (psg1Screen != null)
        {
            psg1Screen.SetActive(false);
            isPSG1Active = false;
            
            #if UNITY_WEBGL && !UNITY_EDITOR
                LogToJS("SOLMECHS_GAME_ACTIVE");
            #endif
        }
        
        ShowGameUI();
    }
    
    void HideGameContent()
    {
        // Hide main game canvas
        if (mainGameCanvas != null)
        {
            mainGameCanvas.gameObject.SetActive(false);
        }
        
        // Hide game UI objects
        foreach (GameObject obj in gameUIObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }
    
    void ShowGameUI()
    {
        // Show main game canvas
        if (mainGameCanvas != null)
        {
            mainGameCanvas.gameObject.SetActive(true);
        }
        
        // Show game UI objects
        foreach (GameObject obj in gameUIObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
    }
    
    public void TogglePSG1Screen()
    {
        if (isPSG1Active)
        {
            ShowGameContent();
        }
        else
        {
            ShowPSG1Screen();
        }
    }
    
    public void StartGame()
    {
        Debug.Log("🚀 PSG1: Starting SolMechs game...");
        
        #if UNITY_WEBGL && !UNITY_EDITOR
            LogToJS("PSG1_STARTING_GAME");
        #endif
        
        // For WebGL, just show the game content instead of changing scenes
        // because scene changes can be problematic in WebGL
        ShowGameContent();
        
        // If you really need to change scenes in WebGL, use this:
        // StartCoroutine(LoadSceneAsync(gameSceneName));
    }
    
    public void TestPSG1Connection()
    {
        Debug.Log("🔧 Testing PSG1 Connection...");
        
        if (psg1Manager != null)
        {
            psg1Manager.TestPSG1Connection();
        }
        
        #if UNITY_WEBGL && !UNITY_EDITOR
            LogToJS("PSG1_CONNECTION_TEST");
        #endif
    }
    
    // For scene transitions in WebGL (if needed)
    System.Collections.IEnumerator LoadSceneAsync(string sceneName)
    {
        Debug.Log($"🎮 Loading scene: {sceneName}");
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        Debug.Log($"✅ Scene loaded: {sceneName}");
    }
}