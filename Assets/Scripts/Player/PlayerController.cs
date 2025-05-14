using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public Camera mainCamera;
    PlayerInput playerInput;
    Vector2 moveDirection;

    public bool isMoving;
    // 이동 속도
    public float moveSpeed = 5f;

    // 마우스 민감도
    public float mouseSensitivity = 2.0f;

    // 카메라 상하 회전 제한 각도
    public float pitchMin = -85f;
    public float pitchMax = 85f;

    // 현재 카메라의 X축 회전값 (Pitch)
    private float currentPitch = 0.0f;

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();

        playerInput.actions.FindAction("Move").performed += OnMovePerformed;
        playerInput.actions.FindAction("Move").canceled += OnMoveCanceled;
        playerInput.actions.FindAction("Look").performed += OnLook;

        // 게임 시작 시 커서 잠금 및 숨김
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!isMoving) return;
        Vector3 move = new Vector3(moveDirection.x, 0, moveDirection.y).normalized;

        transform.Translate(moveSpeed * Time.deltaTime * move, Space.Self);
    }

    void OnMovePerformed(InputAction.CallbackContext callback)
    {
        moveDirection = callback.ReadValue<Vector2>();
        isMoving = moveDirection != Vector2.zero;
    }

    void OnMoveCanceled(InputAction.CallbackContext callback)
    {
        moveDirection = Vector2.zero;
        isMoving = false;
    }

    void OnLook(InputAction.CallbackContext callback)
    {
        Vector2 mouseDelta = callback.ReadValue<Vector2>();

        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        // 플레이어 좌우 회전 (Yaw)
        transform.Rotate(Vector3.up * mouseX);

        // 카메라 상하 회전 (Pitch)
        currentPitch -= mouseY; 
        currentPitch = Mathf.Clamp(currentPitch, pitchMin, pitchMax); // 상하 회전 각도 제한

        // mainCamera.transform.localEulerAngles = new Vector3(currentPitch, 0, 0);
        mainCamera.transform.localRotation = Quaternion.Euler(currentPitch, 0, 0);
    }

    // 게임 플레이 중단 또는 메뉴 호출 시 커서 잠금 해제
    // void OnDisable()
    // {
    //     Cursor.lockState = CursorLockMode.None;
    //     Cursor.visible = true;
    // }
}