using UnityEngine;
using Photon.Pun;

public class DirectExit : MonoBehaviourPun
{
    private void Update()
    {
        if (!photonView.IsMine) return;

        // Detectar la tecla Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Log.Info("Escape presionado. Saliendo directamente de la partida...");
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            PhotonNetwork.LeaveRoom(); 
        }
    }
}