using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    private bool _initialSetupDone = false;
    private Coroutine _disconnectTracker;
    
    public static PhotonManager Instance;
    public Action OnRoom;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        
            Application.runInBackground = true;
            PhotonNetwork.KeepAliveInBackground = 15000;
            
            PhotonNetwork.LogLevel = PunLogLevel.ErrorsOnly;
            PhotonNetwork.NetworkingClient.LoadBalancingPeer.DebugOut = DebugLevel.ERROR;
        }
        else
        {
            Destroy(gameObject);
        }
        QualitySettings.vSyncCount = 0; 
        Application.targetFrameRate = 60;
    }
    
    void Start()
    {
        PhotonNetwork.NickName = PlayerPrefs.GetString("PlayerName", "Default");
        Log.Info($"Connecting as: {PhotonNetwork.NickName}");
        PhotonNetwork.ConnectUsingSettings();
        Log.Info("Loading");
    }

    #region PhotonServices Logic

    public override void OnConnectedToMaster()
    {
        Log.Info("Connected to Services");
        PhotonNetwork.JoinLobby();
    }
    
    public override void OnJoinedLobby()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        if (!_initialSetupDone)
        {
            Log.Info("Joined Lobby");
            _initialSetupDone = true;
            CreateRoom("4-ConnectionStats");
        }
        else
        {
            Log.Info("Rejoined Lobby");
        }
    }
    
    #endregion

    public override void OnJoinedRoom()
    {
        if (!PhotonNetwork.CurrentRoom.IsVisible)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            string roomName = PhotonNetwork.CurrentRoom.Name;
            int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
            int maxPlayerCount = PhotonNetwork.CurrentRoom.MaxPlayers;
            bool isMaster = PhotonNetwork.IsMasterClient;

            Log.Info("JoinedRoom: " + roomName + ", PlayerCount: " + playerCount + "/" + maxPlayerCount + ",IsMaster: " + isMaster);

            PhotonNetwork.LoadLevel("4-ConnectionStats");
            Log.Info("Scene Loaded");
            OnRoom?.Invoke();
        }
    }

    private void CreateRoom(string roomName)
    {
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.NetworkClientState != ClientState.Joining)
        {
            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = 5,
                EmptyRoomTtl = 10000,
                CleanupCacheOnLeave = true
            };
            PhotonNetwork.CreateRoom(roomName, roomOptions);
        }
    }
}
