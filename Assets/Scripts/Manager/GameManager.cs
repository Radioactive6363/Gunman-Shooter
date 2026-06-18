using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.RemoteConfig;
using Unity.Services.RemoteConfig;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;
    public static event Action<GameState> OnGameStateChanged;
    public static event Action<int> OnLobbyCountdownTick;
    public static event Action<string> OnServerMessageDeclared;
    public static event Action<string> OnRoundWinnerDeclared;
    public static event Action<string> OnMatchWinnerDeclared;
    public static event Action OnMatchCancelled;
    
    [Header("Timer Settings")]
    [SerializeField] private int timeTillDuel = 3;
    [SerializeField] private int lobbyCountdownSeconds = 5;

    [Header("Scene Management")]
    [SerializeField] private string[] duelScenes = { "DesertScene", "JungleScene", "CaveScene" };
    
    [Header("Lobby Scenes")]
    [SerializeField] private string lobbyRoomSceneName = "LobbyRoomScene"; 
    [SerializeField] private string mainMenuSceneName = "MenuScene";
    
    [Header("Game State")]
    [SerializeField] private GameState currentState;
    
    public struct userAttributes { }
    public struct appAttributes { }

    private int _currentSceneIndex;
    private List<int> _alivePlayers = new List<int>();
    private Dictionary<int, int> _playerWins = new Dictionary<int, int>();
    
    private const int WINS_NEEDED = 2;
    private const string READY_PROP_KEY = "IsReady";

    private Coroutine _lobbyCountdownCoroutine;
    private GameObject _localPlayerInstance;
    public  GameObject LocalPlayer => _localPlayerInstance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            _ = InitializeRemoteConfig();
        }
        else
        {
            Destroy(gameObject);
        }
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
    
    private async Task InitializeRemoteConfig()
    {
        try
        {
            var task = await RemoteConfigService.Instance.FetchConfigsAsync(new userAttributes(), new appAttributes());
            
            int seed = RemoteConfigService.Instance.appConfig.GetInt("DesiredGameOrder");
            
            if (seed != 0)
            {
                ShuffleLevels(seed);
                Log.Info($"[LiveOps] Maps Swapped with: {seed}");
            }
        }
        catch (Exception e)
        {
            Log.Warning($"[LiveOps] Cant Apply Remote Config: {e.Message}");
        }
    }
    
    private void ShuffleLevels(int seed)
    {
        System.Random rng = new System.Random(seed);
        int n = duelScenes.Length;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (duelScenes[k], duelScenes[n]) = (duelScenes[n], duelScenes[k]);
        }
        Log.Info($"[LiveOps] Levels Mixed with seed: {seed}");
    }
    
    public void SetDuelStartDelay(float seconds)
    {
        timeTillDuel = Mathf.CeilToInt(seconds);
        Log.Info($"[GameManager] timeTillDuel sync: {timeTillDuel}s");
    }
    
    public void StartDuelCountdown()
    {
        if (PhotonNetwork.IsMasterClient)
            StartCoroutine(DuelState());
    }

    private void SetGameState(GameState newState)
    {
        currentState = newState;
        OnGameStateChanged?.Invoke(newState);
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
        
        bool canStart = (playerCount >= 2) && (readyCount == playerCount);

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
        
            Log.Info("Lobby countdown cancelled - player unreadied or not enough players");
            photonView.RPC("CancelLobbyCountdownRPC", RpcTarget.All);
        }
    }

    [PunRPC]
    private void SetPreparationStateRPC() => SetGameState(GameState.Preparation);

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
            photonView.RPC(nameof(UpdateLobbyCountdownRPC), RpcTarget.All, remaining);
            yield return new WaitForSeconds(1f);
            remaining--;
        }

        photonView.RPC(nameof(UpdateLobbyCountdownRPC), RpcTarget.All, 0);
        _lobbyCountdownCoroutine = null;

        LoadNextDuelScene();
    }
    
    [PunRPC]
    private void UpdateLobbyCountdownRPC(int remaining)
    {
        OnLobbyCountdownTick?.Invoke(remaining);
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
        SpawnPlayer();
        SetGameState(GameState.Preparation);
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Log.Info($"Load Level: {sceneName}. Preparation.");
        yield break;
    }
    
    private void SpawnPlayer()
    {
        Vector3 spawnPosition = new Vector3(0, 1f, 0);
        _localPlayerInstance = PhotonNetwork.Instantiate("PlayerPrefab", spawnPosition, Quaternion.identity);
    }

    private void LoadNextDuelScene()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        PhotonNetwork.LoadLevel(duelScenes[_currentSceneIndex]);
        _currentSceneIndex = (_currentSceneIndex + 1) % duelScenes.Length;
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
            Player deadPlayer = PhotonNetwork.CurrentRoom.GetPlayer(deadPlayerActorNr);
            
            if (deadPlayer != null)
            {
                photonView.RPC("BroadcastServerMessageRPC", RpcTarget.All, $"¡{deadPlayer.NickName} has fallen!");
            }
            Log.Info($"Player {deadPlayerActorNr} eliminated. {_alivePlayers.Count} players alive.");
        }
        
        if (_alivePlayers.Count > 1) return;
        
        int survivor = -1;
        if (_alivePlayers.Count == 1)
        {
            survivor = _alivePlayers[0];
            if (!_playerWins.ContainsKey(survivor)) _playerWins[survivor] = 0;
            _playerWins[survivor]++;
        }
        
        int[] actorNrs = new int[_playerWins.Count];
        int[] wins = new int[_playerWins.Count];
        int i = 0;
        foreach (var kvp in _playerWins) { actorNrs[i] = kvp.Key; wins[i] = kvp.Value; i++; }
        
        photonView.RPC("SyncRoundResultRPC", RpcTarget.All, survivor, actorNrs, wins);
        
        int matchWinner = -1;
        foreach (var kvp in _playerWins)
        {
            if (kvp.Value >= WINS_NEEDED) { matchWinner = kvp.Key; break; }
        }
        
        if (matchWinner != -1)
            photonView.RPC("EndMatchRPC", RpcTarget.All, matchWinner);
        else
            StartCoroutine(ResetRoundRoutine());
    }

    [PunRPC]
    private void SyncRoundResultRPC(int roundWinnerActorNr, int[] actorNrs, int[] wins)
    {
        SetGameState(GameState.PostDuel);

        _playerWins.Clear();
        for (int i = 0; i < actorNrs.Length; i++)
            _playerWins[actorNrs[i]] = wins[i];
        
        string winnerName = roundWinnerActorNr != -1 ? PhotonNetwork.CurrentRoom.GetPlayer(roundWinnerActorNr).NickName : "No one (Draw)";
        
        OnRoundWinnerDeclared?.Invoke(winnerName);
    }

    private IEnumerator ResetRoundRoutine()
    {
        yield return new WaitForSeconds(3f);
        if (PhotonNetwork.IsMasterClient)
            LoadNextDuelScene();
    }
    
    [PunRPC]
    private void EndMatchRPC(int matchWinnerActorNr)
    {
        SetGameState(GameState.MatchOver);
        
        string winnerName = matchWinnerActorNr != -1 
            ? PhotonNetwork.CurrentRoom.GetPlayer(matchWinnerActorNr).NickName 
            : "Unknown";

        OnMatchWinnerDeclared?.Invoke(winnerName);
        
        if (PhotonNetwork.IsMasterClient)
            StartCoroutine(ReturnToLobbyRoutine());
    }

    private IEnumerator ReturnToLobbyRoutine()
    {
        yield return new WaitForSeconds(5f);
        
        if (PhotonNetwork.IsMasterClient)
        {
            Destroy(gameObject);
            PhotonNetwork.LoadLevel(lobbyRoomSceneName);
        }
        Instance = null;
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Log.Info($"Player {otherPlayer.NickName} disconnected.");
        
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("BroadcastServerMessageRPC", RpcTarget.All, $"{otherPlayer.NickName} has fled the duel");

            if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
            {
                photonView.RPC("CancelMatchRPC", RpcTarget.All);
            }
            else
            {
                if (currentState == GameState.Duel && _alivePlayers.Contains(otherPlayer.ActorNumber))
                {
                    RegisterPlayerDeath(otherPlayer.ActorNumber);
                }
            }
        }
    }
    
    [PunRPC]
    private void CancelMatchRPC()
    {
        SetGameState(GameState.MatchOver);
        OnMatchCancelled?.Invoke();
        
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(ReturnToLobbyRoutine()); 
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    [PunRPC]
    private void BroadcastServerMessageRPC(string message)
    {
        OnServerMessageDeclared?.Invoke(message);
    }
}