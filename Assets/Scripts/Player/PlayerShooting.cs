using System;
using UnityEngine;
using Photon.Pun;

public class PlayerShooting : MonoBehaviourPun
{
    [Header("Shooting Properties")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float maxChargeTime = 2f; // Tiempo para el 100%
    
    private float _currentChargeTime = 0f;
    private bool _isCharging = false;
    private bool canShoot = false; 
    
    private IWeapon _currentWeapon;
    public static event Action<float> OnWeaponChargeChanged;

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
        if (!canShoot || _currentWeapon == null) return;
        
        if (Input.GetMouseButtonDown(0))
        {
            _isCharging = true;
            _currentChargeTime = 0f;
        }
        
        if (Input.GetMouseButton(0) && _isCharging)
        {
            _currentChargeTime += Time.deltaTime;
            _currentChargeTime = Mathf.Clamp(_currentChargeTime, 0f, maxChargeTime);
            
            float normalizedCharge = _currentChargeTime / maxChargeTime;
            OnWeaponChargeChanged?.Invoke(normalizedCharge); 
        }
        
        if (Input.GetMouseButtonUp(0) && _isCharging)
        {
            _isCharging = false;
            float finalCharge = _currentChargeTime / maxChargeTime;
            
            _currentWeapon.Attack(cameraTransform.position, cameraTransform.forward, finalCharge);
            
            OnWeaponChargeChanged?.Invoke(0f); 
        }
    }
}