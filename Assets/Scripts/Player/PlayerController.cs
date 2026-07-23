using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPun
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private Transform cameraTransform;
    
    [Header("Local UI")]
    [SerializeField] private GameObject localUiPrefab;
    
    [Header("Aiming Settings")]
    [SerializeField] private float aimingSpeedMultiplier = 0.4f;
    
    [Header("Camera Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float baseFOV = 60f;
    [SerializeField] private float aimFOV = 40f;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    
    private float originalSpeed;
    
    private float xRotation = 0f;
    
    private void OnEnable()
    {
        PlayerShooting.OnWeaponChargeChanged += HandleChargeEffects;
    }

    private void OnDisable()
    {
        PlayerShooting.OnWeaponChargeChanged -= HandleChargeEffects;
    }

    private void Start()
    {
        if (!photonView.IsMine)
        {
            cameraTransform.gameObject.SetActive(false);
            return;
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        originalSpeed = speed;
        
        if (playerCamera == null) playerCamera = cameraTransform.GetComponent<Camera>();
        
        if (playerCamera != null) playerCamera.fieldOfView = baseFOV;

        if (localUiPrefab != null) Instantiate(localUiPrefab);
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        HandleMovement();
        HandleLook();

        if (animator != null)
        {
            UpdateAnimations();
        }
    }
    
    private void HandleChargeEffects(float chargePercentage)
    {
        if (!photonView.IsMine) return; 
        
        float targetSpeed = originalSpeed * aimingSpeedMultiplier;
        speed = Mathf.Lerp(originalSpeed, targetSpeed, chargePercentage);

        if (playerCamera != null)
        {
            playerCamera.fieldOfView = Mathf.Lerp(baseFOV, aimFOV, chargePercentage);
        }
    }

    private void HandleMovement()
    {
        if (!DuelRestrictor.CanMove()) return;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        if (!DuelRestrictor.CanMoveBackward())
            z = Mathf.Max(0f, z);

        Vector3 move = transform.right * x + transform.forward * z;
        transform.position += move * speed * Time.deltaTime;
    }

    private void HandleLook()
    {
        if (!DuelRestrictor.CanRotateCamera()) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
    
    private void UpdateAnimations()
    {
        float moveMagnitude = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).magnitude;
        animator.SetFloat("Speed", moveMagnitude);
    }
}