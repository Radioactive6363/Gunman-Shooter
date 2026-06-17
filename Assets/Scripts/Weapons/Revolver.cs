using UnityEngine;
using Photon.Pun;

public class Revolver : MonoBehaviourPun, IWeapon
{
    [Header("Revolver Settings")]
    [SerializeField] private float range = 100f;

    public void Attack(Vector3 origin, Vector3 direction)
    {
        photonView.RPC("FireRevolverRPC", RpcTarget.MasterClient, origin, direction);
    }

    [PunRPC]
    private void FireRevolverRPC(Vector3 origin, Vector3 direction)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, range))
        {
            IDamageable target = hit.collider.GetComponent<IDamageable>();
            
            if (target != null)
            {
                target.TakeDamage();
            }
        }
    }
}