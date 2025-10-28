using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using MechBattle;
using ExitGames.Client.Photon;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Complete Photon Connection Manager with full UI panel system.
/// Handles connection, room creation/joining, and mech data sharing.
/// </summary>
public class PhotonConnectionManager : MonoBehaviourPunCallbacks
{
    [Header("Connection Settings")]
    [Tooltip("Photon App Version - increment when making breaking changes")]
    public string gameVersion = "1.0";

    [Tooltip("Max players per room")]
    public byte maxPlayersPerRoom = 2;

    [Tooltip("Photon Region - use 'us', 'eu', 'asia', etc. Leave empty for best region")]
    public string preferredRegion = "in";

    [Header("Scene Management")]
    [Tooltip("Scene to load for PVP battles")]
    public string pvpBattleSceneName = "PvP Game Play";

    [Tooltip("Scene to load for PVE battles")]
    public string pveBattleSceneName = "3_PilotMechBattleV1";

    [Header("UI Panels")]
    public GameObject connectingPanel;
    public GameObject menuPanel;
    public GameObject playPanel;
    public GameObject vsFriendPanel;
    public GameObject createRoomPanel;
    public GameObject joinRoomPanel;
    public GameObject startGamePanel;

    [Header("UI Elements")]
    public Text statusText;
    public InputField usernameInput;
    public Button playButton;
    public Button vsFriendButton;
    public Button createRoomButton;
    public Button joinRoomButton;
    public InputField createRoomNameInput;
    public InputField joinRoomNameInput;
    public Button createRoomConfirmButton;
    public Button joinRoomConfirmButton;
    public Text playerListText;
    public Button startGameButton;
    public GameObject loadingPanel;

    [Header("Mech Data")]
    [Tooltip("Reference to your build service or catalogs")]
    public MatrixCatalog matrixCatalog;
    public PartCatalog partCatalog;

    private bool isConnecting = false;
    private MechBuild currentBuild;
    private string pendingRoomJoin = "";

    private void Start()
    {
        // Load current build
        currentBuild = BuildService.LoadOrNull();

        if (currentBuild == null)
        {
            Debug.LogWarning("[PhotonConnectionManager] No build found! Using default.");
            currentBuild = PartCodeUtil.BuildForFamily("01"); // Default to Titan
            BuildService.Save(currentBuild);
        }

        // Setup UI listeners
        if (playButton) playButton.onClick.AddListener(OnPlayClicked);
        if (vsFriendButton) vsFriendButton.onClick.AddListener(OnVsFriendClicked);
        if (createRoomButton) createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        if (joinRoomButton) joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
        if (createRoomConfirmButton) createRoomConfirmButton.onClick.AddListener(OnCreateRoomConfirmClicked);
        if (joinRoomConfirmButton) joinRoomConfirmButton.onClick.AddListener(OnJoinRoomConfirmClicked);
        if (startGameButton) startGameButton.onClick.AddListener(OnStartGameClicked);

        // Load saved username
        string savedName = PlayerPrefs.GetString("PlayerName", "");
        if (!string.IsNullOrEmpty(savedName))
        {
            PhotonNetwork.NickName = savedName;
            if (usernameInput) usernameInput.text = savedName;
        }
        else
        {
            PhotonNetwork.NickName = "Pilot_" + Random.Range(1000, 9999);
            if (usernameInput) usernameInput.text = PhotonNetwork.NickName;
        }

        // Show connecting panel initially
        ShowOnly(connectingPanel);

        // Auto-connect to Photon
        ConnectToPhoton();
    }

