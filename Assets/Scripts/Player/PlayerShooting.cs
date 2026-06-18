using UnityEngine;
using Photon.Pun;

public class PlayerShooting : MonoBehaviourPun
{
    [SerializeField] private Transform cameraTransform;
    private IWeapon _currentWeapon;
    private bool canShoot = false; 

    private void Start()
    {
        AssignWeaponBasedOnScene();
    }

    private void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState newState)
    {
        canShoot = (newState == GameState.Duel);
    }

    private void AssignWeaponBasedOnScene()
    {
        if (LevelWeaponConfig.Instance == null)
        {
            Log.Warning("No weapon config found. Adding Revolver as Default.");
            _currentWeapon = gameObject.AddComponent<Revolver>();
            return;
        }
        
        switch (LevelWeaponConfig.Instance.allowedWeapon)
        {
            case WeaponType.Revolver:
                _currentWeapon = gameObject.AddComponent<Revolver>(); 
                break;
            case WeaponType.Spear:
                _currentWeapon = gameObject.AddComponent<Spear>();
                break;
            case WeaponType.Dynamite:
                _currentWeapon = gameObject.AddComponent<Dynamite>();
                break;
        }
        
        if (_currentWeapon == null)
        {
            Log.Error($"[{gameObject.name}] {LevelWeaponConfig.Instance.allowedWeapon} could not be added.");
        }
        else
        {
            Log.Info($"Weapon Dynamically Added & Equipped: {_currentWeapon.GetType().Name}");
        }
    }

    private void Update()
    {
        if (!photonView.IsMine) return;
        
        if (!canShoot) return;
        
        if (Input.GetMouseButtonDown(0) && _currentWeapon != null) 
        {
            _currentWeapon.Attack(cameraTransform.position, cameraTransform.forward);
        }
    }
}