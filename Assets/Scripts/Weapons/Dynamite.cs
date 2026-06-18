using UnityEngine;
using Photon.Pun;

public class Dynamite : MonoBehaviourPun, IWeapon
{
    [Header("Dynamite Settings")]
    [SerializeField] private string prefabName = "DynamitePrefab";
    [SerializeField] private float throwForce = 15f;
    [SerializeField] private float upwardArcForce = 5f;

    public void Attack(Vector3 origin, Vector3 direction)
    {
        photonView.RPC("ThrowDynamiteRPC", RpcTarget.MasterClient, origin, direction);
    }

    [PunRPC]
    private void ThrowDynamiteRPC(Vector3 origin, Vector3 direction, PhotonMessageInfo info)
    {
        Vector3 spawnPos = origin + (direction * 1.5f);
        
        object[] instantiationData = new object[] { info.Sender.ActorNumber };
        
        GameObject projectile = PhotonNetwork.Instantiate(prefabName, spawnPos, Quaternion.identity, 0, instantiationData);
        
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 force = (direction * throwForce) + (Vector3.up * upwardArcForce);
            rb.AddForce(force, ForceMode.Impulse);
        }
    }
}