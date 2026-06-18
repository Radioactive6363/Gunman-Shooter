using UnityEngine;
using Photon.Pun;

public class SpearProjectile : MonoBehaviourPun
{
    private bool hasHit = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (!PhotonNetwork.IsMasterClient || hasHit) return;

        hasHit = true;

        IDamageable target = collision.collider.GetComponent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage();
        }
        
        PhotonNetwork.Destroy(gameObject);
    }
}