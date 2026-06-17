using UnityEngine;
using TMPro;

public class MenuButtonsScript : MonoBehaviour
{
    public TMP_InputField RoomField;

    public void OnCreateRoomClick()
    {
        string inputText = RoomField.text;
        PhotonManager.Instance.CreateRoom(inputText);
    }

    public void OnJoinByNameClick()
    {
        string inputText = RoomField.text;
        PhotonManager.Instance.JoinByNameRoom(inputText);
    }

    //public void OnCreateRoomClick()
    //{
    //    string inputText = createRoomField.text;
    //    PhotonManager.instance.CreateRoom(inputText);
    //}
}
