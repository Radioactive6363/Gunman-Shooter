using UnityEngine;
using Photon.Pun;
using Photon.Voice.PUN;
using Photon.Voice.Unity;

public class LobbyVoiceBridge : MonoBehaviour
{
    [SerializeField] private PunVoiceClient voiceClient;
    [SerializeField] private Recorder recorder;

    private void Start()
    {
        if (voiceClient == null) voiceClient = GetComponent<PunVoiceClient>();
        if (recorder == null) recorder = GetComponent<Recorder>();
        
        if (voiceClient != null && recorder != null)
        {
            voiceClient.PrimaryRecorder = recorder;
            
            if (!recorder.TransmitEnabled)
            {
                recorder.TransmitEnabled = true;
            }
            Log.Info("[LobbyVoice] Bridge Stablished.");
        }
    }

    /*private void OnEnable()
    {
        PhotonNetwork.NetworkingClient.EventReceived += NetworkingClient_EventReceived;
    }

    private void OnDisable()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= NetworkingClient_EventReceived;
    }

    private void NetworkingClient_EventReceived(ExitGames.Client.Photon.EventData obj)
    {
        
    }*/
}