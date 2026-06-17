using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject roomContainer;
    [SerializeField] private GameObject roomPrefab;
    private List<GameObject> _roomList = new List<GameObject>();
    
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            if (!info.RemovedFromList && info.IsVisible && !_roomList.Exists(room => room.GetComponent<RoomLobbyData>().RoomInfoLobby.Name == info.Name))
            {
                GameObject room = Instantiate(roomPrefab, roomContainer.transform);
                _roomList.Add(room);
                room.GetComponent<RoomLobbyData>().CreateRoom(info);
            }
            else
            {
                if (info.RemovedFromList)
                {
                    GameObject RoomToRemove = _roomList.Find(r => r.GetComponent<RoomLobbyData>().RoomInfoLobby.Name == info.Name);
                    if (RoomToRemove != null)
                    {
                        _roomList.Remove(RoomToRemove);
                        Destroy(RoomToRemove);
                    }
                }
            }
        }
    }
}
