using System;
using System.Collections;
using System.IO;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    [Header("Debug Latency")]
    [SerializeField] private bool debuggingLatency = true;
    [SerializeField] private float incomeLag;
    [SerializeField] private float outcomeLag;
    [SerializeField] private float incomeJitter;
    [SerializeField] private float outcomeJitter;
    
    private string _savePath => Path.Combine(Application.persistentDataPath, "player_profile.json");
    
    public static PhotonManager Instance;
    public static string LastDisconnectErrorMessage = "";
    
    public Action OnConnectedToMasterEvent;
    public Action<short, string> OnJoinRoomFailedEvent;
    
    private Coroutine _disconnectTracker;
    
    private bool _initialSetupDone = false;
    public Action<DisconnectCause> OnDisconnectedEvent;

    public static bool ShowDisconnectErrorOnLoad = false; 
    
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

    public override void OnEnable()
    {
        base.OnEnable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "LobbyRoomScene" && PhotonNetwork.IsMasterClient)
        {
            if (GameManager.Instance == null)
            {
                PhotonNetwork.InstantiateRoomObject("GameManager", Vector3.zero, Quaternion.identity);
                Log.Info("GameManager instantiated.");
            }
        }
    }

    private void LatencyDebugging()
    {
    #if UNITY_EDITOR
        if (debuggingLatency)
        {
            PhotonNetwork.NetworkingClient.LoadBalancingPeer.IsSimulationEnabled = true;
            var settings = PhotonNetwork.NetworkingClient.LoadBalancingPeer.NetworkSimulationSettings;
            settings.IncomingLag = (int)incomeLag;
            settings.OutgoingLag = (int)outcomeLag;
            settings.IncomingJitter = (int)incomeJitter;
            settings.OutgoingJitter = (int)outcomeJitter;
            settings.IncomingLossPercentage = 0;
            settings.OutgoingLossPercentage = 0;
        }
    #endif
    }
    
    private void Start()
    {
        LatencyDebugging();
        LoadPlayerData();
        
        Log.Info($"Connecting as: {PhotonNetwork.NickName}");
        PhotonNetwork.ConnectUsingSettings();
        Log.Info("Loading");
    }
    
    private void LoadPlayerData()
    {
        PlayerProfilePackage loadedData = JsonDataHandler.LoadJSON(_savePath);

        if (loadedData != null)
        {
            PhotonNetwork.NickName = loadedData.nickname;
            Log.Info($"Loaded profile for: {PhotonNetwork.NickName}");
        }
        else
        {
            string defaultName = PlayerPrefs.GetString("PlayerName", "Default");
            PhotonNetwork.NickName = defaultName;
            
            SavePlayerData(defaultName, 0, false);
        }
    }
    
    public void SavePlayerData(string nickname, int avatarId, bool isReady)
    {
        PlayerProfilePackage newData = new PlayerProfilePackage(nickname, avatarId, isReady);
        JsonDataHandler.SaveJSON(_savePath, newData);
        
        PhotonNetwork.NickName = nickname;
        
        PlayerPrefs.SetString("PlayerName", nickname);
        PlayerPrefs.Save();
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
        }
        else
        {
            Log.Info("Rejoined Lobby");
        }
        OnConnectedToMasterEvent?.Invoke(); 
    }
    
    public override void OnDisconnected(DisconnectCause cause)
    {
        if (cause == DisconnectCause.DisconnectByClientLogic)
        {
            Log.Info("Disconnected: Player left voluntarily");
        }
        else 
        {
            ShowDisconnectErrorOnLoad = true;
            LastDisconnectErrorMessage = $"Network Error: {cause}"; 

            if (SceneManager.GetActiveScene().name != "MenuScene") 
            {
                SceneManager.LoadScene("MenuScene");
            }
            CheckDisconnectionError(cause);
        }
    }
    
    #endregion

    #region RoomLogic

    public override void OnJoinedRoom()
    {
        if (!PhotonNetwork.CurrentRoom.IsVisible)
        {
            PhotonNetwork.LeaveRoom();
            return;
        }

        Log.Info($"JoinedRoom: {PhotonNetwork.CurrentRoom.Name}, " +
                 $"PlayerCount: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}, " +
                 $"IsMaster: {PhotonNetwork.IsMasterClient}");

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("LobbyRoomScene");
        }
    }

   
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Log.Info($"Player {newPlayer.NickName} has joined the room");
    }

    public void CreateRoom(string roomName)
    {
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.NetworkClientState != ClientState.Joining)
        {
            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = 8,
                EmptyRoomTtl = 10000,
                CleanupCacheOnLeave = false
            };
            PhotonNetwork.CreateRoom(roomName, roomOptions);
        }
    }
    
    public override void OnLeftRoom() //Local, only client
    {
        Log.Info("Left room");
    }

    
    public override void OnPlayerLeftRoom(Player otherPlayer) //All Players
    {
        Log.Info("Player " + otherPlayer.NickName + "left the Room");
    }
    
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        CheckRoomFailedError(returnCode, message);
        string friendlyMessage = GetFriendlyJoinError(returnCode, message);
        OnJoinRoomFailedEvent?.Invoke(returnCode, friendlyMessage);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        CheckRoomCreationFailedError(returnCode, message);
        string friendlyMessage = GetFriendlyCreateError(returnCode, message);
        OnJoinRoomFailedEvent?.Invoke(returnCode, friendlyMessage);
    }
    
    #endregion

    #region CleanUp Logic

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Log.Info("Master client has Switched to " + newMasterClient.NickName);
    }

    #endregion
    
    #region JoinBy

    public void JoinByNameRoom(string roomName)
    {
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.NetworkClientState != ClientState.Joining)
        {
            PhotonNetwork.JoinRoom(roomName);
        }
    }

    public void JoinBySearchRoom(string roomName)
    {
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.NetworkClientState != ClientState.Joining)
        {
            PhotonNetwork.JoinRoom(roomName);
        }
    }

    #endregion

    #region FailChecks
    
    private void CheckDisconnectionError(DisconnectCause cause)
    {
        OnDisconnectedEvent?.Invoke(cause);
        switch (cause)
        {
            case DisconnectCause.ExceptionOnConnect:
            case DisconnectCause.Exception:
            case DisconnectCause.ServerTimeout:
            case DisconnectCause.ClientTimeout:
                if (_disconnectTracker != null) StopCoroutine(_disconnectTracker);
                _disconnectTracker = StartCoroutine(TrackDisconnectTime(cause)); 
                break;
        
            case DisconnectCause.InvalidAuthentication:
            case DisconnectCause.MaxCcuReached:
            case DisconnectCause.DisconnectByServerLogic:
            case DisconnectCause.DisconnectByServerReasonUnknown:
                Log.Error($"Disconnected: Server error ({cause})");
                break;
        
            default:
                Log.Warning($"Disconnected: Unknown cause ({cause})");
                break;
        }
    }
    private void CheckRoomFailedError(short returnCode, string message)
    {
        switch (returnCode)
        {
            case ErrorCode.GameDoesNotExist:
                Log.Warning("Room does not exist");
                break;
            case ErrorCode.GameFull:
                Log.Warning("Room is full");
                break;
            case ErrorCode.GameClosed:
                Log.Warning("Room is closed");
                break;
            case ErrorCode.UserBlocked:
                Log.Error("User is blocked");
                break;
            default:
                Log.Warning($"Join failed ({returnCode}): {message}");
                break;
        }
    }
    private void CheckRoomCreationFailedError(short returnCode, string message)
    {
        switch (returnCode)
        {
            case ErrorCode.GameIdAlreadyExists:
                Log.Warning("Room already exists");
                break;
            case ErrorCode.InvalidAuthentication:
                Log.Error("Invalid authentication");
                break;
            case ErrorCode.MaxCcuReached:
                Log.Error("Max concurrent users reached");
                break;
            case ErrorCode.InvalidOperation:
                Log.Error("Invalid operation");
                break;
            default:
                Log.Warning($"Create room failed ({returnCode}): {message}");
                break;
        }
        Log.Warning(message);
    }
    
    private IEnumerator TrackDisconnectTime(DisconnectCause cause)
    {
        float elapsed = 0f;
        float nextLogTime = 5f;
        Log.Warning($"Tracking disconnect time... (cause: {cause})");

        while (!PhotonNetwork.IsConnected)
        {
            elapsed += Time.deltaTime;
           
            if (elapsed >= nextLogTime)
            {
                Log.Warning($"Still disconnected... {Mathf.RoundToInt(elapsed)}s elapsed");
                nextLogTime += 5f;
            }
           
            if (elapsed >= 30f)
            {
                Log.Error($"Disconnect timeout reached after 30s. Cause: {cause}");
                yield break;
            }
            yield return null;
        }
        Log.Info($"Reconnected after {elapsed:F1}s");
    }
    
    public void ManualReconnect()
   {
       if (PhotonNetwork.IsConnected) return;

       Log.Info("Initializing Manual Rejoin");
       
       if (PhotonNetwork.ReconnectAndRejoin())
       {
           Log.Info("Reconnecting and Returning to Menu");
       }
       else if (PhotonNetwork.Reconnect())
       {
           Log.Info("Reconnecting to Services");
       }
       else
       {
           Log.Info("Initializing Clear Connection.");
           PhotonNetwork.ConnectUsingSettings();
       }
   }
    
    #endregion

    #region ErrorLogging for UI

    private string GetFriendlyJoinError(short returnCode, string message) => returnCode switch
    {
        ErrorCode.GameDoesNotExist => "Room Not Found.",
        ErrorCode.GameFull         => "Room is Full.",
        ErrorCode.GameClosed       => "Room is Closed.",
        _                          => $"Error ({returnCode}): {message}"
    };
    
    private string GetFriendlyCreateError(short returnCode, string message) => returnCode switch
    {
        ErrorCode.GameIdAlreadyExists => "Room Name Already Exists.",
        ErrorCode.InvalidAuthentication => "Authentication Error.",
        _                               => $"Error ({returnCode}): {message}"
    };

    #endregion
    
}
