using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

/// <summary>
/// Enhanced lobby UI with room browser, player list, and match status.
/// Works alongside PhotonConnectionManager.
/// </summary>
public class PhotonLobbyUI : MonoBehaviourPunCallbacks
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject lobbyPanel;
    public GameObject roomPanel;
    public GameObject roomBrowserPanel;

    [Header("Room Browser")]
    public Transform roomListContent;
    public GameObject roomListItemPrefab;
    public Button refreshRoomsButton;

    [Header("Room Info")]
    public Text roomNameText;
    public Text playerCountText;
    public Transform playerListContent;
    public GameObject playerListItemPrefab;
    public Button leaveRoomButton;
    public Button startMatchButton;

    [Header("Player Info")]
    public InputField playerNameInput;
    public Text currentPlayerNameText;

    private Dictionary<string, GameObject> roomListItems = new Dictionary<string, GameObject>();
    private Dictionary<int, GameObject> playerListItems = new Dictionary<int, GameObject>();

    private void Start()
    {
        // Setup button listeners
        if (refreshRoomsButton) refreshRoomsButton.onClick.AddListener(RefreshRoomList);
        if (leaveRoomButton) leaveRoomButton.onClick.AddListener(LeaveRoom);
        if (startMatchButton) startMatchButton.onClick.AddListener(StartMatch);

        // Load saved player name
        string savedName = PlayerPrefs.GetString("PlayerName", "");
        if (!string.IsNullOrEmpty(savedName))
        {
            PhotonNetwork.NickName = savedName;
            if (playerNameInput) playerNameInput.text = savedName;
        }
        else
        {
            PhotonNetwork.NickName = "Pilot_" + Random.Range(1000, 9999);
        }

        UpdatePlayerNameDisplay();
        ShowPanel("main");
    }

    // ================== Panel Management ==================

    public void ShowPanel(string panelName)
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(panelName == "main");
        if (lobbyPanel) lobbyPanel.SetActive(panelName == "lobby");
        if (roomPanel) roomPanel.SetActive(panelName == "room");
        if (roomBrowserPanel) roomBrowserPanel.SetActive(panelName == "browser");
    }

    // ================== Player Name ==================

    public void OnPlayerNameChanged()
    {
        if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text))
        {
            string newName = playerNameInput.text;
            PhotonNetwork.NickName = newName;
            PlayerPrefs.SetString("PlayerName", newName);
            PlayerPrefs.Save();
            UpdatePlayerNameDisplay();
        }
    }

    private void UpdatePlayerNameDisplay()
    {
        if (currentPlayerNameText)
        {
            currentPlayerNameText.text = PhotonNetwork.NickName;
        }
    }

    // ================== Photon Callbacks ==================

    public override void OnConnectedToMaster()
    {
        Debug.Log("[PhotonLobbyUI] Connected to Master");
        ShowPanel("lobby");
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[PhotonLobbyUI] Joined Lobby");
        ShowPanel("lobby");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[PhotonLobbyUI] Joined room: {PhotonNetwork.CurrentRoom.Name}");
        ShowPanel("room");
        UpdateRoomInfo();
        UpdatePlayerList();

        // Only master client can start
        if (startMatchButton)
        {
            startMatchButton.interactable = PhotonNetwork.IsMasterClient;
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[PhotonLobbyUI] Left room");
        ShowPanel("lobby");
        ClearPlayerList();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[PhotonLobbyUI] Player joined: {newPlayer.NickName}");
        UpdateRoomInfo();
        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[PhotonLobbyUI] Player left: {otherPlayer.NickName}");
        UpdateRoomInfo();
        UpdatePlayerList();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"[PhotonLobbyUI] New master: {newMasterClient.NickName}");

        if (startMatchButton)
        {
            startMatchButton.interactable = PhotonNetwork.IsMasterClient;
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        UpdateRoomBrowser(roomList);
    }

    // ================== Room Browser ==================

    public void ShowRoomBrowser()
    {
        ShowPanel("browser");
        RefreshRoomList();
    }

    public void RefreshRoomList()
    {
        // Room list updates automatically via OnRoomListUpdate
        Debug.Log("[PhotonLobbyUI] Refreshing room list...");
    }

    private void UpdateRoomBrowser(List<RoomInfo> roomList)
    {
        if (roomListContent == null || roomListItemPrefab == null) return;

        // Update existing rooms or remove if they're gone
        foreach (RoomInfo info in roomList)
        {
            // Room removed
            if (info.RemovedFromList)
            {
                if (roomListItems.ContainsKey(info.Name))
                {
                    Destroy(roomListItems[info.Name]);
                    roomListItems.Remove(info.Name);
                }
            }
            // Room updated or added
            else
            {
                if (!roomListItems.ContainsKey(info.Name))
                {
                    // Create new room item
                    GameObject item = Instantiate(roomListItemPrefab, roomListContent);
                    roomListItems[info.Name] = item;

                    // Setup room item
                    SetupRoomItem(item, info);
                }
                else
                {
                    // Update existing room item
                    SetupRoomItem(roomListItems[info.Name], info);
                }
            }
        }
    }

    private void SetupRoomItem(GameObject item, RoomInfo info)
    {
        // Find text components
        Text[] texts = item.GetComponentsInChildren<Text>();
        if (texts.Length > 0) texts[0].text = info.Name;
        if (texts.Length > 1) texts[1].text = $"{info.PlayerCount}/{info.MaxPlayers}";

        // Setup join button
        Button joinBtn = item.GetComponentInChildren<Button>();
        if (joinBtn)
        {
            joinBtn.onClick.RemoveAllListeners();
            joinBtn.interactable = info.PlayerCount < info.MaxPlayers;

            string roomName = info.Name;
            joinBtn.onClick.AddListener(() => JoinRoomByName(roomName));
        }
    }

    private void JoinRoomByName(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    // ================== Room Info ==================

    private void UpdateRoomInfo()
    {
        if (!PhotonNetwork.InRoom) return;

        if (roomNameText)
        {
            roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        }

        if (playerCountText)
        {
            int current = PhotonNetwork.CurrentRoom.PlayerCount;
            int max = PhotonNetwork.CurrentRoom.MaxPlayers;
            playerCountText.text = $"Players: {current}/{max}";
        }
    }

    // ================== Player List ==================

    private void UpdatePlayerList()
    {
        if (!PhotonNetwork.InRoom || playerListContent == null) return;

        ClearPlayerList();

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            GameObject item = Instantiate(playerListItemPrefab, playerListContent);
            playerListItems[player.ActorNumber] = item;

            Text playerText = item.GetComponentInChildren<Text>();
            if (playerText)
            {
                string prefix = player.IsMasterClient ? "[HOST] " : "";
                string suffix = player.IsLocal ? " (You)" : "";
                playerText.text = prefix + player.NickName + suffix;
            }

            // Optional: Add ready/status indicators
            Image statusImg = item.GetComponentInChildren<Image>();
            if (statusImg)
            {
                statusImg.color = player.IsLocal ? Color.green : Color.white;
            }
        }
    }

    private void ClearPlayerList()
    {
        foreach (var item in playerListItems.Values)
        {
            if (item != null) Destroy(item);
        }
        playerListItems.Clear();
    }

    // ================== Room Actions ==================

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void StartMatch()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[PhotonLobbyUI] Only master client can start match!");
            return;
        }

        // Check if room is full or has minimum players
        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            Debug.LogWarning("[PhotonLobbyUI] Need at least 2 players to start!");
            return;
        }

        // Close the room so no one else can join
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        // MasterClient loads the battle scene
        // (This assumes PhotonConnectionManager handles scene loading)
        var connectionManager = FindObjectOfType<PhotonConnectionManager>();
        if (connectionManager)
        {
            // The connection manager's OnJoinedRoom will handle loading
            // Or you can directly call:
            // PhotonNetwork.LoadLevel("BattleScene_PVP");
        }
    }

    // ================== Quick Actions ==================

    public void QuickMatch()
    {
        var connectionManager = FindObjectOfType<PhotonConnectionManager>();
        if (connectionManager)
        {
            connectionManager.QuickMatch();
        }
        else
        {
            PhotonNetwork.JoinRandomRoom();
        }
    }

    public void CreatePrivateRoom()
    {
        string roomName = "Private_" + Random.Range(1000, 9999);
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = false, // Private room not shown in browser
            IsOpen = true
        };
        PhotonNetwork.CreateRoom(roomName, options);
    }

    public void BackToMainMenu()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        ShowPanel("main");
    }

    // ================== Utility ==================

    public void CopyRoomCode()
    {
        if (PhotonNetwork.InRoom)
        {
            string roomCode = PhotonNetwork.CurrentRoom.Name;
            GUIUtility.systemCopyBuffer = roomCode;
            Debug.Log($"[PhotonLobbyUI] Room code copied: {roomCode}");

            // Optional: Show a toast/notification
            // ShowNotification("Room code copied!");
        }
    }

    public void SetRoomVisibility(bool visible)
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsVisible = visible;
            PhotonNetwork.CurrentRoom.IsOpen = visible;
            Debug.Log($"[PhotonLobbyUI] Room visibility set to: {visible}");
        }
    }
}