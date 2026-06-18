using System.Collections;
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
    private int _player1Wins;
    private int _player2Wins;
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
    
    public void PlayerEnteredRoom()
    {
        if (currentState == GameState.WaitingForPlayers)
        {
            CheckPlayers();
        }
    }

    private void CheckPlayers()
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            Log.Info("Both Players Connected.");
            LoadNextDuelScene();
        }
        else
        {
            Log.Info($"({PhotonNetwork.CurrentRoom.PlayerCount}/2 Players)");
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
    
    public override void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    public override void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == lobbyRoomSceneName)
        {
            Initialize();
        }
        if (scene.name != lobbyRoomSceneName && scene.name != mainMenuSceneName)
        {
            SpawnPlayer();
            SetGameState(GameState.Preparation);
            Log.Info($"Load Level: {scene.name}. Preparation.");
            if (PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(DuelState());
            }
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
        
        if (deadPlayerActorNr == 1) _player2Wins++;
        else _player1Wins++;

        Log.Info($"Points - Player 1: {_player1Wins} | Player 2: {_player2Wins}");

        photonView.RPC("SyncScoreRPC", RpcTarget.All, _player1Wins, _player2Wins);

        if (_player1Wins >= WINS_NEEDED || _player2Wins >= WINS_NEEDED)
        {
            photonView.RPC("EndMatchRPC", RpcTarget.All);
        }
        else
        {
            StartCoroutine(ResetRoundRoutine());
        }
    }

    [PunRPC]
    private void SyncScoreRPC(int p1Wins, int p2Wins)
    {
        _player1Wins = p1Wins;
        _player2Wins = p2Wins;
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
        
        _player1Wins = 0;
        _player2Wins = 0;
        _currentSceneIndex = 0;
        
        PhotonNetwork.LeaveRoom();
    }
    

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Log.Info($"Player {otherPlayer.NickName} disconnected.");
        if (PhotonNetwork.CurrentRoom.PlayerCount < 2) 
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(mainMenuSceneName);
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