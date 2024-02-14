using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using System.Threading;
using Quantum.Demo;
using System;
using ExitGames.Client.Photon;
using Quantum;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.UIElements;

public class QuickMatchHandler : MonoBehaviour, IInRoomCallbacks, IOnEventCallback, IConnectionCallbacks, IMatchmakingCallbacks {

    [HideInInspector] public QuantumLoadBalancingClient client;

    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private TextMeshProUGUI winText = null;
    [SerializeField] private TextMeshProUGUI ingameUsername = null;
    [SerializeField] private TMP_InputField usernameText = null;
    [SerializeField] private RuntimeConfigContainer RuntimeConfigContainer;
    [SerializeField] private ClientIdProvider.Type IdProvider = ClientIdProvider.Type.NewGuid;
    [SerializeField] private UnityEngine.UI.Button Button = null;
    [SerializeField] private GameObject UIObjectsToDisable = null;

    [HideInInspector] public List<Player> playerList = new List<Player>();
    [HideInInspector] public Dictionary<PlayerRef, PlayerStat> quantumPlayerList = new Dictionary<PlayerRef, PlayerStat>();
    [HideInInspector] public Dictionary<PlayerRef, string> quantumUsernameList = new Dictionary<PlayerRef, string>();

    public static string LastUsername {
        get => PlayerPrefs.GetString("Quantum.Demo.UIConnect.Username", Guid.NewGuid().ToString());
        set => PlayerPrefs.SetString("Quantum.Demo.UIConnect.Username", value);
    }
    public TMP_InputField UsernameText { get => usernameText; set => usernameText = value; }

    public string GetUsername() {
        return LastUsername;
    }

    public int GetWins(string player) {
        return PlayerPrefs.GetInt("Quantum.Demo.Wins" + player);
    }

    public void SetWins(string player, int value) {
        PlayerPrefs.SetInt("Quantum.Demo.Wins" + player, value);
    }

    public void AwardWin() {
        SetWins(LastUsername, GetWins(LastUsername) + 1);
        winText.text = "Wins: " + GetWins(LastUsername).ToString();
    }

    private void Awake() {
        usernameText.text = LastUsername;

        var appSettings = PhotonServerSettings.CloneAppSettings(PhotonServerSettings.Instance.AppSettings);
        client = new QuantumLoadBalancingClient(PhotonServerSettings.Instance.AppSettings.Protocol);
        client.ConnectUsingSettings(appSettings, IdProvider.ToString());

        client.AddCallbackTarget(this);
        
    }

    public void Connect() {
        usernameText.text = LastUsername;

        var appSettings = PhotonServerSettings.CloneAppSettings(PhotonServerSettings.Instance.AppSettings);
        client = new QuantumLoadBalancingClient(PhotonServerSettings.Instance.AppSettings.Protocol);
        client.ConnectUsingSettings(appSettings, IdProvider.ToString());

        client.AddCallbackTarget(this);
    }

    public void OnClickPlay() {
        client.OpJoinRandomRoom();

        LastUsername = usernameText.text;
    }

    private void Update() {
        client?.Service();
    }

    private void CreateRoom() {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = maxPlayers;
        EnterRoomParams enterRoomParams = new EnterRoomParams();
        enterRoomParams.RoomOptions = roomOptions;
        client.OpCreateRoom(enterRoomParams);
        playerList.Add(client.LocalPlayer);
    }

    #region Callbacks
    public void OnJoinedRoom() {
        Debug.Log($"Entered room '{client.CurrentRoom.Name}' as actor '{client.LocalPlayer.ActorNumber}'");

        StartQuantumGame(RuntimeConfigContainer.Config.Map.Id);

    }

    public void OnCreatedRoom() {
        Debug.Log("Created Room Successfully");
    }
    public void OnConnected() {
        Debug.Log("Connected!");
    }

    public void OnConnectedToMaster() {
        Debug.Log("Connected to Master!");
        Button.interactable = true;
    }

    public void OnJoinRandomFailed(short returnCode, string message) {
        CreateRoom();
    }