    private void ConnectToPhoton()
    {
        if (PhotonNetwork.IsConnected)
        {
            UpdateStatus("Connected to Photon!");
            return;
        }

        isConnecting = true;
        UpdateStatus("Connecting to Photon...");
        ShowLoading(true);

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = gameVersion;
        
        // Set region if specified
        if (!string.IsNullOrEmpty(preferredRegion))
        {
            Debug.Log($"[PhotonConnectionManager] Connecting to region: {preferredRegion}");
            // Set the region in Photon App Settings
            PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = preferredRegion;
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            Debug.Log("[PhotonConnectionManager] Connecting to best region");
            // Clear any fixed region setting
            PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "";
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    // ================== Photon Callbacks ==================
    public override void OnConnectedToMaster()
    {
        Debug.Log("[PhotonConnectionManager] Connected to Master Server");
        Debug.Log($"[PhotonConnectionManager] Connected to region: {PhotonNetwork.CloudRegion}");
        isConnecting = false;
        UpdateStatus($"Connected to {PhotonNetwork.CloudRegion}! Ready to play.");
        ShowLoading(false);

        // Join lobby to see available rooms
        PhotonNetwork.JoinLobby();

        // Show menu panel when connected
        ShowOnly(menuPanel);
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[PhotonConnectionManager] Joined Lobby");
        UpdateStatus("In Lobby - Ready to match!");
        
        // If we have a pending room join, try to join it now
        if (!string.IsNullOrEmpty(pendingRoomJoin))
        {
            Debug.Log($"[PhotonConnectionManager] Joining pending room: {pendingRoomJoin}");
            PhotonNetwork.JoinRoom(pendingRoomJoin);
            pendingRoomJoin = "";
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"[PhotonConnectionManager] Room list updated. Found {roomList.Count} rooms:");
        foreach (var room in roomList)
        {
            Debug.Log($"  - Room: '{room.Name}' (Players: {room.PlayerCount}/{room.MaxPlayers}, Open: {room.IsOpen})");
        }
        
        // Check if pending room join exists in the list
        if (!string.IsNullOrEmpty(pendingRoomJoin))
        {
            bool roomExists = roomList.Any(room => room.Name == pendingRoomJoin);
            Debug.Log($"[PhotonConnectionManager] Pending room '{pendingRoomJoin}' exists in list: {roomExists}");
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[PhotonConnectionManager] Disconnected: {cause}");
        isConnecting = false;
        UpdateStatus($"Disconnected: {cause}");
        ShowLoading(false);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[PhotonConnectionManager] Joined Room: {PhotonNetwork.CurrentRoom.Name}");
        UpdateStatus($"Joined room: {PhotonNetwork.CurrentRoom.Name}");

        // Store mech data in player custom properties
        ShareMechData();

        // Show start game panel
        ShowOnly(startGamePanel);
        UpdatePlayerList();

        // Only master client can start
        if (startGameButton)
        {
            startGameButton.interactable = PhotonNetwork.IsMasterClient;
        }
    }

    /// <summary>
    /// Public method to manually refresh mech data sharing
    /// </summary>
    public void RefreshMechData()
    {
        Debug.Log("[PhotonConnectionManager] Manually refreshing mech data...");
        
        // Reload current build
        currentBuild = BuildService.LoadOrNull();
        if (currentBuild == null)
        {
            Debug.LogWarning("[PhotonConnectionManager] No saved build found, creating default.");
            currentBuild = PartCodeUtil.BuildForFamily("01");
            BuildService.Save(currentBuild);
        }
        
        // Share the data
        ShareMechData();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[PhotonConnectionManager] Player joined: {newPlayer.NickName}");
        UpdateStatus($"{newPlayer.NickName} joined!");
        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[PhotonConnectionManager] Player left: {otherPlayer.NickName}");
        UpdateStatus($"{otherPlayer.NickName} left!");
        UpdatePlayerList();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"[PhotonConnectionManager] New master: {newMasterClient.NickName}");
        if (startGameButton)
        {
            startGameButton.interactable = PhotonNetwork.IsMasterClient;
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[PhotonConnectionManager] Left room");
        UpdateStatus("Left room");
        ShowOnly(menuPanel);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[PhotonConnectionManager] Join Room Failed - Code: {returnCode}, Message: {message}");
        
        string errorMessage = GetJoinRoomErrorMessage(returnCode);
        UpdateStatus($"Failed to join room: {errorMessage}");
        ShowLoading(false);
    }
    
    private string GetJoinRoomErrorMessage(short returnCode)
    {
        switch (returnCode)
        {
            case 32765: // RoomNotFound
                return "Room not found. Please check the room name and try again.";
            case 32764: // RoomFull
                return "Room is full. Maximum 2 players allowed.";
            case 32762: // GameClosed
                return "Room is closed. Please try another room.";
            case 32761: // GameDoesNotExist
                return "Room does not exist. Please check the room name.";
            case 32760: // MaxCcuReached
                return "Server is full. Please try again later.";
            case 32758: // InvalidRegion
                return "Different region error! Both players must connect to the same region. Please check region settings.";
            case 32757: // CustomAuthenticationFailed
                return "Authentication failed. Please restart the game.";
            case 32756: // RegionException
                return "Region error! Both players must be in the same region.";
            default:
                return $"Error {returnCode}: Please check the room name and try again.";
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[PhotonConnectionManager] Create Room Failed: {message}");
        UpdateStatus($"Failed to create room: {message}");
        ShowLoading(false);
    }

    // ================== UI Panel Management ==================

    void ShowOnly(GameObject panel)
    {
        connectingPanel.SetActive(false);
        menuPanel.SetActive(false);
        playPanel.SetActive(false);
        vsFriendPanel.SetActive(false);
        createRoomPanel.SetActive(false);
        joinRoomPanel.SetActive(false);
        startGamePanel.SetActive(false);

        panel.SetActive(true);
    }

    // ================== UI Button Handlers ==================

    public void OnPlayClicked()
    {
        PhotonNetwork.NickName = usernameInput.text;
        ShowOnly(playPanel);
    }

    public void OnVsFriendClicked()
    {
        ShowOnly(vsFriendPanel);
    }

    public void OnCreateRoomClicked()
    {
        ShowOnly(createRoomPanel);
    }

    public void OnJoinRoomClicked()
    {
        ShowOnly(joinRoomPanel);
    }

    public void OnCreateRoomConfirmClicked()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            UpdateStatus("Not connected to Photon! Please wait...");
            return;
        }
        
        if (createRoomNameInput.text.Length > 0)
        {
            string roomName = createRoomNameInput.text.Trim();
            Debug.Log($"[PhotonConnectionManager] === CREATE ROOM DEBUG ===");
            Debug.Log($"[PhotonConnectionManager] Room name: '{roomName}'");
            Debug.Log($"[PhotonConnectionManager] Connected: {PhotonNetwork.IsConnected}");
            Debug.Log($"[PhotonConnectionManager] In lobby: {PhotonNetwork.InLobby}");
            
            UpdateStatus($"Creating room: {roomName}...");
            ShowLoading(true);
            
            RoomOptions options = new RoomOptions 
            { 
                MaxPlayers = 2,
                IsVisible = true,
                IsOpen = true
            };
            
            bool createResult = PhotonNetwork.CreateRoom(roomName, options);
            Debug.Log($"[PhotonConnectionManager] CreateRoom returned: {createResult}");
        }
        else
        {
            UpdateStatus("Please enter a room name!");
        }
    }

    public void OnJoinRoomConfirmClicked()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            UpdateStatus("Not connected to Photon! Please wait...");
            return;
        }
        
        if (joinRoomNameInput.text.Length > 0)
        {
            string roomName = joinRoomNameInput.text.Trim();
            Debug.Log($"[PhotonConnectionManager] === JOIN ROOM DEBUG ===");
            Debug.Log($"[PhotonConnectionManager] Room name: '{roomName}'");
            Debug.Log($"[PhotonConnectionManager] Connected: {PhotonNetwork.IsConnected}");
            Debug.Log($"[PhotonConnectionManager] In lobby: {PhotonNetwork.InLobby}");
            Debug.Log($"[PhotonConnectionManager] Current room: {(PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.Name : "None")}");
            Debug.Log($"[PhotonConnectionManager] Region: {PhotonNetwork.CloudRegion}");
            Debug.Log($"[PhotonConnectionManager] Master client: {PhotonNetwork.IsMasterClient}");
            
            UpdateStatus($"Joining room: {roomName}...");
            ShowLoading(true);
            
            // Make sure we're in lobby before joining room
            if (!PhotonNetwork.InLobby)
            {
                Debug.Log("[PhotonConnectionManager] Not in lobby, joining lobby first...");
                PhotonNetwork.JoinLobby();
                // Store the room name to join after lobby join
                pendingRoomJoin = roomName;
                return;
            }
            
            // Try to join the room
            bool joinResult = PhotonNetwork.JoinRoom(roomName);
            Debug.Log($"[PhotonConnectionManager] JoinRoom returned: {joinResult}");
            
            // If join failed immediately, try alternative approach
            if (!joinResult)
            {
                Debug.Log("[PhotonConnectionManager] JoinRoom returned false, trying alternative approach...");
                StartCoroutine(RetryJoinRoom(roomName));
            }
        }
        else
        {
            UpdateStatus("Please enter a room name!");
        }
    }

