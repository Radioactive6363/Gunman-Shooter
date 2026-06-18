using UnityEngine;
using Photon.Pun;

public class Spear : MonoBehaviourPun, IWeapon
{
    [Header("Spear Settings")]
    [SerializeField] private string prefabName = "SpearPrefab";
    [SerializeField] private float throwForce = 25f;

    public void Attack(Vector3 origin, Vector3 direction)
    {
        photonView.RPC("ThrowSpearRPC", RpcTarget.MasterClient, origin, direction);
    }

    [PunRPC]
    private void ThrowSpearRPC(Vector3 origin, Vector3 direction, PhotonMessageInfo info)
    {
        Vector3 spawnPos = origin + (direction * 1.5f);
        
        object[] instantiationData = new object[] { info.Sender.ActorNumber };
        
        GameObject projectile = PhotonNetwork.Instantiate(prefabName, spawnPos, Quaternion.LookRotation(direction), 0, instantiationData);
        
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = direction * throwForce;
        }
    }
}