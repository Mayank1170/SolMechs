using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// Web3Bridge - Handles communication between Unity and Web3 blockchain
/// Place this script on a GameObject in your scene to enable Web3 functionality
/// </summary>
public class Web3Bridge : MonoBehaviour
{
    [Header("Web3 Integration")]
    public bool enableWeb3 = true;
    public bool debugMode = true;
    
    // External JavaScript function calls (WebGL only)
    [DllImport("__Internal")]
    private static extern void mintReward(int amount);
    
    [DllImport("__Internal")]
    private static extern int getBalance();
    
    [DllImport("__Internal")]
    private static extern string getWalletAddress();
    
    [DllImport("__Internal")]
    private static extern void showWalletInfo();

    void Start()
    {
        if (debugMode)
        {
            Debug.Log("🌐 Web3Bridge initialized!");
            
            #if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("🔗 WebGL detected - Web3 functions available");
            #else
            Debug.Log("⚠️ Not running in WebGL - Web3 functions will be mocked");
            #endif
        }
    }

    /// <summary>
    /// Call this when player wins a battle
    /// </summary>
    public void OnPlayerWin()
    {
        if (debugMode) Debug.Log("🎉 Player won! Minting reward...");
        
        #if UNITY_WEBGL && !UNITY_EDITOR
        if (enableWeb3)
        {
            // Call Web3 function to mint 10 tokens
            mintReward(10);
        }
        #else
        Debug.Log("🧪 [Mock] Would mint 10 reward tokens");
        #endif
    }

    /// <summary>
    /// Call this when player loses a battle
    /// </summary>
    public void OnPlayerLose()
    {
        if (debugMode) Debug.Log("😔 Player lost the battle");
        
        // Could implement penalty or other logic here
        // For now, just log the event
    }

    /// <summary>
    /// Get player's current token balance
    /// </summary>
    public int GetPlayerBalance()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        if (enableWeb3)
        {
            int balance = getBalance();
            if (debugMode) Debug.Log($"💰 Player balance: {balance} tokens");
            return balance;
        }
        #endif
        
        // Mock balance for testing
        int mockBalance = 150;
        if (debugMode) Debug.Log($"🧪 [Mock] Player balance: {mockBalance} tokens");
        return mockBalance;
    }

    /// <summary>
    /// Get connected wallet address
    /// </summary>
    public string GetWalletAddress()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        if (enableWeb3)
        {
            string address = getWalletAddress();
            if (debugMode) Debug.Log($"🔗 Wallet address: {address}");
            return address;
        }
        #endif
        
        // Mock wallet for testing
        string mockWallet = "Demo...Wallet";
        if (debugMode) Debug.Log($"🧪 [Mock] Wallet address: {mockWallet}");
        return mockWallet;
    }

    /// <summary>
    /// Show wallet information popup
    /// </summary>
    public void ShowWalletInfo()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        if (enableWeb3)
        {
            showWalletInfo();
            return;
        }
        #endif
        
        Debug.Log("🔗 [Mock] Wallet info popup would show here");
    }

    /// <summary>
    /// Test all Web3 functions (for debugging)
    /// </summary>
    [ContextMenu("Test Web3 Functions")]
    public void TestWeb3Functions()
    {
        Debug.Log("🧪 Testing Web3 functions...");
        
        string wallet = GetWalletAddress();
        int balance = GetPlayerBalance();
        
        Debug.Log($"✅ Wallet: {wallet}");
        Debug.Log($"✅ Balance: {balance} tokens");
        
        OnPlayerWin(); // Test reward minting
    }

    // Called by Web3 bridge when connection is ready
    public void OnWeb3BridgeReady(string message)
    {
        if (debugMode) Debug.Log($"🌐 Web3 Bridge Ready: {message}");
    }

    // Example: Call this from your battle system when player wins
    public void HandleBattleVictory()
    {
        // Your existing victory logic here
        // ...
        
        // Then mint Web3 reward
        OnPlayerWin();
    }

    // Example: Call this from your battle system when player loses  
    public void HandleBattleDefeat()
    {
        // Your existing defeat logic here
        // ...
        
        // Log the loss
        OnPlayerLose();
    }
}

// Additional helper functions for different battle scenarios
public static class Web3BattleRewards
{
    public static void QuickBattleWin() => FindObjectOfType<Web3Bridge>()?.OnPlayerWin();
    public static void QuickBattleLose() => FindObjectOfType<Web3Bridge>()?.OnPlayerLose();
    public static int GetBalance() => FindObjectOfType<Web3Bridge>()?.GetPlayerBalance() ?? 0;
}