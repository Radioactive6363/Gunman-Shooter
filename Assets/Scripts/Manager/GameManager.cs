using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    public enum GameState { WaitingForPlayers, Preparation, Duel, PostDuel }
    public GameState currentState;

    [Header("Scene Management")]
    public string[] duelScenes = { "DesiertoScene", "SelvaScene", "MinaScene" };
    private int currentSceneIndex = 0;
    
    [Header("Lobby Scenes")]
    public string lobbyRoomSceneName = "LobbyRoomScene"; 
    public string mainMenuSceneName = "MenuScene";

    private int player1Wins = 0;
    private int player2Wins = 0;
    private const int WINS_NEEDED = 2;

    private GameObject localPlayerInstance;

    void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else 
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (SceneManager.GetActiveScene().name == lobbyRoomSceneName)
        {
            SetGameState(GameState.WaitingForPlayers);
            CheckPlayers();
        }
    }

    public void SetGameState(GameState newState)
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

    void CheckPlayers()
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            Log.Info("Both Players Connected.");
            LoadNextDuelScene();
        }
        else
        {
            Log.Info("(1/2 Players)");
        }
    }

    void LoadNextDuelScene()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        if (currentSceneIndex < duelScenes.Length)
        {
            PhotonNetwork.LoadLevel(duelScenes[currentSceneIndex]);
            currentSceneIndex++;
        }
        else
        {
            PhotonNetwork.LoadLevel(duelScenes[0]); 
        }
    }
    
    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != lobbyRoomSceneName && scene.name != mainMenuSceneName)
        {
            SpawnPlayer();
            SetGameState(GameState.Preparation);
            Log.Info($"Load Level: {scene.name}. Preparation.");
        }
    }

    void SpawnPlayer()
    {
        Vector3 spawnPosition = new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f));
        localPlayerInstance = PhotonNetwork.Instantiate("PlayerPrefab", spawnPosition, Quaternion.identity);
    }
    
    public void RegisterKill(int deadPlayerActorNr)
    {
        if (currentState != GameState.Duel) return;
        
        SetGameState(GameState.PostDuel);
        
        if (deadPlayerActorNr == 1) player2Wins++;
        else player1Wins++;

        Log.Info($"Points - Player 1: {player1Wins} | Player 2: {player2Wins}");

        photonView.RPC("SyncScoreRPC", RpcTarget.All, player1Wins, player2Wins);

        if (player1Wins >= WINS_NEEDED || player2Wins >= WINS_NEEDED)
        {
            photonView.RPC("EndMatchRPC", RpcTarget.All);
        }
        else
        {
            StartCoroutine(ResetRoundRoutine());
        }
    }

    [PunRPC]
    void SyncScoreRPC(int p1Wins, int p2Wins)
    {
        player1Wins = p1Wins;
        player2Wins = p2Wins;
    }

    [PunRPC]
    void EndMatchRPC()
    {
        Log.Info("Returning to lobby.");
        StartCoroutine(ReturnToLobbyRoutine());
    }

    IEnumerator ResetRoundRoutine()
    {
        yield return new WaitForSeconds(3f);
        
        if (PhotonNetwork.IsMasterClient)
        {
            LoadNextDuelScene();
        }
    }

    IEnumerator ReturnToLobbyRoutine()
    {
        yield return new WaitForSeconds(5f);
        
        player1Wins = 0;
        player2Wins = 0;
        currentSceneIndex = 0;
        
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
        Destroy(gameObject);
        SceneManager.LoadScene(mainMenuSceneName);
    }
}