using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public Camera mainCamera;
    PlayerInput playerInput;
    Vector2 moveDirection;

    public bool isMoving;
    public float moveSpeed = 5f;
    
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
        transform.Translate(moveSpeed * Time.deltaTime * move, Space.World);
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
        // Vector2 lookDirection = callback.ReadValue<Vector2>();

        // // X -> Y Rotation, Y -> X Rotation
        // Vector3 rotation = new Vector3(-lookDirection.y, lookDirection.x, 0);
        // Quaternion targetRotation = Quaternion.Euler(rotation + transform.eulerAngles);
        // transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);

        // Debug.Log("Look: " + callback.ReadValue<Vector2>());
    }

}
