using UnityEngine;

public interface IWeapon
{
    void Attack(Vector3 origin, Vector3 direction, float chargePercentage);
}