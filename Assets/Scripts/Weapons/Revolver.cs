using UnityEngine;
using Photon.Pun;
using System.Collections;

public class Revolver : MonoBehaviourPun, IWeapon
{
    [Header("Revolver Settings")]
    [SerializeField] private float range = 100f;
    
    [Header("Visual Settings")]
    [SerializeField] private float tracerDuration = 0.5f;
    [SerializeField] private Material tracerMaterial;

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
        
        Vector3 visualEndPoint = origin + (finalDirection * range);
        
        if (Physics.Raycast(origin, finalDirection, out RaycastHit hit, range))
        {
            visualEndPoint = hit.point;

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
        
        photonView.RPC("DrawTracerRPC", RpcTarget.All, origin, visualEndPoint);
    }

    [PunRPC]
    private void DrawTracerRPC(Vector3 startPoint, Vector3 endPoint)
    {
        StartCoroutine(SpawnTemporaryTracer(startPoint, endPoint));
    }

    private IEnumerator SpawnTemporaryTracer(Vector3 startPoint, Vector3 endPoint)
    {
        GameObject tracerObj = new GameObject("RevolverTracerTemp");
        LineRenderer lr = tracerObj.AddComponent<LineRenderer>();
        
        lr.positionCount = 2;
        lr.SetPosition(0, startPoint);
        lr.SetPosition(1, endPoint);
        lr.startWidth = 0.02f;
        lr.endWidth = 0.02f;
        
        if (tracerMaterial != null)
        {
            lr.material = tracerMaterial;
        }
        
        yield return new WaitForSeconds(tracerDuration);
        
        Destroy(tracerObj);
    }
}