    public void OnFriendListUpdate(List<FriendInfo> friendList) {
        throw new System.NotImplementedException();
    }

    public void OnCreateRoomFailed(short returnCode, string message) {
        throw new System.NotImplementedException();
    }

    public void OnJoinRoomFailed(short returnCode, string message) {
        throw new System.NotImplementedException();
    }

    public void OnLeftRoom() {
        client.Disconnect();
        Connect();
    }

    public void OnDisconnected(DisconnectCause cause) {
        throw new System.NotImplementedException();
    }

    public void OnRegionListReceived(RegionHandler regionHandler) {
        throw new System.NotImplementedException();
    }

    public void OnCustomAuthenticationResponse(Dictionary<string, object> data) {
        throw new System.NotImplementedException();
    }

    public void OnCustomAuthenticationFailed(string debugMessage) {
        throw new System.NotImplementedException();
    }

    public void OnPlayerEnteredRoom(Player newPlayer) {
        playerList.Add(newPlayer);

        InGameUIHandler inGameUIHandler = FindObjectOfType<InGameUIHandler>();
        inGameUIHandler.InstantiatePlayerOnScoreBoard("Player: " + newPlayer.ActorNumber.ToString(), "0");
    }

    public void OnPlayerLeftRoom(Player otherPlayer) {

    }

    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) {
        throw new NotImplementedException();
    }

    public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) {
        throw new NotImplementedException();
    }

    public void OnMasterClientSwitched(Player newMasterClient) {
        throw new NotImplementedException();
    }

    public void OnEvent(EventData photonEvent) {

    }
    #endregion

    private void StartQuantumGame(AssetGuid mapGuid) {
        if (QuantumRunner.Default != null) {
            // There already is a runner, maybe because of duplicated calls, button events or race-conditions sending start and not deregistering from event callbacks in time.
            Debug.LogWarning($"Another QuantumRunner '{QuantumRunner.Default.name}' has prevented starting the game");
            return;
        }

        var config = RuntimeConfigContainer != null ? RuntimeConfig.FromByteArray(RuntimeConfig.ToByteArray(RuntimeConfigContainer.Config)) : new RuntimeConfig();
        config.Map.Id = mapGuid;
        var param = new QuantumRunner.StartParameters {
            RuntimeConfig = config,
            DeterministicConfig = DeterministicSessionConfigAsset.Instance.Config,
            ReplayProvider = null,
            GameMode = Photon.Deterministic.DeterministicGameMode.Multiplayer,
            FrameData = null,
            InitialFrame = 0,
            PlayerCount = client.CurrentRoom.MaxPlayers,
            LocalPlayerCount = 1,
            RecordingFlags = RecordingFlags.None,
            NetworkClient = client,
            StartGameTimeoutInSeconds = 10.0f
        };

        Debug.Log($"Starting QuantumRunner with map guid '{mapGuid}' and requesting {param.LocalPlayerCount} player(s).");

        // Joining with the same client id will result in the same quantum player slot which is important for reconnecting.
        var clientId = ClientIdProvider.CreateClientId(IdProvider, client);
        QuantumRunner.StartGame(clientId, param);

        ReconnectInformation.Refresh(client, TimeSpan.FromMinutes(1));
        winText.text = "Wins: " + GetWins(LastUsername).ToString();
        

        HideUI();
    }

    public void SetIngameUsername() {
        ingameUsername.text = "Username: " + GetUsername();
    }

    PlayerLink GetPlayerLink() {
        QuantumGame game = QuantumRunner.Default.Game;

        if (Utils.TryGetQuantumFrame(out Frame frame)) {
            foreach (var stat in frame.GetComponentIterator<PlayerLink>()) {
                if (game.PlayerIsLocal(stat.Component.Player)) {
                    return stat.Component;
                }
            }
        }

        return new PlayerLink();
    }

    public void HideUI() {
        UIObjectsToDisable.SetActive(false);
    }

    public void ShowUI() {
        UIObjectsToDisable.SetActive(true);
    }
}
