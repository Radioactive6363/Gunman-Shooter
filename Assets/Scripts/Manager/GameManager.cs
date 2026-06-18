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
    [SerializeField] private float timeTillDuel = 0.5f;

    [Header("Scene Management")]
    [SerializeField] private string[] duelScenes = { "DesertScene", "JungleScene", "CaveScene" };
    
    [Header("Lobby Scenes")]
    [SerializeField] private string lobbyRoomSceneName = "LobbyRoomScene"; 
    [SerializeField] private string mainMenuSceneName = "MenuScene";
    
    [Header("Game State")]
    [SerializeField] private GameState currentState;

    private int _currentSceneIndex;
    private Dictionary<int, int> _playerWins = new Dictionary<int, int>();
    private const int WINS_NEEDED = 2;
    private GameObject localPlayerInstance;

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
            CheckPlayers();
        }
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

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (currentState == GameState.WaitingForPlayers)
            CheckPlayers();
    }

    private void CheckPlayers()
    {
        int needed = PhotonNetwork.CurrentRoom.MaxPlayers;
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == needed)
        {
            Log.Info("All Players Connected. Starting match shortly...");
            StartCoroutine(WaitAndLoadDuelScene(5f)); 
        }
        else
        {
            Log.Info($"({PhotonNetwork.CurrentRoom.PlayerCount}/{needed} Players)");
        }
    }
    
    private IEnumerator WaitAndLoadDuelScene(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        LoadNextDuelScene();
    }

    private void SpawnPlayer()
    {
        Vector3 spawnPosition = new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f));
        localPlayerInstance = PhotonNetwork.Instantiate("PlayerPrefab", spawnPosition, Quaternion.identity);
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
    }
    

    public void RegisterKill(int deadPlayerActorNr)
    {
        if (currentState != GameState.Duel) return;

        SetGameState(GameState.PostDuel);

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.ActorNumber != deadPlayerActorNr)
            {
                if (!_playerWins.ContainsKey(player.ActorNumber))
                    _playerWins[player.ActorNumber] = 0;
                _playerWins[player.ActorNumber]++;
            }
        }

        int[] actorNrs = new int[_playerWins.Count];
        int[] wins = new int[_playerWins.Count];
        int i = 0;
        foreach (var kvp in _playerWins) { actorNrs[i] = kvp.Key; wins[i] = kvp.Value; i++; }

        photonView.RPC("SyncScoreRPC", RpcTarget.All, actorNrs, wins);

        int winner = -1;
        foreach (var kvp in _playerWins)
            if (kvp.Value >= WINS_NEEDED) { winner = kvp.Key; break; }

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