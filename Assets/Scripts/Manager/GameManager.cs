using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;
    public static event Action<GameState> OnGameStateChanged;
    
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
    
    public int TimeTillDuel { get => timeTillDuel; set => timeTillDuel = value; }

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
    
    private void CheckAllPlayersReady()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        int readyCount = 0;
        
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.TryGetValue(READY_PROP_KEY, out object isReady) && (bool)isReady)
            {
                readyCount++;
            }
        }

        Log.Info($"Players Ready: {readyCount}/{playerCount}");
        
        bool canStart = false;

        if (playerCount == 1)
        {
            canStart = (readyCount == 1); 
        }
        else
        {
            canStart = (readyCount == playerCount);
        }

        if (canStart)
        {
            Log.Info("Everyone Ready");
            SetGameState(GameState.Preparation); 
            StartCoroutine(WaitAndLoadDuelScene(2f));
        }
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
    
        if (PhotonNetwork.IsMasterClient)
            StartCoroutine(DuelState());
    }
    
    private IEnumerator WaitAndLoadDuelScene(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        LoadNextDuelScene();
    }

    private void SpawnPlayer()
    {
        Vector3 spawnPosition = new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f));
        _localPlayerInstance = PhotonNetwork.Instantiate("PlayerPrefab", spawnPosition, Quaternion.identity);
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