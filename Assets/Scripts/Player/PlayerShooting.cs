using UnityEngine;
using Photon.Pun;

public class PlayerShooting : MonoBehaviourPun
{
    public Transform cameraTransform;
    private IWeapon currentWeapon;

    private void Start()
    {
        currentWeapon = GetComponent<IWeapon>();
    }

    public void EquipWeapon(IWeapon newWeapon)
    {
        currentWeapon = newWeapon;
    }

    private void Update()
    {
        if (!photonView.IsMine) return;
        
        if (GameManager.Instance.currentState != GameState.Duel) return;

        if (Input.GetMouseButtonDown(0) && currentWeapon != null) 
        {
            currentWeapon.Attack(cameraTransform.position, cameraTransform.forward);
        }
    }
}