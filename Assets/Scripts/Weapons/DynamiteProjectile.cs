using UnityEngine;
using Photon.Pun;
using System.Collections;

public class DynamiteProjectile : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    [Header("Explosion Settings")]
    [SerializeField] private float explosionDelay = 3f;
    [SerializeField] private float explosionRadius = 6f;
    [SerializeField] private LayerMask obstacleLayer;

    private int _throwerActorNr = -1;

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] instantiationData = info.photonView.InstantiationData;
        if (instantiationData != null && instantiationData.Length > 0)
        {
            _throwerActorNr = (int)instantiationData[0];
        }
    }

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
                Vector3 targetCenter = hitCollider.bounds.center;

                if (!Physics.Linecast(transform.position, targetCenter, out RaycastHit hit, obstacleLayer))
                {
                    target.TakeDamage();
                }
                else
                {
                    if (hit.collider == hitCollider)
                    {
                        target.TakeDamage();
                    }
                }
            }
        }

        photonView.RPC("PlayExplosionEffectsRPC", RpcTarget.All);
        
        PhotonNetwork.Destroy(gameObject);
    }

    [PunRPC]
    private void PlayExplosionEffectsRPC()
    {
        Log.Info($"{gameObject.name} exploded at {transform.position}");
        // TODO: Instanciar prefab de humo/fuego aquí (sin PhotonNetwork.Instantiate, solo Instantiate local)
    }
}