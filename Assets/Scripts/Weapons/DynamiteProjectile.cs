using UnityEngine;
using Photon.Pun;
using System.Collections;

public class DynamiteProjectile : MonoBehaviourPun
{
    [Header("Explosion Settings")]
    [SerializeField] private float explosionDelay = 3f;
    [SerializeField] private float explosionRadius = 6f;

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(ExplodeRoutine());
        }
    }

    private IEnumerator ExplodeRoutine()
    {
        yield return new WaitForSeconds(explosionDelay);
        
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        
        foreach (var hitCollider in hitColliders)
        {
            IDamageable target = hitCollider.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage();
            }
        }
        
        photonView.RPC("PlayExplosionEffectsRPC", RpcTarget.All);
        
        PhotonNetwork.Destroy(gameObject);
    }

    [PunRPC]
    private void PlayExplosionEffectsRPC()
    {
        Log.Info($"{gameObject} exploded");
    }
}