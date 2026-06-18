using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPun
{
    public float speed = 5f;
    public float mouseSensitivity = 100f;
    public Transform cameraTransform;
    
    private float xRotation = 0f;

    private void Start()
    {
        if (!photonView.IsMine)
        {
            cameraTransform.gameObject.SetActive(false);
            return;
        }
        
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        HandleMovement();
        HandleLook();
    }

    private void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        transform.position += move * speed * Time.deltaTime;
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
}