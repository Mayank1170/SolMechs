using UnityEngine;
using MechBattle;

public class GameController : MonoBehaviour
{
    [Header("Mech GameObjects")]
    public GameObject playerMechObject;
    public GameObject enemyMechObject;

    private UIManager uiManager;
    private BattleManager battleManager;

    void Awake()
    {
        uiManager = GetComponent<UIManager>();
        battleManager = GetComponent<BattleManager>();
        MechUnit player = playerMechObject.GetComponent<MechUnitLoader>()?.GetUnitData();
        MechUnit enemy = enemyMechObject.GetComponent<MechUnitLoader>()?.GetUnitData();
        if (player == null || enemy == null)
        {
            Debug.LogError("⚠️ MechUnit data not loaded! Check MechUnitLoader.");
            return;
        }
        battleManager.Initialize(player, enemy, uiManager);
        uiManager.InitializeHealthBars(player, enemy, battleManager.playerMaxHPs, battleManager.enemyMaxHPs);
    }

    void Start()
    {
        uiManager.LogMessage("Battle start!\n" + $"{battleManager.playerUnit.Name} vs {battleManager.enemyUnit.Name}!\n\n");
        battleManager.StartTurn();
    }
}