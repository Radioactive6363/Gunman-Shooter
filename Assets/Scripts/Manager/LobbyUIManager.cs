using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class LobbyUIManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elements - Ready Button")]
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyButtonText;
    
    [Header("UI Elements - Room Info")]
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TextMeshProUGUI playerListText;
    
    [Header("Colors")]
    [SerializeField] private Color notReadyColor = Color.white;
    [SerializeField] private Color readyColor = Color.green;

    private void Start()
    {
        if (readyButton != null)
        {
            readyButton.onClick.AddListener(OnReadyButtonClicked);
        }
        
        UpdatePlayerList();
    }

    private void OnReadyButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ToggleReady();
        }
    }
    
    private void UpdatePlayerList()
    {
        if (!PhotonNetwork.InRoom) return;
        
        if (playerCountText != null)
        {
            playerCountText.text = $"Players on Lobby: {PhotonNetwork.CurrentRoom.PlayerCount} / {PhotonNetwork.CurrentRoom.MaxPlayers}";
        }
        
        if (playerListText != null)
        {
            playerListText.text = "";
            
            foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerList)
            {
                bool isReady = false;
                
                if (p.CustomProperties.TryGetValue("IsReady", out object readyObj))
                {
                    isReady = (bool)readyObj;
                }
                
                string statusText = isReady ? "Ready" : "Waiting";
                string colorHex = ColorUtility.ToHtmlStringRGB(isReady ? readyColor : notReadyColor);
                
                playerListText.text += $"<color=#{colorHex}>- {p.NickName} {statusText}</color>\n";
            }
        }
    }
    
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("IsReady"))
        {
            UpdatePlayerList();
        }
        
        if (Equals(targetPlayer, PhotonNetwork.LocalPlayer) && changedProps.ContainsKey("IsReady"))
        {
            bool isReady = (bool)changedProps["IsReady"];
            
            if (readyButtonText != null)
                readyButtonText.text = isReady ? "Cancel" : "Ready";
                
            if (readyButton != null)
                readyButton.image.color = isReady ? readyColor : notReadyColor;
        }
    }
}