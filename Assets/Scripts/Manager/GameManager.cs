using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;
    public static event Action<GameState> OnGameStateChanged;
    public static event Action<int> OnDuelCountdownStarted;
    public static event Action<int> OnLobbyCountdownTick;
    
    [Header("Timer Settings")]
    [SerializeField] private int timeTillDuel = 3;

    [Header("Scene Management")]
    [SerializeField] private string[] duelScenes = { "DesertScene", "JungleScene", "CaveScene" };
    
    [Header("Lobby Scenes")]
    [SerializeField] private string lobbyRoomSceneName = "LobbyRoomScene"; 
    [SerializeField] private string mainMenuSceneName = "MenuScene";
    
    [Header("Game State")]
    [SerializeField] private GameState currentState;

    private int _currentSceneIndex;
    private List<int> _alivePlayers = new List<int>();
    private Dictionary<int, int> _playerWins = new Dictionary<int, int>();
    
    private const int WINS_NEEDED = 2;
    private const string READY_PROP_KEY = "IsReady";
    
    private GameObject _localPlayerInstance;
    public  GameObject LocalPlayer => _localPlayerInstance;
    
    public int TimeTillDuel => timeTillDuel;
    
    public void SetDuelStartDelay(float seconds)
    {
        timeTillDuel = Mathf.CeilToInt(seconds);
        Log.Info($"[GameManager] timeTillDuel sync: {timeTillDuel}s");
    }
    
    public void StartDuelCountdown()
    {
        OnDuelCountdownStarted?.Invoke(timeTillDuel);
        
        if (PhotonNetwork.IsMasterClient)
            StartCoroutine(DuelState());
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        if (SceneManager.GetActiveScene().name == lobbyRoomSceneName)
        {
            Initialize();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }
        }
    }

    private void SetGameState(GameState newState)
    {
        currentState = newState;
        OnGameStateChanged?.Invoke(newState);
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
        if (scene.name == lobbyRoomSceneName)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Initialize();
        }
        else if (scene.name == mainMenuSceneName)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Instance = null;
            Destroy(gameObject);
        }
        else
        {
            StartCoroutine(SpawnWhenReady(scene.name));
        }
    }
    
    private void Initialize()
    {
        if (SceneManager.GetActiveScene().name == lobbyRoomSceneName)
        {
            SetGameState(GameState.WaitingForPlayers);
            ResetReadyState();
        }
    }
    
    public void ToggleReady()
    {
        bool isCurrentlyReady = false;
        
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(READY_PROP_KEY, out object readyState))
        {
            isCurrentlyReady = (bool)readyState;
        }
        
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { READY_PROP_KEY, !isCurrentlyReady }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }
    
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (currentState != GameState.WaitingForPlayers) return;

        if (changedProps.ContainsKey(READY_PROP_KEY))
        {
            CheckAllPlayersReady();
        }
    }
    
    [SerializeField] private int lobbyCountdownSeconds = 5;
    private Coroutine _lobbyCountdownCoroutine;

    private void CheckAllPlayersReady()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        int readyCount  = 0;
        
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.TryGetValue(READY_PROP_KEY, out object isReady) && (bool)isReady)
                readyCount++;
        }

        Log.Info($"Players Ready: {readyCount}/{playerCount}");

        bool canStart = playerCount == 1 ? readyCount == 1 : readyCount == playerCount;

        if (canStart)
        {
            Log.Info("Everyone Ready - Starting lobby countdown");
            photonView.RPC("SetPreparationStateRPC", RpcTarget.All);

            if (_lobbyCountdownCoroutine != null) StopCoroutine(_lobbyCountdownCoroutine);
            _lobbyCountdownCoroutine = StartCoroutine(LobbyCountdownCoroutine());
        }
        else if (_lobbyCountdownCoroutine != null)
        {
            StopCoroutine(_lobbyCountdownCoroutine);
            _lobbyCountdownCoroutine = null;
            
            Log.Info("Lobby countdown cancelled - player unreadied");
            photonView.RPC("CancelLobbyCountdownRPC", RpcTarget.All);
        }
    }

    [PunRPC]
    private void SetPreparationStateRPC() => SetGameState(GameState.Preparation);

    [PunRPC]
    private void SetWaitingStateRPC() => SetGameState(GameState.WaitingForPlayers);

    [PunRPC]
    private void CancelLobbyCountdownRPC()
    {
        SetGameState(GameState.WaitingForPlayers);
        OnLobbyCountdownTick?.Invoke(-1);
    }

    private IEnumerator LobbyCountdownCoroutine()
    {
        int remaining = lobbyCountdownSeconds;
        while (remaining > 0)
        {
            OnLobbyCountdownTick?.Invoke(remaining);
            yield return new WaitForSeconds(1f);
            remaining--;
        }
        OnLobbyCountdownTick?.Invoke(0);
        _lobbyCountdownCoroutine = null;
        LoadNextDuelScene();
    }
    
    private void ResetReadyState()
    {
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { READY_PROP_KEY, false }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }
    
    private IEnumerator SpawnWhenReady(string sceneName)
    {
        while (Instance == null || !photonView.IsMine && !photonView.IsOwnerActive)
            yield return null;

        SpawnPlayer();
        SetGameState(GameState.Preparation);
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Log.Info($"Load Level: {sceneName}. Preparation.");
    }
    

    private void SpawnPlayer()
    {
        Vector3 spawnPosition = new Vector3(0, 1f, 0);
        _localPlayerInstance = PhotonNetwork.Instantiate("PlayerPrefab", spawnPosition, Quaternion.identity);
        SetLocalPlayerName();
    }

    private void SetLocalPlayerName()
    {
        var label = _localPlayerInstance.GetComponentInChildren<TMPro.TextMeshPro>();
        if (label != null)
            label.text = PhotonNetwork.LocalPlayer.NickName;
        else
            Log.Warning("[GameManager] No TextMeshPro name label found on PlayerPrefab.");
    }

    private void LoadNextDuelScene()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (_currentSceneIndex < duelScenes.Length)
        {
            PhotonNetwork.LoadLevel(duelScenes[_currentSceneIndex]);
            _currentSceneIndex++;
        }
        else
        {
            PhotonNetwork.LoadLevel(duelScenes[0]);
        }
    }

    private IEnumerator DuelState()
    {
        yield return new WaitForSeconds(timeTillDuel);
        photonView.RPC("DuelStateRPC", RpcTarget.All);
    }

    [PunRPC]
    private void DuelStateRPC()
    {
        SetGameState(GameState.Duel);
        Log.Info("Duel Started");
        
        _alivePlayers.Clear();
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            _alivePlayers.Add(player.ActorNumber);
        }
    }

    public void RegisterPlayerDeath(int deadPlayerActorNr)
    {
        if (currentState != GameState.Duel) return;
        
        if (_alivePlayers.Contains(deadPlayerActorNr))
        {
            _alivePlayers.Remove(deadPlayerActorNr);
            Log.Info($"Player {deadPlayerActorNr} eliminated. {_alivePlayers.Count} players alive.");
        }
        
        if (_alivePlayers.Count > 1) return;
        
        SetGameState(GameState.PostDuel);

        if (_alivePlayers.Count == 1)
        {
            int survivor = _alivePlayers[0];
        
            if (!_playerWins.ContainsKey(survivor))
                _playerWins[survivor] = 0;
            
            _playerWins[survivor]++;
            Log.Info($"Player {survivor} has won");
        }
        else
        {
            Log.Info("Tie, no Survivors.");
        }
        
        int[] actorNrs = new int[_playerWins.Count];
        int[] wins = new int[_playerWins.Count];
        int i = 0;
        foreach (var kvp in _playerWins) { actorNrs[i] = kvp.Key; wins[i] = kvp.Value; i++; }

        photonView.RPC("SyncScoreRPC", RpcTarget.All, actorNrs, wins);
        
        int winner = -1;
        foreach (var kvp in _playerWins)
        {
            if (kvp.Value >= WINS_NEEDED) { winner = kvp.Key; break; }
        }

        if (winner != -1)
            photonView.RPC("EndMatchRPC", RpcTarget.MasterClient);
        else
            StartCoroutine(ResetRoundRoutine());
    }

    [PunRPC]
    private void SyncScoreRPC(int[] actorNrs, int[] wins)
    {
        _playerWins.Clear();
        for (int i = 0; i < actorNrs.Length; i++)
            _playerWins[actorNrs[i]] = wins[i];
    }

    private IEnumerator ResetRoundRoutine()
    {
        yield return new WaitForSeconds(3f);
        if (PhotonNetwork.IsMasterClient)
            LoadNextDuelScene();
    }
    
    [PunRPC]
    private void EndMatchRPC()
    {
        Log.Info("Match ended, returning to lobby.");
        StartCoroutine(ReturnToLobbyRoutine());
    }

    private IEnumerator ReturnToLobbyRoutine()
    {
        yield return new WaitForSeconds(5f);
        
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
            PhotonNetwork.LoadLevel(lobbyRoomSceneName);
        }
        Instance = null;
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Log.Info($"Player {otherPlayer.NickName} disconnected.");
        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
            PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}