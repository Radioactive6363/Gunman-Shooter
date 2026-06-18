using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class LobbyUIManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyButtonText;
    
    [Header("Colors")]
    [SerializeField] private Color notReadyColor = Color.white;
    [SerializeField] private Color readyColor = Color.green;

    private void Start()
    {
        if (readyButton != null)
        {
            readyButton.onClick.AddListener(OnReadyButtonClicked);
        }
    }

    private void OnReadyButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ToggleReady();
        }
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (targetPlayer == PhotonNetwork.LocalPlayer && changedProps.ContainsKey("IsReady"))
        {
            bool isReady = (bool)changedProps["IsReady"];
            
            if (readyButtonText != null)
                readyButtonText.text = isReady ? "Cancel" : "Ready";
                
            if (readyButton != null)
                readyButton.image.color = isReady ? readyColor : notReadyColor;
        }
    }
}