using System;
using Photon.Pun;

public class PlayerHealth : MonoBehaviourPun, IDamageable 
{
    private bool isDead = false;
    private PlayerController _controller;
    private PlayerShooting _shooting;
    
    public event Action OnDeath;
    public static event Action OnLocalDeath;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _shooting = GetComponent<PlayerShooting>();
    }

    public void TakeDamage()
    {
        photonView.RPC("TakeDamageRPC", RpcTarget.All);
    }

    [PunRPC]
    public void TakeDamageRPC()
    {
        if (isDead) return;
        Die();
    }

    private void Die()
    {
        isDead = true;
    
        _controller.enabled = false;
        _shooting.enabled = false;
    
        OnDeath?.Invoke();
        
        if (photonView.IsMine)
        {
            OnLocalDeath?.Invoke();
        }
    
        Log.Info($"Player {photonView.Owner.NickName} has died.");
    
        if (PhotonNetwork.IsMasterClient)
        {
            GameManager.Instance.RegisterPlayerDeath(photonView.OwnerActorNr);
        }
    }
}