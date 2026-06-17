using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class RoomLobbyData : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI playerAmount;
    public RoomInfo RoomInfoLobby { get; private set; }


    public void CreateRoom(RoomInfo info)
    {
        RoomInfoLobby = info;
        nameText.text = info.Name;
        string maxPlayers = info.MaxPlayers.ToString();
        playerAmount.text = info.PlayerCount + "/" + maxPlayers;
    }

    public void JoinRoom()
    {
        PhotonManager.Instance.JoinBySearchRoom(RoomInfoLobby.Name);
    }
}
