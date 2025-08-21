using UnityEngine;
using MechBattle;

/// <summary>
/// GameController sets up a two-player local battle (Player 1 vs Player 2),
/// initializing mechs, UI, and starting the first turn.
/// </summary>
public class LocalGameController : MonoBehaviour
{
    [Header("Mech GameObjects")]
    public GameObject player1MechObject;
    public GameObject player2MechObject;

    private LocalUIManager uiManager;
    private LocalBattleManager battleManager;

    void Awake()
    {
        uiManager = GetComponent<LocalUIManager>();
        battleManager = GetComponent<LocalBattleManager>();

        // Load Player 1 and Player 2 MechUnits from their loaders
        MechUnit player1 = player1MechObject.GetComponent<MechUnitLoader>()?.GetUnitData();
        MechUnit player2 = player2MechObject.GetComponent<MechUnitLoader>()?.GetUnitData();

        if (player1 == null || player2 == null)
        {
            Debug.LogError("⚠️ MechUnit data not loaded! Check that MechUnitLoader is assigned.");
            return;
        }

        // Initialize battle manager with both players
        battleManager.Initialize(player1, player2, uiManager);

        // Set up health bars for both players
        uiManager.InitializeHealthBars(player1, player2, battleManager.player1MaxHPs, battleManager.player2MaxHPs);

        // Make sure sliders are not interactable
        uiManager.DisableSliderInteractabilityAndHandles();
    }

    void Start()
    {
        uiManager.LogMessage("Battle start!\n" +
            $"{battleManager.player1Unit.Name} vs {battleManager.player2Unit.Name}!\n\n");

        // Start with Player 1’s turn
        battleManager.StartTurn();
    }
}