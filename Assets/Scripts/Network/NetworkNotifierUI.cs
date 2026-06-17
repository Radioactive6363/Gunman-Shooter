using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class NetworkNotifierUI : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private GameObject warningPanel;
    [SerializeField] private TextMeshProUGUI warningText;

    [Header("Thresholds")]
    [SerializeField] private int pingWarningThreshold = 200;
    [SerializeField] private int pingCriticalThreshold = 500;
    
    [SerializeField] private Color colorWarning = Color.yellow;
    [SerializeField] private Color colorCritical = Color.red;

    private void Start()
    {
        warningPanel.SetActive(false);
    }

    private void Update()
    {
        if (!PhotonNetwork.IsConnected)
        {
            ShowWarning("Connection Lost!", colorCritical);
            return;
        }
        
        var state = PhotonNetwork.NetworkClientState;
        if (state == ClientState.Disconnecting || state == ClientState.DisconnectingFromMasterServer)
        {
            ShowWarning("Disconnecting...", colorCritical);
            return;
        }

        int ping = PhotonNetwork.GetPing();

        if (ping >= pingCriticalThreshold)
            ShowWarning($"Mc Donald Connection LMAO({ping}ms)", colorCritical);
        else if (ping >= pingWarningThreshold)
            ShowWarning($"Unstable Connection ({ping}ms)", colorWarning);
        else
            HideWarning();
    }
    
    public override void OnDisconnected(DisconnectCause cause)
    {
        ShowWarning($"Connection Lost: {cause} + skill issue", colorCritical);
    }
    
    private void ShowWarning(string message, Color color)
    {
        warningPanel.SetActive(true);
        warningText.text = message;
        warningText.color = color;
    }

    private void HideWarning()
    {
        warningPanel.SetActive(false);
    }
}
