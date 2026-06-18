using Photon.Pun;

public class PlayerHealth : MonoBehaviourPun, IDamageable 
{
    private bool isDead = false;
    
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
        
        GetComponent<PlayerController>().enabled = false;
        GetComponent<PlayerShooting>().enabled = false;
        
        Log.Info($"Player {photonView.Owner.NickName} has died.");
        
        if (PhotonNetwork.IsMasterClient)
        {
            GameManager.Instance.RegisterKill(photonView.OwnerActorNr);
        }
    }
}