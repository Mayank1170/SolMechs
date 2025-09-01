using UnityEngine;
using System.Runtime.InteropServices;

// MILESTONE 2 - PSG1 Bridge Manager
public class PSG1_Manager : MonoBehaviour
{
    [Header("MILESTONE 2 - PSG1 Bridge")]
    public bool isPSG1Ready = false;
    public string walletAddress = "";
    public string userBalance = "";
    
    [Header("Testing Controls")]
    public KeyCode testConnectionKey = KeyCode.T;
    public KeyCode testWalletKey = KeyCode.W;
    public KeyCode testNFTKey = KeyCode.N;
    
    // JavaScript bridge functions for WebGL
    [DllImport("__Internal")]
    private static extern string GetWalletAddress();
    
    [DllImport("__Internal")]
    private static extern string GetUserBalance();
    
    [DllImport("__Internal")]
    private static extern void LogToJS(string message);
    
    void Start()
    {
        Debug.Log("🚀 MILESTONE 2: PSG1 Manager starting...");
        
        // Initialize PSG1 after a short delay
        Invoke(nameof(InitializePSG1), 1.5f);
    }
    
    void Update()
    {
        // Test controls
        if (Input.GetKeyDown(testConnectionKey))
        {
            TestPSG1Connection();
        }
        
        if (Input.GetKeyDown(testWalletKey))
        {
            TestWalletConnection();
        }
        
        if (Input.GetKeyDown(testNFTKey))
        {
            TestNFTMinting();
        }
    }
    
    void InitializePSG1()
    {
        Debug.Log("⚙️ MILESTONE 2: Initializing PSG1 bridge...");
        
        try
        {
            // Try to use actual PSG1 SDK if available
            var psg1Type = System.Type.GetType("PlaySolana.Unity.SDK.PSG1Manager");
            if (psg1Type != null)
            {
                Debug.Log("✅ MILESTONE 2: PSG1 SDK detected, using native PSG1");
                InitializeNativePSG1();
            }
            else
            {
                Debug.Log("🌐 MILESTONE 2: Using WebGL bridge mode");
                InitializeBridgeMode();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ MILESTONE 2: PSG1 initialization failed: {e.Message}");
            Debug.Log("🌐 MILESTONE 2: Falling back to bridge mode");
            InitializeBridgeMode();
        }
    }
    
    void InitializeNativePSG1()
    {
        Debug.Log("🎮 MILESTONE 2: Initializing native PSG1 SDK...");
        
        // Here we would initialize the actual PSG1 SDK
        // For now, we'll use bridge mode to ensure compatibility
        InitializeBridgeMode();
    }
    
    void InitializeBridgeMode()
    {
        Debug.Log("🌐 MILESTONE 2: Initializing PSG1 bridge mode...");
        
        #if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                // Get wallet from Next.js bridge
                walletAddress = GetWalletAddress();
                
                if (!string.IsNullOrEmpty(walletAddress))
                {
                    isPSG1Ready = true;
                    LogToJS("MILESTONE2: PSG1 Manager initialized successfully!");
                    Debug.Log("✅ MILESTONE 2: PSG1 bridge ready!");
                }
                else
                {
                    Debug.LogWarning("⚠️ MILESTONE 2: No wallet address from bridge");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ MILESTONE 2: Bridge initialization failed: {e.Message}");
            }
        #else
            // Editor mode - mock data
            walletAddress = "DEMO_PSG1_WALLET_EDITOR";
            isPSG1Ready = true;
            Debug.Log("🖥️ MILESTONE 2: PSG1 initialized (Editor mode)");
        #endif
    }
    
    // Test functions
    [ContextMenu("Test PSG1 Connection")]
    public void TestPSG1Connection()
    {
        Debug.Log("🧪 MILESTONE 2: Testing PSG1 connection...");
        
        if (isPSG1Ready)
        {
            Debug.Log($"✅ PSG1 Status: READY");
            Debug.Log($"✅ Wallet: {walletAddress}");
            
            #if UNITY_WEBGL && !UNITY_EDITOR
                LogToJS("MILESTONE2_TEST: PSG1 connection test successful!");
            #endif
        }
        else
        {
            Debug.LogError("❌ PSG1 Status: NOT READY");
        }
    }
    
    [ContextMenu("Test Wallet Connection")]
    public void TestWalletConnection()
    {
        Debug.Log("🔗 MILESTONE 2: Testing wallet connection...");
        
        if (!isPSG1Ready)
        {
            Debug.LogError("❌ PSG1 not ready for wallet test");
            return;
        }
        
        #if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                userBalance = GetUserBalance();
                Debug.Log($"💰 User Balance: {userBalance}");
                LogToJS($"MILESTONE2_WALLET: Wallet test - Address: {walletAddress}, Balance: {userBalance}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Wallet test failed: {e.Message}");
            }
        #else
            userBalance = "999";
            Debug.Log($"💰 User Balance: {userBalance} (Editor)");
        #endif
    }
    
    [ContextMenu("Test NFT Minting")]
    public void TestNFTMinting()
    {
        Debug.Log("🎨 MILESTONE 2: Testing NFT minting...");
        
        if (!isPSG1Ready)
        {
            Debug.LogError("❌ PSG1 not ready for NFT minting");
            return;
        }
        
        string nftData = "{'type': 'test', 'name': 'SolMechs Test NFT', 'description': 'MILESTONE 2 test NFT'}";
        
        #if UNITY_WEBGL && !UNITY_EDITOR
            LogToJS($"NFT_MINT_REQUEST:{nftData}");
            Debug.Log("🎨 NFT mint request sent to bridge");
        #else
            Debug.Log($"🎨 NFT mint request: {nftData} (Editor)");
        #endif
    }
    
    // Public functions for other scripts
    public bool IsPSG1Ready()
    {
        return isPSG1Ready;
    }
    
    public string GetConnectedWallet()
    {
        return walletAddress;
    }
    
    public void RequestNFTMint(string nftMetadata)
    {
        if (!isPSG1Ready)
        {
            Debug.LogError("❌ PSG1 not ready for NFT mint request");
            return;
        }
        
        Debug.Log($"🎨 MILESTONE 2: NFT mint requested: {nftMetadata}");
        
        #if UNITY_WEBGL && !UNITY_EDITOR
            LogToJS($"NFT_MINT_REQUEST:{nftMetadata}");
        #else
            Debug.Log("🎨 NFT mint request (Editor mode)");
        #endif
    }
    
    public void OnGameWin()
    {
        Debug.Log("🎉 MILESTONE 2: Player won - triggering PSG1 reward!");
        
        string victoryNFT = "{'type': 'victory', 'game': 'SolMechs', 'timestamp': '" + System.DateTime.Now.ToString() + "'}";
        RequestNFTMint(victoryNFT);
    }
}