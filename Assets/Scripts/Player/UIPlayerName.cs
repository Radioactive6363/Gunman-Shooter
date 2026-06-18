using Photon.Pun;
using TMPro;
using UnityEngine;

public class UIPlayerName : MonoBehaviourPun
{
    [SerializeField] private TMP_Text playerName;

    private void Start()
    {
        playerName.text = photonView.Owner.NickName;
    }
}