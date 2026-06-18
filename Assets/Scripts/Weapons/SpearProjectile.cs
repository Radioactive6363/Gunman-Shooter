using UnityEngine;
using Photon.Pun;

public class SpearProjectile : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    private bool _hasHit = false;
    private int _shooterActorNr = -1;
    private Rigidbody rb;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    private void Update()
    {
        if (_hasHit || rb == null) return;
        
        if (rb.velocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.velocity);
        }
    }
    
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] instantiationData = info.photonView.InstantiationData;
        if (instantiationData != null && instantiationData.Length > 0)
        {
            _shooterActorNr = (int)instantiationData[0];
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_hasHit) return;
        
        if (!PhotonNetwork.IsMasterClient) return;
        
        PhotonView targetView = collision.collider.GetComponent<PhotonView>();
        if (targetView != null)
        {
            if (targetView.Owner.ActorNumber == _shooterActorNr)
            {
                return; 
            }
        }
        
        _hasHit = true;
        
        IDamageable target = collision.collider.GetComponent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(); 
        }
        
        PhotonNetwork.Destroy(gameObject);
    }
}