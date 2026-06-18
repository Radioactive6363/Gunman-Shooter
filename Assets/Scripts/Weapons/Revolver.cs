using UnityEngine;
using Photon.Pun;

public class Revolver : MonoBehaviourPun, IWeapon
{
    [Header("Revolver Settings")]
    [SerializeField] private float range = 100f;

    public void Attack(Vector3 origin, Vector3 direction, float chargePercentage)
    {
        photonView.RPC("FireRevolverRPC", RpcTarget.MasterClient, origin, direction, chargePercentage);
    }

    [PunRPC]
    private void FireRevolverRPC(Vector3 origin, Vector3 direction, float chargePercentage, PhotonMessageInfo info)
    {
        int shooterActorNr = info.Sender.ActorNumber;
        
        float currentSpread = Mathf.Lerp(3f, 0f, chargePercentage);
        
        Vector3 randomSpread = UnityEngine.Random.insideUnitSphere * currentSpread;
        Vector3 finalDirection = (direction * range + randomSpread).normalized;
        
        if (Physics.Raycast(origin, finalDirection, out RaycastHit hit, range))
        {
            PhotonView targetView = hit.collider.GetComponent<PhotonView>();
            
            if (targetView != null && targetView.OwnerActorNr == shooterActorNr)
            {
                return;
            }

            IDamageable target = hit.collider.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage();
            }
        }
    }
}