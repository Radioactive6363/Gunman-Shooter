using UnityEngine;
using Photon.Pun;

public class LanzaWeapon : MonoBehaviourPun, IWeapon
{
    [Header("Spear Settings")]
    [SerializeField] private string prefabName = "SpearPrefab";
    [SerializeField] private float throwForce = 25f;

    public void Attack(Vector3 origin, Vector3 direction)
    {
        Vector3 spawnPos = origin + (direction * 1.5f);
        
        GameObject projectile = PhotonNetwork.Instantiate(prefabName, spawnPos, Quaternion.LookRotation(direction));
        
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = direction * throwForce;
        }
    }
}