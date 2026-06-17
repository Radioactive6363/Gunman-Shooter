using System;
using Photon.Pun;
using UnityEngine;

public class PlayerShooter : MonoBehaviourPun
{
    [SerializeField] private LayerMask shootingLayerMask;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !PhotonNetwork.IsMasterClient)
        {
            RaycastHit hit;
            if(Physics.Raycast(transform.position,transform.TransformDirection(Vector3.forward),out hit, Mathf.Infinity,shootingLayerMask))
            {
                if (hit.collider.gameObject.TryGetComponent<IShootable>(out IShootable shootable))
                {
                    shootable.OnHit();
                }
            }
        }
    }
}

