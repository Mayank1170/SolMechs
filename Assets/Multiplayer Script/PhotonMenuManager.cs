using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro; // Use TMP if you're using TextMeshPro

public class PhotonMenuManager : MonoBehaviourPunCallbacks
{
    [Header("Panels")]
    public GameObject connectingPanel;
    public GameObject menuPanel;
    public GameObject playPanel;
    public GameObject vsFriendPanel;
    public GameObject joinRoomPanel;
    public GameObject createRoomPanel;
    public GameObject startGamePanel;

    [Header("UI Elements")]
    public InputField usernameInput;
    public InputField joinRoomInput;
    public InputField createRoomInput;
    public Text playerListText; // to show both players
    public Button startGameButton;

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
        ShowOnly(connectingPanel);
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        ShowOnly(menuPanel);
    }

    public void OnPlayClicked()
    {
        PhotonNetwork.NickName = usernameInput.text;
        ShowOnly(playPanel);
    }

    public void OnVsFriendClicked()
    {
        ShowOnly(vsFriendPanel);
    }

    public void OnJoinRoomClicked()
    {
        ShowOnly(joinRoomPanel);
    }

    public void OnCreateRoomClicked()
    {
        ShowOnly(createRoomPanel);
    }

    public void CreateRoomNow()
    {
        if (createRoomInput.text.Length > 0)
        {
            RoomOptions options = new RoomOptions { MaxPlayers = 2 };
            PhotonNetwork.CreateRoom(createRoomInput.text, options);
        }
    }

    public void JoinRoomNow()
    {
        if (joinRoomInput.text.Length > 0)
        {
            PhotonNetwork.JoinRoom(joinRoomInput.text);
        }
    }

    public override void OnJoinedRoom()
    {
        ShowOnly(startGamePanel);
        UpdatePlayerList();
        startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerList();
    }

    void UpdatePlayerList()
    {
        playerListText.text = "";
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            playerListText.text += p.NickName + "\n";
        }
    }

    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("PilotMechBattleV1"); // Or your game scene
        }
    }

    void ShowOnly(GameObject panel)
    {
        connectingPanel.SetActive(false);
        menuPanel.SetActive(false);
        playPanel.SetActive(false);
        vsFriendPanel.SetActive(false);
        joinRoomPanel.SetActive(false);
        createRoomPanel.SetActive(false);
        startGamePanel.SetActive(false);

        panel.SetActive(true);
    }
}
