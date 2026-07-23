using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviourPun
{
    private Animator _animator;
    
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int ShootHash = Animator.StringToHash("Shoot");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int WeaponIndexHash = Animator.StringToHash("WeaponIndex");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        PlayerHealth.OnLocalDeath += HandleDeath;
    }

    private void OnDisable()
    {
        PlayerHealth.OnLocalDeath -= HandleDeath;
    }

    private void Start()
    {
        if (photonView.IsMine)
        {
            SetWeaponAnimationProfile();
        }
    }

    private void Update()
    {
        if (!photonView.IsMine || !DuelRestrictor.CanMove()) return;

        UpdateMovementAnimation();
        HandleShootingInput();
    }

    private void SetWeaponAnimationProfile()
    {
        if (LevelWeaponConfig.Instance != null)
        {
            int currentWeaponInt = (int)LevelWeaponConfig.Instance.allowedWeapon;
            
            _animator.SetInteger(WeaponIndexHash, currentWeaponInt);
            
            Log.Info($"[Animation] WeaponIndex set to {currentWeaponInt}");
        }
    }

    private void UpdateMovementAnimation()
    {
        float x = Input.GetAxis("Horizontal"); 
        float y = Input.GetAxis("Vertical");
        
        if (!DuelRestrictor.CanMoveBackward())
        {
            y = Mathf.Max(0f, y);
        }
        
        Vector2 inputDir = new Vector2(x, y).normalized;
        
        _animator.SetFloat(SpeedHash, inputDir.magnitude);
    }

    private void HandleShootingInput()
    {
        if (Input.GetMouseButtonUp(0))
        {
            _animator.SetTrigger(ShootHash);
        }
    }

    private void HandleDeath()
    {
        if (!photonView.IsMine) return;
        _animator.SetTrigger(DieHash);
        this.enabled = false;
    }
}