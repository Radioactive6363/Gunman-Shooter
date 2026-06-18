using UnityEngine;

public class LevelWeaponConfig : MonoBehaviour
{
    public static LevelWeaponConfig Instance;
    
    [Header("Weapon Setting")]
    public WeaponType allowedWeapon;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}