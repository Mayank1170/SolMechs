using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MechBattle;

/// <summary>
/// Extended UIManager for PVP with connection state handling.
/// Inherits from UIManager to keep all existing functionality.
/// </summary>
public class NetworkUIManager : UIManager
{
    [Header("PVP Connection UI")]
    [Tooltip("Panel shown while waiting for opponent")]
    public GameObject connectionPanel;

    [Tooltip("Text showing connection status")]
    public Text connectionStatusText;

    [Tooltip("Loading animation or spinner")]
    public GameObject loadingSpinner;

    [Header("PVP Player Info")]
    [Tooltip("Text showing local player name")]
    public Text localPlayerNameText;

    [Tooltip("Text showing opponent player name")]
    public Text opponentPlayerNameText;

    private bool isWaitingForOpponent = false;

    // ================== Connection State Management ==================

    /// <summary>
    /// Show waiting screen while opponent connects
    /// </summary>
    public void ShowWaitingForOpponent()
    {
        isWaitingForOpponent = true;

        if (connectionPanel)
            connectionPanel.SetActive(true);

        if (connectionStatusText)
            connectionStatusText.text = "Waiting for opponent...";

        if (loadingSpinner)
            loadingSpinner.SetActive(true);

        // Hide action buttons while waiting
        HideAllActionButtons();
        HideBackButton();

        Debug.Log("[NetworkUIManager] Showing waiting screen");
    }

    /// <summary>
    /// Hide waiting screen when opponent connects
    /// </summary>
    public void HideWaitingForOpponent()
    {
        isWaitingForOpponent = false;

        if (connectionPanel)
            connectionPanel.SetActive(false);

        if (loadingSpinner)
            loadingSpinner.SetActive(false);

        Debug.Log("[NetworkUIManager] Hiding waiting screen");
    }

    /// <summary>
    /// Update connection status text
    /// </summary>
    public void UpdateConnectionStatus(string message)
    {
        if (connectionStatusText)
            connectionStatusText.text = message;

        LogMessage(message);
    }

    // ================== Player Name Display ==================

    /// <summary>
    /// Set player names for PVP display
    /// </summary>
    public void SetPlayerNames(string localName, string opponentName)
    {
        if (localPlayerNameText)
            localPlayerNameText.text = localName;

        if (opponentPlayerNameText)
            opponentPlayerNameText.text = opponentName;

        Debug.Log($"[NetworkUIManager] Set names: {localName} vs {opponentName}");
    }

    // ================== Enhanced Button Rendering ==================

    /// <summary>
    /// Override to ensure buttons show correctly in PVP
    /// </summary>
    public new void RenderActionButtons(MechUnit unit, System.Action<ModuleSlot, AttackData> onAttackSelected)
    {
        // Don't show buttons if waiting for opponent
        if (isWaitingForOpponent)
        {
            Debug.Log("[NetworkUIManager] Skipping button render - waiting for opponent");
            return;
        }

        // Call base implementation
        base.RenderActionButtons(unit, onAttackSelected);

        Debug.Log($"[NetworkUIManager] Rendered {unit?.Name}'s action buttons");
    }

    /// <summary>
    /// Hide all action buttons (useful during opponent's turn)
    /// </summary>
    public void HideAllActionButtons()
    {
        if (actionButtons == null) return;

        foreach (var btn in actionButtons)
        {
            if (btn != null)
                btn.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Show a "waiting for opponent" message during their turn
    /// </summary>
    public void ShowOpponentTurnIndicator(string opponentName)
    {
        HideAllActionButtons();
        HideBackButton();

        // Optional: Show a visual indicator
        LogMessage($"--- {opponentName}'s Turn ---");
    }

    /// <summary>
    /// Show a "your turn" message when it's the local player's turn
    /// </summary>
    public void ShowYourTurnIndicator()
    {
        LogMessage("--- Your Turn ---");
    }

    // ================== Enhanced Initialization ==================

    /// <summary>
    /// Initialize for PVP with player names
    /// </summary>
    public void InitializeForPVP(MechUnit localPlayer, MechUnit remotePlayer,
        string localName, string remoteName,
        Dictionary<ModuleSlot, int> localMaxHPs,
        Dictionary<ModuleSlot, int> remoteMaxHPs)
    {
        // Set player names first
        SetPlayerNames(localName, remoteName);

        // Initialize health bars (use base implementation)
        base.InitializeHealthBars(localPlayer, remotePlayer, localMaxHPs, remoteMaxHPs);

        Debug.Log("[NetworkUIManager] Initialized for PVP");
    }

    // ================== Connection Status Helpers ==================

    /// <summary>
    /// Show a temporary status message overlay
    /// </summary>
    public void ShowStatusMessage(string message, float duration = 2f)
    {
        StartCoroutine(ShowStatusMessageCoroutine(message, duration));
    }

    private IEnumerator ShowStatusMessageCoroutine(string message, float duration)
    {
        if (connectionStatusText)
        {
            connectionStatusText.text = message;

            if (connectionPanel && !connectionPanel.activeSelf)
            {
                connectionPanel.SetActive(true);
                yield return new WaitForSeconds(duration);
                connectionPanel.SetActive(false);
            }
            else
            {
                yield return new WaitForSeconds(duration);
            }
        }
    }

    // ================== Disconnect Handling ==================

    /// <summary>
    /// Show disconnect message
    /// </summary>
    public void ShowDisconnectMessage(string playerName)
    {
        LogMessage($"\n>>> {playerName} disconnected! <<<\n");
        ShowStatusMessage($"{playerName} disconnected!", 3f);

        // Disable all interactive elements
        DisableAllButtons();
    }

    // ================== Turn Timer Display Enhancement ==================

    /// <summary>
    /// Highlight the active player's timer
    /// </summary>
    public void HighlightActiveTimer(bool isLocalPlayerTurn)
    {
        // This is handled by BattleTurnTimer, but we can add extra visual feedback here
        // For example, pulse effect, color change, etc.

        Debug.Log($"[NetworkUIManager] Active timer: {(isLocalPlayerTurn ? "Local Player" : "Opponent")}");
    }

    // ================== Enhanced Battle Result ==================

    /// <summary>
    /// Show battle result with PVP context
    /// </summary>
    public void ShowPVPBattleResult(bool playerWon, string opponentName, int points = 0)
    {
        string resultMessage = playerWon
            ? $"Victory!\nYou defeated {opponentName}!"
            : $"Defeat!\n{opponentName} won!";

        LogMessage($"\n{resultMessage}\n");

        // Call base method
        base.ShowBattleResult(playerWon, points);
    }

    // ================== Debug Helpers ==================

    [ContextMenu("Test Show Waiting")]
    private void TestShowWaiting()
    {
        ShowWaitingForOpponent();
    }

    [ContextMenu("Test Hide Waiting")]
    private void TestHideWaiting()
    {
        HideWaitingForOpponent();
    }

    [ContextMenu("Test Show Your Turn")]
    private void TestYourTurn()
    {
        ShowYourTurnIndicator();
    }

    [ContextMenu("Test Show Opponent Turn")]
    private void TestOpponentTurn()
    {
        ShowOpponentTurnIndicator("TestOpponent");
    }
}