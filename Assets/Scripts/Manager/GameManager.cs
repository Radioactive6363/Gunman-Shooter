using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;
    public GameState currentState;

    [Header("Scene Management")]
    public string[] duelScenes = { "DesertScene", "JungleScene", "CaveScene" };
    
    [Header("Lobby Scenes")]
    public string lobbyRoomSceneName = "LobbyRoomScene"; 
    public string mainMenuSceneName = "MenuScene";

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

    private void SetGameState(GameState newState)
    {
        currentState = newState;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (currentState == GameState.WaitingForPlayers)
        {
            CheckPlayers();
        }
    }

    private void CheckPlayers()
    {
        int needed = PhotonNetwork.CurrentRoom.MaxPlayers;
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == needed)
        {
            Log.Info("All Players Connected.");
            LoadNextDuelScene();
        }
        else
        {
            Log.Info($"({PhotonNetwork.CurrentRoom.PlayerCount}/{needed} Players)");
        }
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
            Initialize();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (scene.name == mainMenuSceneName)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else // duel scenes
        {
            SpawnPlayer();
            SetGameState(GameState.Preparation);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Log.Info($"Load Level: {scene.name}. Preparation.");
            if (PhotonNetwork.IsMasterClient)
                StartCoroutine(DuelState());
        }
    }

    private void SpawnPlayer()
    {
        Vector3 spawnPosition = new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f));
        localPlayerInstance = PhotonNetwork.Instantiate("PlayerPrefab", spawnPosition, Quaternion.identity);
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
    
    [PunRPC]
    private void EndMatchRPC()
    {
        Log.Info("Returning to lobby.");
        StartCoroutine(ReturnToLobbyRoutine());
    }

    private IEnumerator ResetRoundRoutine()
    {
        yield return new WaitForSeconds(3f);
        
        if (PhotonNetwork.IsMasterClient)
        {
            LoadNextDuelScene();
        }
    }

    private IEnumerator ReturnToLobbyRoutine()
    {
        yield return new WaitForSeconds(5f);
        _playerWins.Clear();
        _currentSceneIndex = 0;
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LeaveRoom();
        }

    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Log.Info($"Player {otherPlayer.NickName} disconnected.");
        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            SceneManager.LoadScene(mainMenuSceneName);
            Destroy(gameObject);
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(mainMenuSceneName);
        Destroy(gameObject);
    }

    public void Initialize()
    {
        if (SceneManager.GetActiveScene().name == lobbyRoomSceneName)
        {
            SetGameState(GameState.WaitingForPlayers);
            CheckPlayers();
        }
    }

    private IEnumerator DuelState()
    {
        yield return new WaitForSeconds(5f);

        photonView.RPC("DuelStateRPC", RpcTarget.All);
    }

    [PunRPC]
    private void DuelStateRPC()
    {
        SetGameState(GameState.Duel);
        Log.Info("DUEL!");
    }
}