using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MechBattle;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

/// <summary>
/// Complete Photon-enabled PVP BattleManager.
/// This is a SINGLE, COMPLETE file - just copy and use!
/// Replaces BattleManager.cs for PVP battles.
/// </summary>
public class NetworkBattleManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    // === Network Event Codes ===
    private const byte ATTACK_SELECTED_EVENT = 1;
    private const byte TARGET_CONFIRMED_EVENT = 2;
    private const byte BATTLE_END_EVENT = 7;

    // === Loaders and UI ===
    public MechUnitLoader playerLoader;
    public MechUnitLoader enemyLoader;
    public UIManager ui; // Can be UIManager or NetworkUIManager

    public MechUnit localPlayerUnit;
    public MechUnit remotePlayerUnit;

    private AttackData selectedAttack;
    private ModuleSlot selectedSourceSlot;
    private UIManager uiManager;
    private NetworkUIManager networkUI; // Enhanced UI if available

    public Dictionary<ModuleSlot, int> localPlayerMaxHPs = new Dictionary<ModuleSlot, int>();
    public Dictionary<ModuleSlot, int> remotePlayerMaxHPs = new Dictionary<ModuleSlot, int>();

    private Dictionary<MechUnit, Dictionary<string, int>> matrixBuffsDict = new Dictionary<MechUnit, Dictionary<string, int>>();

    private enum BattleState
    {
        WaitingForOpponent,
        MyTurn,
        OpponentTurn,
        SelectingAttack,
        SelectingTarget,
        SelectingSelfTarget,
        ProcessingAction,
        Victory,
        Defeat
    }
    private BattleState currentState;
    private bool battleOver = false;

    // === TIMER ===
    [SerializeField] private BattleTurnTimer turnTimer;

    // === FX pacing ===
    [Header("FX Pacing")]
    [SerializeField] private float attackFxDuration = 0.80f;
    [SerializeField] private float fxDestroyDelay = 0.12f;

    // === Network State ===
    private bool isMyTurn = false;
    private PhotonView photonView;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
        if (photonView == null)
        {
            photonView = gameObject.AddComponent<PhotonView>();
        }
    }

    private void Start()
    {
        uiManager = uiManager != null ? uiManager : (ui != null ? ui : FindObjectOfType<UIManager>());

        // Check if we have the enhanced NetworkUIManager
        networkUI = uiManager as NetworkUIManager;

        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogError("[NetworkBattleManager] Not connected to Photon room!");
            return;
        }

        // Determine who goes first (MasterClient starts)
        isMyTurn = PhotonNetwork.IsMasterClient;

        if (playerLoader != null) localPlayerUnit = playerLoader.GetUnitData();

        if (localPlayerUnit == null || uiManager == null)
        {
            Debug.LogError("[NetworkBattleManager] Missing local unit or UIManager!");
            return;
        }

        // Wait for opponent to load
        StartCoroutine(WaitForOpponentAndInitialize());
    }

    private IEnumerator WaitForOpponentAndInitialize()
    {
        currentState = BattleState.WaitingForOpponent;

        // Show waiting UI
        if (networkUI != null)
        {
            networkUI.ShowWaitingForOpponent();
            networkUI.UpdateConnectionStatus("Waiting for opponent to connect...");
        }
        else
        {
            uiManager.LogMessage("Waiting for opponent...");
        }

        // Wait for both players to be in room
        while (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Update status
        if (networkUI != null)
        {
            networkUI.UpdateConnectionStatus("Opponent found! Loading battle...");
        }

        yield return new WaitForSeconds(1f); // Give time for opponent's loader to initialize

        // Try to create remote player unit with retry mechanism
        int retryCount = 0;
        int maxRetries = 5;
        
        while (retryCount < maxRetries)
        {
            remotePlayerUnit = CreateRemotePlayerUnit();
            
            if (remotePlayerUnit != null)
            {
                Debug.Log($"[NetworkBattleManager] ✅ Successfully created remote player unit on attempt {retryCount + 1}");
                break;
            }
            
            retryCount++;
            Debug.LogWarning($"[NetworkBattleManager] Failed to create remote player unit (attempt {retryCount}/{maxRetries}). Retrying...");
            
            if (networkUI != null)
            {
                networkUI.UpdateConnectionStatus($"Loading opponent's mech... (attempt {retryCount}/{maxRetries})");
            }
            
            yield return new WaitForSeconds(2f); // Wait longer between retries
        }

        if (remotePlayerUnit == null)
        {
            Debug.LogError("[NetworkBattleManager] Failed to create remote player unit after all retries!");
            if (networkUI != null)
                networkUI.UpdateConnectionStatus("Error: Could not load opponent's mech after multiple attempts!");
            yield break;
        }

        // Hide waiting UI
        if (networkUI != null)
        {
            networkUI.HideWaitingForOpponent();
        }

        Initialize(localPlayerUnit, remotePlayerUnit, uiManager);
        uiManager.DisableSliderInteractabilityAndHandles();

        // Use enhanced initialization if available
        if (networkUI != null)
        {
            networkUI.InitializeForPVP(
                localPlayerUnit,
                remotePlayerUnit,
                PhotonNetwork.LocalPlayer.NickName,
                GetOpponentPlayer()?.NickName ?? "Opponent",
                localPlayerMaxHPs,
                remotePlayerMaxHPs
            );
            networkUI.InitializePaperDolls(playerLoader, enemyLoader);
        }
        else
        {
            uiManager.InitializeHealthBars(localPlayerUnit, remotePlayerUnit, localPlayerMaxHPs, remotePlayerMaxHPs);
            uiManager.InitializePaperDolls(playerLoader, enemyLoader);
        }

        // TIMER setup
        if (turnTimer)
        {
            turnTimer.Initialize();
            turnTimer.onPlayerFlagFall.AddListener(OnLocalPlayerTimeout);
            turnTimer.onEnemyFlagFall.AddListener(OnRemotePlayerTimeout);
        }

        uiManager.LogMessage($"Battle start!\n{localPlayerUnit.Name} vs {remotePlayerUnit.Name}!\n");

        StartTurn();
    }

    private MechUnit CreateRemotePlayerUnit()
    {
        // Get opponent's build data from Custom Properties
        Player opponent = GetOpponentPlayer();
        if (opponent == null)
        {
            Debug.LogError("[NetworkBattleManager] No opponent found!");
            Debug.LogError($"Current room player count: {PhotonNetwork.CurrentRoom?.PlayerCount}");
            return null;
        }

        Debug.Log($"[NetworkBattleManager] Found opponent: {opponent.NickName}");
        Debug.Log($"[NetworkBattleManager] Opponent properties: {opponent.CustomProperties.ToStringFull()}");

        object matrixObj, raObj, laObj, lbObj;
        if (!opponent.CustomProperties.TryGetValue("matrixId", out matrixObj) ||
            !opponent.CustomProperties.TryGetValue("rightArmId", out raObj) ||
            !opponent.CustomProperties.TryGetValue("leftArmId", out laObj) ||
            !opponent.CustomProperties.TryGetValue("lowerBodyId", out lbObj))
        {
            Debug.LogError("[NetworkBattleManager] Opponent's mech data not found in CustomProperties!");
            Debug.LogError("Available properties:");
            foreach (var key in opponent.CustomProperties.Keys)
            {
                Debug.LogError($"  {key}: {opponent.CustomProperties[key]}");
            }

            Debug.LogError("\n⚠️ SOLUTION: Make sure PhotonConnectionManager.ShareMechData() is called BEFORE loading battle scene!");
            Debug.LogError("⚠️ Check that opponent set CustomProperties with their mech build data!");
            return null;
        }

        Debug.Log($"[NetworkBattleManager] Opponent mech data: M:{matrixObj} RA:{raObj} LA:{laObj} LB:{lbObj}");

        // Load catalogs directly from Resources with multiple possible names
        var matrixCatalog = Resources.Load<MatrixCatalog>("MatrixCatalog") ?? 
                           Resources.Load<MatrixCatalog>("Matrix Catalog 1") ??
                           Resources.Load<MatrixCatalog>("Matrix Catalog");
        
        var partCatalog = Resources.Load<PartCatalog>("PartCatalog") ?? 
                         Resources.Load<PartCatalog>("Part Catalog");

        if (matrixCatalog == null)
        {
            Debug.LogError("[NetworkBattleManager] MatrixCatalog not found in Resources!");
            Debug.LogError("⚠️ SOLUTION: Ensure MatrixCatalog.asset exists in Assets/Resources/ folder");
            Debug.LogError("⚠️ Tried paths: MatrixCatalog, Matrix Catalog 1, Matrix Catalog");
            
            // Try to find any MatrixCatalog in Resources
            var allCatalogs = Resources.LoadAll<MatrixCatalog>("");
            Debug.LogError($"Found {allCatalogs.Length} MatrixCatalog assets in Resources:");
            foreach (var catalog in allCatalogs)
            {
                Debug.LogError($"  - {catalog.name}");
            }
            return null;
        }

        if (partCatalog == null)
        {
            Debug.LogError("[NetworkBattleManager] PartCatalog not found in Resources!");
            Debug.LogError("⚠️ SOLUTION: Ensure PartCatalog.asset exists in Assets/Resources/ folder");
            Debug.LogError("⚠️ Tried paths: PartCatalog, Part Catalog");
            
            // Try to find any PartCatalog in Resources
            var allCatalogs = Resources.LoadAll<PartCatalog>("");
            Debug.LogError($"Found {allCatalogs.Length} PartCatalog assets in Resources:");
            foreach (var catalog in allCatalogs)
            {
                Debug.LogError($"  - {catalog.name}");
            }
            return null;
        }

        matrixCatalog.Init();
        partCatalog.Init();

        Debug.Log($"[NetworkBattleManager] Successfully loaded catalogs: MatrixCatalog={matrixCatalog.name}, PartCatalog={partCatalog.name}");

        var matrix = matrixCatalog.Get(matrixObj.ToString());
        var ra = partCatalog.Get(raObj.ToString());
        var la = partCatalog.Get(laObj.ToString());
        var lb = partCatalog.Get(lbObj.ToString());

        Debug.Log($"[NetworkBattleManager] Part lookup results: Matrix={matrix?.matrixName ?? "null"}, RA={ra?.partName ?? "null"}, LA={la?.partName ?? "null"}, LB={lb?.partName ?? "null"}");

        if (matrix == null)
        {
            Debug.LogError($"[NetworkBattleManager] Matrix '{matrixObj}' not found in catalog!");
            Debug.LogError("⚠️ SOLUTION: Ensure matrix exists in MatrixCatalog with correct matrixCode");
            Debug.LogError($"Available matrices in catalog:");
            foreach (var m in matrixCatalog.matrices)
            {
                if (m != null)
                    Debug.LogError($"  - {m.matrixCode}: {m.matrixName}");
            }
            return null;
        }
        if (ra == null)
        {
            Debug.LogError($"[NetworkBattleManager] RightArm '{raObj}' not found in catalog!");
            Debug.LogError("⚠️ SOLUTION: Ensure part exists in PartCatalog with correct partCode");
            Debug.LogError($"Available parts in catalog:");
            foreach (var p in partCatalog.parts)
            {
                if (p != null)
                    Debug.LogError($"  - {p.partCode}: {p.partName}");
            }
            return null;
        }
        if (la == null)
        {
            Debug.LogError($"[NetworkBattleManager] LeftArm '{laObj}' not found in catalog!");
            Debug.LogError("⚠️ SOLUTION: Ensure part exists in PartCatalog with correct partCode");
            Debug.LogError($"Available parts in catalog:");
            foreach (var p in partCatalog.parts)
            {
                if (p != null)
                    Debug.LogError($"  - {p.partCode}: {p.partName}");
            }
            return null;
        }
        if (lb == null)
        {
            Debug.LogError($"[NetworkBattleManager] LowerBody '{lbObj}' not found in catalog!");
            Debug.LogError("⚠️ SOLUTION: Ensure part exists in PartCatalog with correct partCode");
            Debug.LogError($"Available parts in catalog:");
            foreach (var p in partCatalog.parts)
            {
                if (p != null)
                    Debug.LogError($"  - {p.partCode}: {p.partName}");
            }
            return null;
        }

        Debug.Log($"[NetworkBattleManager] Successfully loaded all opponent parts!");

        var unit = new MechUnit
        {
            Name = opponent.NickName,
            chassis = matrix,
            matrixHP = matrix.baseStats?.HP ?? 100
        };

        unit.modules[ModuleSlot.RightArm] = ra.ToModuleData(ModuleSlot.RightArm);
        unit.modules[ModuleSlot.LeftArm] = la.ToModuleData(ModuleSlot.LeftArm);
        unit.modules[ModuleSlot.LowerBody] = lb.ToModuleData(ModuleSlot.LowerBody);

        unit.partStatuses[ModuleSlot.RightArm] = new PartStatus
        {
            partName = ra.partName,
            maxHP = ra.statModifiers?.HP ?? 50,
            currentHP = ra.statModifiers?.HP ?? 50,
            buffs = new Dictionary<string, int>()
        };
        unit.partStatuses[ModuleSlot.LeftArm] = new PartStatus
        {
            partName = la.partName,
            maxHP = la.statModifiers?.HP ?? 50,
            currentHP = la.statModifiers?.HP ?? 50,
            buffs = new Dictionary<string, int>()
        };
        unit.partStatuses[ModuleSlot.LowerBody] = new PartStatus
        {
            partName = lb.partName,
            maxHP = lb.statModifiers?.HP ?? 50,
            currentHP = lb.statModifiers?.HP ?? 50,
            buffs = new Dictionary<string, int>()
        };

        Debug.Log($"[NetworkBattleManager] ✅ Successfully created remote player unit for {opponent.NickName}!");
        return unit;
    }

    public void Initialize(MechUnit local, MechUnit remote, UIManager uiM)
    {
        localPlayerUnit = local;
        remotePlayerUnit = remote;
        uiManager = uiM;
        matrixBuffsDict[localPlayerUnit] = new Dictionary<string, int>();
        matrixBuffsDict[remotePlayerUnit] = new Dictionary<string, int>();
        InitializeMaxHPs();
        currentState = isMyTurn ? BattleState.MyTurn : BattleState.OpponentTurn;
    }

    private void InitializeMaxHPs()
    {
        if (localPlayerUnit != null)
        {
            localPlayerMaxHPs[ModuleSlot.Matrix] = localPlayerUnit.matrixHP;
            foreach (var slot in localPlayerUnit.partStatuses.Keys)
                localPlayerMaxHPs[slot] = localPlayerUnit.partStatuses[slot].currentHP;
        }
        if (remotePlayerUnit != null)
        {
            remotePlayerMaxHPs[ModuleSlot.Matrix] = remotePlayerUnit.matrixHP;
            foreach (var slot in remotePlayerUnit.partStatuses.Keys)
                remotePlayerMaxHPs[slot] = remotePlayerUnit.partStatuses[slot].currentHP;
        }
    }

    // ================== Turn Management ==================
    public void StartTurn()
    {
        if (battleOver) return;

        if (isMyTurn)
        {
            if (!HasAnyUsableModule(localPlayerUnit) || AllNonMatrixPartsBroken(localPlayerUnit))
            {
                uiManager.LogMessage($"{localPlayerUnit.Name} can no longer fight!");
                SendBattleEndEvent(false);
                return;
            }

            currentState = BattleState.SelectingAttack;
            if (turnTimer) turnTimer.BeginTurn(attackerIsPlayer: true);

            // Show your turn indicator
            if (networkUI != null)
            {
                networkUI.ShowYourTurnIndicator();
            }

            uiManager.RenderActionButtons(localPlayerUnit, SelectAttack);
        }
        else
        {
            currentState = BattleState.OpponentTurn;
            if (turnTimer) turnTimer.BeginTurn(attackerIsPlayer: false);

            // Show opponent's turn indicator
            if (networkUI != null)
            {
                networkUI.ShowOpponentTurnIndicator(remotePlayerUnit.Name);
            }
            else
            {
                uiManager.LogMessage($"Waiting for {remotePlayerUnit.Name}...");
                uiManager.DisableAllButtons();
            }
        }
    }

    public void SelectAttack(ModuleSlot sourceSlot, AttackData attack)
    {
        if (currentState != BattleState.SelectingAttack || !isMyTurn) return;

        if (localPlayerUnit.IsPartBroken(sourceSlot))
        {
            uiManager.LogMessage($"{sourceSlot} is broken!");
            return;
        }

        selectedAttack = attack;
        selectedSourceSlot = sourceSlot;
        bool isSelfTarget = attack.target == MechBattle.TargetType.Self;

        if (isSelfTarget)
        {
            currentState = BattleState.SelectingSelfTarget;
            uiManager.RenderSelfTargetButtons(localPlayerUnit, ConfirmSelfTarget);
            uiManager.ShowBackButton(OnBackFromTargetSelection);
        }
        else
        {
            currentState = BattleState.SelectingTarget;
            uiManager.RenderTargetButtons(remotePlayerUnit, ConfirmTarget);
            uiManager.ShowBackButton(OnBackFromTargetSelection);
        }
    }

    private void OnBackFromTargetSelection()
    {
        selectedAttack = null;
        selectedSourceSlot = ModuleSlot.Matrix;
        currentState = BattleState.SelectingAttack;
        uiManager.HideBackButton();
        uiManager.RenderActionButtons(localPlayerUnit, SelectAttack);
        uiManager.LogMessage("(Selection cancelled)");
    }

    public void ConfirmSelfTarget(ModuleSlot targetSlot)
    {
        if (currentState != BattleState.SelectingSelfTarget || !isMyTurn) return;
        uiManager.HideBackButton();

        // Send self-target action to network
        SendSelfTargetAction(selectedSourceSlot, targetSlot, selectedAttack);
    }

    public void ConfirmTarget(ModuleSlot targetSlot)
    {
        if (currentState != BattleState.SelectingTarget || !isMyTurn) return;
        uiManager.HideBackButton();

        // Send attack action to network
        SendAttackAction(selectedSourceSlot, targetSlot, selectedAttack);
    }

    // ================== Network Events ==================
    private void SendAttackAction(ModuleSlot sourceSlot, ModuleSlot targetSlot, AttackData attack)
    {
        if (turnTimer && turnTimer.IsRunning) turnTimer.EndTurn();

        object[] content = new object[] {
            (byte)sourceSlot,
            (byte)targetSlot,
            attack.attackName,
            attack.damage,
            attack.type,
            (byte)attack.target,
            attack.effect ?? ""
        };

        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(TARGET_CONFIRMED_EVENT, content, raiseEventOptions, SendOptions.SendReliable);
    }

    private void SendSelfTargetAction(ModuleSlot sourceSlot, ModuleSlot targetSlot, AttackData attack)
    {
        if (turnTimer && turnTimer.IsRunning) turnTimer.EndTurn();

        object[] content = new object[] {
            (byte)sourceSlot,
            (byte)targetSlot,
            attack.attackName,
            attack.damage,
            attack.type,
            (byte)attack.target,
            attack.effect ?? ""
        };

        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(ATTACK_SELECTED_EVENT, content, raiseEventOptions, SendOptions.SendReliable);
    }

    private void SendBattleEndEvent(bool iWon)
    {
        object[] content = new object[] { iWon };
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(BATTLE_END_EVENT, content, raiseEventOptions, SendOptions.SendReliable);

        EndBattle(iWon);
    }

    // ================== Event Callbacks ==================
    public void OnEvent(EventData photonEvent)
    {
        byte eventCode = photonEvent.Code;

        if (eventCode != ATTACK_SELECTED_EVENT && eventCode != TARGET_CONFIRMED_EVENT && eventCode != BATTLE_END_EVENT)
            return;

        object[] data = (object[])photonEvent.CustomData;

        switch (eventCode)
        {
            case ATTACK_SELECTED_EVENT:
                OnNetworkSelfTargetAction(data, photonEvent.Sender);
                break;
            case TARGET_CONFIRMED_EVENT:
                OnNetworkAttackAction(data, photonEvent.Sender);
                break;
            case BATTLE_END_EVENT:
                OnNetworkBattleEnd(data, photonEvent.Sender);
                break;
        }
    }

    private void OnNetworkSelfTargetAction(object[] data, int sender)
    {
        ModuleSlot sourceSlot = (ModuleSlot)(byte)data[0];
        ModuleSlot targetSlot = (ModuleSlot)(byte)data[1];
        string attackName = (string)data[2];
        int damage = (int)data[3];
        string type = (string)data[4];
        MechBattle.TargetType target = (MechBattle.TargetType)(byte)data[5]; // Fixed: explicit namespace
        string effect = (string)data[6];

        AttackData attack = new AttackData
        {
            attackName = attackName,
            damage = damage,
            type = type,
            target = target,
            effect = effect
        };

        bool wasMyAction = (sender == PhotonNetwork.LocalPlayer.ActorNumber);
        StartCoroutine(ProcessSelfTargetAction(sourceSlot, targetSlot, attack, wasMyAction));
    }

    private void OnNetworkAttackAction(object[] data, int sender)
    {
        ModuleSlot sourceSlot = (ModuleSlot)(byte)data[0];
        ModuleSlot targetSlot = (ModuleSlot)(byte)data[1];
        string attackName = (string)data[2];
        int damage = (int)data[3];
        string type = (string)data[4];
        MechBattle.TargetType target = (MechBattle.TargetType)(byte)data[5]; // Fixed: explicit namespace
        string effect = (string)data[6];

        AttackData attack = new AttackData
        {
            attackName = attackName,
            damage = damage,
            type = type,
            target = target,
            effect = effect
        };

        bool wasMyAction = (sender == PhotonNetwork.LocalPlayer.ActorNumber);
        StartCoroutine(ProcessAttackAction(sourceSlot, targetSlot, attack, wasMyAction));
    }

    private void OnNetworkBattleEnd(object[] data, int sender)
    {
        bool opponentWon = (bool)data[0];
        bool senderWon = opponentWon;

        // If the sender won, and I'm not the sender, then I lost
        bool iWon = (sender == PhotonNetwork.LocalPlayer.ActorNumber) ? senderWon : !senderWon;

        EndBattle(iWon);
    }

    // ================== Action Processing ==================
    private IEnumerator ProcessSelfTargetAction(ModuleSlot sourceSlot, ModuleSlot targetSlot, AttackData attack, bool isMyAction)
    {
        currentState = BattleState.ProcessingAction;

        MechUnit actor = isMyAction ? localPlayerUnit : remotePlayerUnit;

        if (actor.IsPartBroken(sourceSlot))
        {
            uiManager.LogMessage($"{actor.Name}'s {sourceSlot} was destroyed!");
            SwitchTurns();
            yield break;
        }

        uiManager.LogMessage($"{actor.Name} used {attack.attackName} on {targetSlot}.");
        uiManager.PlayAttackFx(isMyAction, isMyAction, targetSlot, attack.attackName);

        yield return new WaitForSeconds(attackFxDuration);

        ApplyEffect(actor, actor, targetSlot, attack);
        uiManager.UpdateHealthBars(actor, targetSlot, GetCurrentHP(actor, targetSlot),
            localPlayerMaxHPs, remotePlayerMaxHPs, localPlayerUnit, remotePlayerUnit);

        SwitchTurns();
    }

    private IEnumerator ProcessAttackAction(ModuleSlot sourceSlot, ModuleSlot targetSlot, AttackData attack, bool isMyAction)
    {
        currentState = BattleState.ProcessingAction;

        MechUnit attacker = isMyAction ? localPlayerUnit : remotePlayerUnit;
        MechUnit defender = isMyAction ? remotePlayerUnit : localPlayerUnit;

        if (attacker.IsPartBroken(sourceSlot))
        {
            uiManager.LogMessage($"{attacker.Name}'s {sourceSlot} was destroyed!");
            SwitchTurns();
            yield break;
        }

        if (targetSlot == ModuleSlot.Matrix && !defender.CanAttackMatrix())
        {
            uiManager.LogMessage("Matrix locked!");
            SwitchTurns();
            yield break;
        }

        uiManager.PlayAttackFx(isMyAction, !isMyAction, targetSlot, attack.attackName);
        yield return new WaitForSeconds(attackFxDuration);

        int damage = CalculateDamage(attack, attacker, defender, targetSlot, sourceSlot);
        int maxHP = isMyAction ? remotePlayerMaxHPs[targetSlot] : localPlayerMaxHPs[targetSlot];
        float damagePercent = (damage / (float)maxHP) * 100f;
        int prevHP = GetCurrentHP(defender, targetSlot);

        ApplyDamage(defender, targetSlot, damage);
        int newHP = GetCurrentHP(defender, targetSlot);

        uiManager.LogMessage($"{attacker.Name} used {attack.attackName} on {defender.Name}'s {targetSlot}.");
        if (damage > 0) uiManager.LogMessage($"Dealt {damage} damage ({damagePercent:F1}%).");

        ApplyEffect(attacker, defender, targetSlot, attack);
        uiManager.UpdateHealthBars(defender, targetSlot, newHP, localPlayerMaxHPs, remotePlayerMaxHPs,
            localPlayerUnit, remotePlayerUnit);

        if (newHP == 0 && prevHP > 0)
        {
            uiManager.LogMessage($"{defender.Name}'s {targetSlot} destroyed!");
            ResetBuffStages(defender, targetSlot);
            yield return new WaitForSeconds(0.05f);
            HandlePartDestroyed(defender, defender == localPlayerUnit, targetSlot);
            yield return new WaitForSeconds(fxDestroyDelay);
        }

        // Check win conditions
        if (defender.matrixHP <= 0)
        {
            uiManager.LogMessage($"{defender.Name}'s Matrix destroyed!\n{attacker.Name} wins!");
            SendBattleEndEvent(attacker == localPlayerUnit);
            yield break;
        }

        if (AllNonMatrixPartsBroken(defender) || !HasAnyUsableModule(defender))
        {
            uiManager.LogMessage($"{defender.Name} can no longer fight!\n{attacker.Name} wins!");
            SendBattleEndEvent(attacker == localPlayerUnit);
            yield break;
        }

        SwitchTurns();
    }

    private void SwitchTurns()
    {
        isMyTurn = !isMyTurn;
        uiManager.LogMessage("\n");
        StartTurn();
    }

    // ================== Timeout Handlers ==================
    public void OnLocalPlayerTimeout()
    {
        if (battleOver) return;
        uiManager.LogMessage("Time's up! You lose!");
        SendBattleEndEvent(false);
    }

    public void OnRemotePlayerTimeout()
    {
        if (battleOver) return;
        uiManager.LogMessage($"{remotePlayerUnit.Name} ran out of time! You win!");
        SendBattleEndEvent(true);
    }

    // ================== Combat Calculations ==================

    private void ApplyEffect(MechUnit attacker, MechUnit effectUnit, ModuleSlot effectSlot, AttackData attack)
    {
        if (attack == null || string.IsNullOrEmpty(attack.attackName))
        {
            return;
        }

        MechPart part = FindMechPartByAttack(attacker, attack);
        MechBattle.MoveDefinition selectedMove = null;

        if (part != null && part.moves != null && part.moves.Count > 0)
        {
            selectedMove = part.moves.Find(m => m.moveName == attack.attackName);
            if (selectedMove == null && part.moves.Count == 1)
                selectedMove = part.moves[0];
        }

        string effect = selectedMove != null && !string.IsNullOrEmpty(selectedMove.effect)
            ? selectedMove.effect
            : attack.effect;

        if (string.IsNullOrEmpty(effect)) return;

        List<string> effects = new List<string>(effect.Split(';'));
        foreach (string eff in effects)
            ApplySingleEffect(effectUnit, effectSlot, eff.Trim());
    }

    private void ApplySingleEffect(MechUnit unit, ModuleSlot slot, string effect)
    {
        if (string.IsNullOrWhiteSpace(effect)) return;
        effect = effect.Trim();

        if (effect.StartsWith("+") || effect.StartsWith("-"))
        {
            bool isBuff = effect.StartsWith("+");
            string[] parts = effect.Substring(1).Trim().Split(' ');
            int amount = 1;
            string stat;

            if (parts.Length == 1)
                stat = parts[0].ToUpper();
            else if (parts.Length > 1 && int.TryParse(parts[0], out int parsed))
            {
                amount = parsed;
                stat = parts[1].ToUpper();
            }
            else return;

            int delta = isBuff ? amount : -amount;
            var (prevStage, newStage) = ApplyBuffStageDetailed(unit, slot, stat, delta);

            uiManager.PlayBuffDebuffFx(unit == localPlayerUnit, slot, isBuff);

            if (newStage == 0)
            {
                if (prevStage != 0)
                    uiManager.LogMessage($"{unit.Name}'s {slot} {stat} returned to normal.");
            }
            else
            {
                bool flipped = (Mathf.Sign(prevStage) != Mathf.Sign(newStage)) && prevStage != 0;
                if (flipped)
                    uiManager.LogMessage($"{unit.Name}'s {slot} {stat} neutralized and moved to {newStage:+#;-#}.");
                else
                {
                    string dir = newStage > 0 ? "increased" : "decreased";
                    uiManager.LogMessage($"{unit.Name}'s {slot} {stat} {dir} to stage {newStage:+#;-#}.");
                }
            }
            return;
        }

        if (effect.ToLower().Contains("piercing"))
        {
            uiManager.LogMessage("Effect: Piercing – halves defense this turn.");
            return;
        }
    }

    private int CalculateDamage(AttackData attack, MechUnit attacker, MechUnit defender, ModuleSlot targetSlot, ModuleSlot sourceSlot)
    {
        if (attack == null || attacker == null || defender == null ||
            attacker.chassis == null || defender.chassis == null)
            return 0;

        string atkStat = attack.type == "Physical" ? "ATK" : "ENG";
        string defStat = attack.type == "Physical" ? "DEF" : "SYS";

        float baseAtk = attack.type == "Physical"
            ? (attacker.chassis.baseStats?.ATK ?? 1)
            : (attacker.chassis.baseStats?.ENG ?? 1);
        float baseDef = attack.type == "Physical"
            ? (defender.chassis.baseStats?.DEF ?? 1)
            : (defender.chassis.baseStats?.SYS ?? 1);

        int atkStage = GetBuffStage(attacker, sourceSlot, atkStat);
        int defStage = GetBuffStage(defender, targetSlot, defStat);

        float atkMult = GetStageMultiplier(atkStage);
        float defMult = GetStageMultiplier(defStage);

        float effectiveAtk = baseAtk * atkMult;
        float effectiveDef = Mathf.Max(1f, baseDef * defMult);

        MechPart part = FindMechPartByAttack(attacker, attack);
        if (part != null && part.moves != null && part.moves.Count > 0)
        {
            MechBattle.MoveDefinition selectedMove = part.moves.Find(m => m.moveName == attack.attackName);
            if (selectedMove == null) selectedMove = part.moves[0];
            if (selectedMove != null && selectedMove.effect.ToLower().Contains("piercing"))
                effectiveDef *= 0.5f;
        }

        float multiplier = effectiveAtk / effectiveDef;
        float baseDamage = attack.damage;

        return Mathf.FloorToInt(baseDamage * Mathf.Max(0.1f, multiplier));
    }

    private MechPart FindMechPartByAttack(MechUnit unit, AttackData attack)
    {
        if (attack == null) return null;
        foreach (var kvp in unit.modules)
        {
            if (kvp.Value.attack != null && kvp.Value.attack.attackName == attack.attackName)
                return Resources.Load<MechPart>($"Parts/{kvp.Value.slot.ToString()}/{kvp.Value.partCode}");
        }
        return null;
    }

    private float GetStageMultiplier(int stage)
    {
        stage = Mathf.Clamp(stage, -6, 6);
        float[] multipliers = { 0.25f, 0.2857f, 0.3333f, 0.4f, 0.5f, 0.6667f, 1f,
                               1.5f, 2f, 2.5f, 3f, 3.5f, 4f };
        return multipliers[stage + 6];
    }

    private int GetBuffStage(MechUnit unit, ModuleSlot slot, string stat)
    {
        if (slot == ModuleSlot.Matrix)
        {
            if (matrixBuffsDict.ContainsKey(unit))
                return matrixBuffsDict[unit].GetValueOrDefault(stat, 0);
        }
        else if (unit.partStatuses.ContainsKey(slot))
        {
            return unit.partStatuses[slot].buffs.GetValueOrDefault(stat, 0);
        }
        return 0;
    }

    private (int prev, int now) ApplyBuffStageDetailed(MechUnit unit, ModuleSlot slot, string stat, int delta)
    {
        int prev = GetBuffStage(unit, slot, stat);
        int now = Mathf.Clamp(prev + delta, -6, 6);

        if (slot == ModuleSlot.Matrix)
        {
            if (!matrixBuffsDict.ContainsKey(unit))
                matrixBuffsDict[unit] = new Dictionary<string, int>();

            if (now == 0)
                matrixBuffsDict[unit].Remove(stat);
            else
                matrixBuffsDict[unit][stat] = now;

            uiManager.SetBuffChips(unit == localPlayerUnit, ModuleSlot.Matrix,
                matrixBuffsDict.ContainsKey(unit) ? matrixBuffsDict[unit] : null);
        }
        else if (unit.partStatuses.ContainsKey(slot))
        {
            var dict = unit.partStatuses[slot].buffs;
            if (now == 0)
                dict.Remove(stat);
            else
                dict[stat] = now;

            uiManager.SetBuffChips(unit == localPlayerUnit, slot, dict);
        }

        return (prev, now);
    }

    private void ResetBuffStages(MechUnit unit, ModuleSlot slot)
    {
        if (slot == ModuleSlot.Matrix)
        {
            if (matrixBuffsDict.ContainsKey(unit))
                matrixBuffsDict[unit].Clear();

            uiManager.SetBuffChips(unit == localPlayerUnit, ModuleSlot.Matrix,
                matrixBuffsDict.ContainsKey(unit) ? matrixBuffsDict[unit] : null);
        }
        else if (unit.partStatuses.ContainsKey(slot))
        {
            unit.partStatuses[slot].buffs.Clear();
            uiManager.SetBuffChips(unit == localPlayerUnit, slot, unit.partStatuses[slot].buffs);
        }
    }

    private bool AllNonMatrixPartsBroken(MechUnit unit)
    {
        if (unit == null) return false;
        return unit.IsPartBroken(ModuleSlot.RightArm)
            && unit.IsPartBroken(ModuleSlot.LeftArm)
            && unit.IsPartBroken(ModuleSlot.LowerBody);
    }

    private bool HasAnyUsableModule(MechUnit unit)
    {
        if (unit == null || unit.modules == null) return false;
        foreach (var kvp in unit.modules)
        {
            if (kvp.Key != ModuleSlot.Matrix && !unit.IsPartBroken(kvp.Key))
                return true;
        }
        return false;
    }

    private void HandlePartDestroyed(MechUnit unit, bool isLocalPlayer, ModuleSlot slot)
    {
        if (uiManager != null)
        {
            uiManager.PlayFxOnSlot("FX_Explosion", isLocalPlayer, slot);
            uiManager.SetPartDestroyedVisual(isLocalPlayer, slot, true);
        }
    }

    // ================== Helper Methods ==================

    private int GetCurrentHP(MechUnit unit, ModuleSlot slot)
    {
        if (unit == null) return 0;
        if (slot == ModuleSlot.Matrix) return unit.matrixHP;
        return unit.partStatuses.ContainsKey(slot) ? unit.partStatuses[slot].currentHP : 0;
    }

    private void ApplyDamage(MechUnit unit, ModuleSlot slot, int damage)
    {
        if (unit == null) return;
        if (slot == ModuleSlot.Matrix)
            unit.matrixHP = Mathf.Max(0, unit.matrixHP - damage);
        else if (unit.partStatuses.ContainsKey(slot))
            unit.partStatuses[slot].currentHP = Mathf.Max(0, unit.partStatuses[slot].currentHP - damage);
    }

    private void EndBattle(bool iWon)
    {
        if (battleOver) return;
        battleOver = true;

        if (turnTimer)
        {
            turnTimer.SetPaused(true);
            turnTimer.enabled = false;
            turnTimer.onPlayerFlagFall.RemoveListener(OnLocalPlayerTimeout);
            turnTimer.onEnemyFlagFall.RemoveListener(OnRemotePlayerTimeout);
        }

        uiManager.HideBackButton();
        uiManager.DisableAllButtons();
        uiManager.ShowBattleResult(iWon, 0);

        Debug.Log($"[NetworkBattleManager] Battle ended. Result: {(iWon ? "Victory" : "Defeat")}");
    }

    private Player GetOpponentPlayer()
    {
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p != PhotonNetwork.LocalPlayer)
                return p;
        }
        return null;
    }

    // ================== Photon Callbacks ==================

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!battleOver)
        {
            Debug.Log($"[NetworkBattleManager] {otherPlayer.NickName} disconnected!");

            if (networkUI != null)
            {
                networkUI.ShowDisconnectMessage(otherPlayer.NickName);
            }
            else
            {
                uiManager.LogMessage($"{otherPlayer.NickName} disconnected!");
            }

            EndBattle(true); // Local player wins by forfeit
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (!battleOver)
        {
            Debug.LogWarning($"[NetworkBattleManager] Disconnected: {cause}");
            uiManager.LogMessage($"Disconnected: {cause}");
            EndBattle(false); // Local player loses on disconnect
        }
    }

    // ================== Required for IOnEventCallback ==================

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
        Debug.Log("[NetworkBattleManager] Photon callbacks registered");
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.RemoveCallbackTarget(this);
        Debug.Log("[NetworkBattleManager] Photon callbacks unregistered");
    }
}