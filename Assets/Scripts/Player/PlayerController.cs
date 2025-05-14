using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public Camera mainCamera;
    PlayerInput playerInput;
    Vector2 moveDirection;

    public bool isMoving;
    public float moveSpeed = 5f;

    public float lookSpeed = 1.0f;
    public float minPitch = -85f, maxPitch = 85f;


    void Start()
    {
        playerInput = GetComponent<PlayerInput>();

        playerInput.actions.FindAction("Move").performed += OnMovePerformed;
        playerInput.actions.FindAction("Move").canceled += OnMoveCanceled;
        playerInput.actions.FindAction("Look").performed += OnLook;

    }

    void Update()
    {
        if (!isMoving) return;
        Vector3 move = new Vector3(moveDirection.x, 0, moveDirection.y).normalized;
        transform.Translate(moveSpeed * Time.deltaTime * move);
    }

    void OnMovePerformed(InputAction.CallbackContext callback)
    {
        moveDirection = callback.ReadValue<Vector2>();
        // Debug.Log("Move: " + moveDirection);
        isMoving = moveDirection != Vector2.zero;
    }

    void OnMoveCanceled(InputAction.CallbackContext callback)
    {
        moveDirection = Vector2.zero;
        isMoving = false;
    }

    void OnLook(InputAction.CallbackContext callback)
    {
        Vector2 targetLook = callback.ReadValue<Vector2>();
        // 스무딩 적용
        
        float xRotation = Mathf.Clamp(targetLook.y, minPitch, maxPitch);
        float yRotation = targetLook.x;

        // 카메라 회전
        mainCamera.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);

        // 플레이어 회전
        transform.rotation = Quaternion.Euler(0, yRotation, 0);
        

    }

}