    public void OnStartGameClicked()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartBattle();
        }
    }

    // ================== Player List Management ==================

    void UpdatePlayerList()
    {
        playerListText.text = "";
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            playerListText.text += p.NickName + "\n";
        }
    }

    // ================== Debug Methods ==================

    /// <summary>
    /// Retry joining room with a delay
    /// </summary>
    private System.Collections.IEnumerator RetryJoinRoom(string roomName)
    {
        yield return new WaitForSeconds(1f);
        
        Debug.Log($"[PhotonConnectionManager] Retrying to join room: {roomName}");
        
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InLobby)
        {
            bool retryResult = PhotonNetwork.JoinRoom(roomName);
            Debug.Log($"[PhotonConnectionManager] Retry JoinRoom returned: {retryResult}");
            
            if (!retryResult)
            {
                UpdateStatus($"Failed to join room '{roomName}'. Room may not exist or be full.");
                ShowLoading(false);
            }
        }
        else
        {
            UpdateStatus("Connection lost during retry. Please try again.");
            ShowLoading(false);
        }
    }

    /// <summary>
    /// Debug method to test room joining with a specific name
    /// </summary>
    public void TestJoinRoom(string roomName)
    {
        Debug.Log($"[PhotonConnectionManager] === TEST JOIN ROOM ===");
        Debug.Log($"[PhotonConnectionManager] Testing join room: '{roomName}'");
        Debug.Log($"[PhotonConnectionManager] Connected: {PhotonNetwork.IsConnected}");
        Debug.Log($"[PhotonConnectionManager] In lobby: {PhotonNetwork.InLobby}");
        Debug.Log($"[PhotonConnectionManager] In room: {PhotonNetwork.InRoom}");
        
        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.JoinRoom(roomName);
        }
        else
        {
            Debug.LogError("[PhotonConnectionManager] Not connected to Photon!");
        }
    }

    /// <summary>
    /// Get current region information for debugging
    /// </summary>
    public string GetCurrentRegionInfo()
    {
        if (PhotonNetwork.IsConnected)
        {
            return $"Connected to region: {PhotonNetwork.CloudRegion}";
        }
        else
        {
            return "Not connected to Photon";
        }
    }

    /// <summary>
    /// Reconnect to a specific region
    /// </summary>
    public void ReconnectToRegion(string region)
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
        
        preferredRegion = region;
        ConnectToPhoton();
    }

    /// <summary>
    /// Test method to create a room with a simple name
    /// </summary>
    public void CreateTestRoom()
    {
        string testRoomName = "TestRoom_" + Random.Range(100, 999);
        Debug.Log($"[PhotonConnectionManager] Creating test room: {testRoomName}");
        
        RoomOptions options = new RoomOptions 
        { 
            MaxPlayers = 2,
            IsVisible = true,
            IsOpen = true
        };
        
        PhotonNetwork.CreateRoom(testRoomName, options);
    }

    /// <summary>
    /// Test method to join a room with a simple name
    /// </summary>
    public void JoinTestRoom(string roomName)
    {
        Debug.Log($"[PhotonConnectionManager] Joining test room: {roomName}");
        PhotonNetwork.JoinRoom(roomName);
    }

    // ================== Public Methods (UI Buttons) ==================

    /// <summary>
    /// Quick match - joins random room or creates one
    /// </summary>
    public void QuickMatch()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            UpdateStatus("Not connected to Photon!");
            return;
        }

        UpdateStatus("Searching for match...");
        ShowLoading(true);

        // Try to join random room, if fails, create one
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("[PhotonConnectionManager] No random room available, creating new room...");

        // Create a new room with random name
        string roomName = "Room_" + Random.Range(1000, 9999);
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    /// <summary>
    /// Start PVE battle (local, no Photon)
    /// </summary>
    public void StartPVEBattle()
    {
        if (!string.IsNullOrEmpty(pveBattleSceneName))
        {
            SceneManager.LoadScene(pveBattleSceneName);
        }
        else
        {
            Debug.LogWarning("[PhotonConnectionManager] PVE scene name not set!");
        }
    }
    // ================== Mech Data Sharing ==================

    /// <summary>
    /// Share current mech build with other players via CustomProperties
    /// </summary>
    private void ShareMechData()
    {
        if (currentBuild == null)
        {
            Debug.LogError("[PhotonConnectionManager] No build to share!");
            
            // Try to load build again
            currentBuild = BuildService.LoadOrNull();
            if (currentBuild == null)
            {
                Debug.LogError("[PhotonConnectionManager] Still no build found! Creating default build.");
                currentBuild = PartCodeUtil.BuildForFamily("01"); // Default to Titan
                BuildService.Save(currentBuild);
            }
        }

        // Validate build data
        if (string.IsNullOrEmpty(currentBuild.matrixId) || 
            string.IsNullOrEmpty(currentBuild.rightArmId) || 
            string.IsNullOrEmpty(currentBuild.leftArmId) || 
            string.IsNullOrEmpty(currentBuild.lowerBodyId))
        {
            Debug.LogError("[PhotonConnectionManager] Invalid build data detected! Creating default build.");
            currentBuild = PartCodeUtil.BuildForFamily("01");
            BuildService.Save(currentBuild);
        }

        // Create hashtable with mech data
        Hashtable properties = new Hashtable
        {
            { "matrixId", currentBuild.matrixId },
            { "rightArmId", currentBuild.rightArmId },
            { "leftArmId", currentBuild.leftArmId },
            { "lowerBodyId", currentBuild.lowerBodyId }
        };

        // Set player properties
        PhotonNetwork.LocalPlayer.SetCustomProperties(properties);

        Debug.Log($"[PhotonConnectionManager] ✅ Successfully shared mech data: M={currentBuild.matrixId}, RA={currentBuild.rightArmId}, LA={currentBuild.leftArmId}, LB={currentBuild.lowerBodyId}");
        
        // Verify the properties were set
        var verifyProps = PhotonNetwork.LocalPlayer.CustomProperties;
        Debug.Log($"[PhotonConnectionManager] Verification - Properties set: {verifyProps.ContainsKey("matrixId")}, {verifyProps.ContainsKey("rightArmId")}, {verifyProps.ContainsKey("leftArmId")}, {verifyProps.ContainsKey("lowerBodyId")}");
    }

    /// <summary>
    /// Load battle scene (MasterClient loads for all)
    /// </summary>
    private void StartBattle()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            UpdateStatus("Opponent is loading battle...");
            return;
        }


        UpdateStatus("Starting battle...");


        // MasterClient loads the scene for everyone
        if (!string.IsNullOrEmpty(pvpBattleSceneName))
        {
            PhotonNetwork.LoadLevel(pvpBattleSceneName);
        }
        else
        {
            Debug.LogError("[PhotonConnectionManager] PVP battle scene name not set!");
        }
    }

    // ================== UI Helpers ==================

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"[PhotonConnectionManager] {message}");
    }

    private void ShowLoading(bool show)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(show);
        }
    }

    // ================== Public Utilities ==================

    /// <summary>
    /// Set player nickname
    /// </summary>
    public void SetPlayerName(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = "Pilot_" + Random.Range(1000, 9999);
        }

        PhotonNetwork.NickName = playerName;
        Debug.Log($"[PhotonConnectionManager] Player name set to: {playerName}");
    }

    /// <summary>
    /// Leave current room
    /// </summary>
    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            UpdateStatus("Left room");
        }
    }

    /// <summary>
    /// Disconnect from Photon
    /// </summary>
    public void Disconnect()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            UpdateStatus("Disconnecting...");
        }
    }
}