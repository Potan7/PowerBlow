using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    #region Variables
    public Transform head;  // 머리 카메라 트랜스폼
    public Transform cameraTransform; // 카메라 트랜스폼
    public CinemachineCamera cinemachineCamera;

    Vector2 moveDirection; // 현재 프레임의 이동 입력 값

    MyInputActions playerInput;
    CharacterController characterController;
    public PlayerAnimator playerAnimator; // PlayerAnimator 참조

    private PlayerState currentState;
    public PlayerState CurrentState
    {
        get { return currentState; }
        set
        {
            if (currentState != value)
            {
                currentState = value;
                // 슬라이딩 상태일 때만 머리 위치로 카메라 변경
                if (currentState == PlayerState.Sliding)
                {
                    ChangeViewToHead(true);
                }
                else
                {
                    ChangeViewToHead(false);
                }
            }
        }
    }

    // --- 내부 요청 플래그 ---
    private bool _jumpRequested = false;
    private bool _crouchActive = false; // 슬라이드 버튼 토글 상태 (이전의 앉기 버튼)

    // --- 슬라이딩 및 이동 관련 변수 ---
    public Vector3 slidingState; // 슬라이딩 중일 때의 월드 속도 벡터
    public float moveSpeed = 5f;
    public float jumpPower = 5.0f;
    public float slideInitialSpeedMultiplier = 1.5f; // 슬라이딩 시작 시 속도 배율 (moveSpeed * 이 값)
    public float slideDeceleration = 2.0f; // 슬라이딩 감속량 (값이 클수록 빨리 멈춤)
    public float slidingColliderHeight = 0.5f; // 슬라이딩 시 캐릭터 컨트롤러 높이
    private float standingColliderHeight; // 서 있을 때 캐릭터 컨트롤러 높이
    private float standingColliderCenterY; // 서 있을 때 캐릭터 컨트롤러 중심 Y 좌표


    // --- 마우스 및 카메라 ---
    public float mouseSensitivity = 2.0f;
    public float pitchMin = -85f;
    public float pitchMax = 85f;
    private float currentPitch = 0.0f;

    // --- 수직 속도 ---
    private float _verticalVelocity;
    #endregion

    #region Unity Methods

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        standingColliderHeight = characterController.height; // 서 있을 때 캐릭터 컨트롤러 높이 저장
        standingColliderCenterY = characterController.center.y; // 서 있을 때 캐릭터 컨트롤러 중심 Y 좌표 저장

        playerInput = new MyInputActions();

        playerInput.Player.Move.performed += OnMove;
        playerInput.Player.Move.canceled += OnMove;
        playerInput.Player.Look.performed += OnLook;
        playerInput.Player.Jump.performed += OnJump;
        playerInput.Player.Crouch.performed += OnCrouch; // Crouch 입력은 슬라이드로 사용

        playerInput.Enable();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        CurrentState = PlayerState.Idle;
        // 초기 애니메이터 상태 설정 (PlayerAnimator 사용)
        playerAnimator.SetAnim(PlayerState.Moving, false);
        playerAnimator.SetAnim(PlayerState.Falling, false);
        playerAnimator.SetAnim(PlayerState.Sliding, false);
    }

    void Update()
    {
        bool isGrounded = characterController.isGrounded;
        bool hasMoveInput = moveDirection != Vector2.zero;

        // --- 상태 전환 로직 ---
        // 1. 공중 상태 처리
        if (!isGrounded)
        {
            if (CurrentState != PlayerState.Jumping && CurrentState != PlayerState.Falling)
            {
                CurrentState = PlayerState.Falling;
            }
            _verticalVelocity += Physics.gravity.y * Time.deltaTime;
            if (slidingState.magnitude > 0) slidingState = Vector3.zero; // 공중에서는 슬라이딩 중단
            _crouchActive = false; // 공중에서는 슬라이드 시도 비활성화
            // TODO - 구르기 기능 추가할거면 위 줄 지우고 구르기 기능 추가하기
        }
        // 2. 지상 상태 처리
        else
        {
            if (CurrentState == PlayerState.Falling || CurrentState == PlayerState.Jumping) // 착지
            {
                _verticalVelocity = -2f;
                if (_crouchActive) // 착지 시 슬라이드 버튼이 눌려있었다면
                {
                    if (hasMoveInput)
                    {
                        CurrentState = PlayerState.Sliding;
                        Vector3 worldMoveDir = transform.TransformDirection(new Vector3(moveDirection.x, 0, moveDirection.y)).normalized;
                        slidingState = worldMoveDir * moveSpeed * slideInitialSpeedMultiplier;
                    }
                    else
                    {
                        CurrentState = PlayerState.Idle;
                        _crouchActive = false; // 이동 입력 없으면 슬라이드 활성화 해제
                    }
                }
                else
                {
                    CurrentState = hasMoveInput ? PlayerState.Moving : PlayerState.Idle;
                }
            }

            if (_jumpRequested)
            {
                _verticalVelocity = jumpPower;
                CurrentState = PlayerState.Jumping;
                _crouchActive = false; // 점프 시 슬라이드 활성화 해제
                slidingState = Vector3.zero;
                _jumpRequested = false;
                playerAnimator.SetAnim(PlayerState.Jumping);
            }
            else if (_crouchActive) // 슬라이드 버튼이 활성화 되어 있다면
            {
                // 현재 슬라이딩 상태가 아니라면 슬라이딩 시작 시도
                if (CurrentState != PlayerState.Sliding)
                {
                    if (hasMoveInput && (CurrentState == PlayerState.Moving || CurrentState == PlayerState.Idle))
                    {
                        CurrentState = PlayerState.Sliding;
                        Vector3 worldMoveDir = transform.TransformDirection(new Vector3(moveDirection.x, 0, moveDirection.y)).normalized;
                        slidingState = worldMoveDir * moveSpeed * slideInitialSpeedMultiplier;
                    }
                    else // 이동 입력 없이 슬라이드 시도 시 Idle 상태로 만들고 슬라이드 비활성화
                    {
                        CurrentState = PlayerState.Idle;
                        _crouchActive = false;
                    }
                }
                else // 현재 슬라이딩 중이라면 감속 처리
                {
                    if (slidingState.magnitude > 0.1f)
                    {
                        slidingState -= slidingState.normalized * slideDeceleration * Time.deltaTime;
                        if (slidingState.magnitude < 0.1f) slidingState = Vector3.zero;
                    }
                    // 슬라이딩 종료
                    if (slidingState.magnitude <= 0.1f)
                    {
                        CurrentState = hasMoveInput ? PlayerState.Moving : PlayerState.Idle;
                        _crouchActive = false; // 슬라이드 액션 완료
                    }
                }
            }
            else // 슬라이드 버튼이 비활성화 되어 있다면 (예: 슬라이드 중 버튼을 떼거나, 일반 상태)
            {
                if (CurrentState == PlayerState.Sliding) // 슬라이드 중에 버튼을 떼면 즉시 일반 상태로
                {
                    CurrentState = hasMoveInput ? PlayerState.Moving : PlayerState.Idle;
                    slidingState = Vector3.zero;
                }
                else if (hasMoveInput && CurrentState == PlayerState.Idle)
                {
                    CurrentState = PlayerState.Moving;
                }
                else if (!hasMoveInput && CurrentState == PlayerState.Moving)
                {
                    CurrentState = PlayerState.Idle;
                }
            }
        }

        // --- 이동 실행 로직 ---
        Vector3 horizontalDisplacement = Vector3.zero;
        float currentEffectiveSpeed = moveSpeed;

        switch (CurrentState)
        {
            case PlayerState.Idle:
                // Idle 상태에서도 이동 입력이 들어오면 바로 움직일 수 있도록 처리 (상태 전환이 한 프레임 늦을 수 있으므로)
                if (hasMoveInput && isGrounded)
                {
                    Vector3 worldMoveDir = transform.TransformDirection(new Vector3(moveDirection.x, 0, moveDirection.y)).normalized;
                    horizontalDisplacement = worldMoveDir * currentEffectiveSpeed * Time.deltaTime;
                }
                break;
            case PlayerState.Moving:
                if (hasMoveInput)
                {
                    Vector3 worldMoveDir = transform.TransformDirection(new Vector3(moveDirection.x, 0, moveDirection.y)).normalized;
                    horizontalDisplacement = worldMoveDir * currentEffectiveSpeed * Time.deltaTime;
                }
                break;
            case PlayerState.Sliding:
                horizontalDisplacement = slidingState * Time.deltaTime; // slidingState는 이미 월드 속도 벡터
                break;
            case PlayerState.Jumping:
            case PlayerState.Falling:
                if (hasMoveInput) // 공중 제어
                {
                    Vector3 worldMoveDir = transform.TransformDirection(new Vector3(moveDirection.x, 0, moveDirection.y)).normalized;
                    horizontalDisplacement = worldMoveDir * currentEffectiveSpeed * Time.deltaTime;
                }
                break;
        }

        Vector3 verticalDisplacement = _verticalVelocity * Time.deltaTime * Vector3.up;
        characterController.Move(horizontalDisplacement + verticalDisplacement);

        // --- 애니메이터 업데이트 (PlayerAnimator 사용) ---
        bool isActuallyMovingOnGround = hasMoveInput && (CurrentState == PlayerState.Moving || CurrentState == PlayerState.Idle) && isGrounded;
        playerAnimator.SetAnim(PlayerState.Moving, isActuallyMovingOnGround);

        playerAnimator.SetAnim(PlayerState.Falling, !isGrounded);

        playerAnimator.SetAnim(PlayerState.Sliding, CurrentState == PlayerState.Sliding);

        playerAnimator.SetDirection(moveDirection); // 애니메이터에 이동 방향 전달
    }

    #endregion

    #region Input Callbacks

    void OnMove(InputAction.CallbackContext callback)
    {
        if (callback.performed)
        {
            moveDirection = callback.ReadValue<Vector2>();
        }
        else if (callback.canceled)
        {
            moveDirection = Vector2.zero;
        }
    }

    void OnLook(InputAction.CallbackContext callback)
    {
        Vector2 mouseDelta = callback.ReadValue<Vector2>();
        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        currentPitch -= mouseY;
        currentPitch = Mathf.Clamp(currentPitch, pitchMin, pitchMax);
        head.localRotation = Quaternion.Euler(currentPitch, head.localEulerAngles.y, head.localEulerAngles.z);
        cameraTransform.localRotation = Quaternion.Euler(currentPitch, cameraTransform.localEulerAngles.y, cameraTransform.localEulerAngles.z);
    }

    void OnJump(InputAction.CallbackContext callback)
    {
        if (callback.performed && characterController.isGrounded)
        {
            // 슬라이딩 중에도 점프 가능하도록 수정
            if (CurrentState == PlayerState.Idle || CurrentState == PlayerState.Moving || CurrentState == PlayerState.Sliding)
            {
                _jumpRequested = true;
            }
        }
    }

    void OnCrouch(InputAction.CallbackContext callback) // 이 입력은 이제 "슬라이드" 버튼으로 작동
    {
        if (callback.performed)
        {
            _crouchActive = !_crouchActive; // 슬라이드 활성화 상태 토글

            // 공중에서는 _crouchActive 상태만 변경하고 실제 상태 변경은 착지 시 Update에서 처리
            if (!characterController.isGrounded)
            {
                return;
            }

            // 지상에서 슬라이드 버튼을 눌러 _crouchActive가 true가 된 경우,
            // 실제 슬라이딩 시작은 Update 루프에서 hasMoveInput과 현재 상태를 보고 결정.
            // 지상에서 슬라이드 버튼을 떼서 _crouchActive가 false가 된 경우,
            // Update 루프에서 CurrentState == PlayerState.Sliding 이면 Moving/Idle로 전환.
        }
    }

    #endregion

    #region Methods

    void ChangeViewToHead(bool isHead)
    {
        if (isHead)
        {
            cinemachineCamera.Follow = head;
            characterController.height = slidingColliderHeight; // 슬라이딩 시 캐릭터 컨트롤러 높이 변경
            characterController.center = new Vector3(0, slidingColliderHeight / 2, 0); // 슬라이딩 시 캐릭터 컨트롤러 중심 Y 좌표 변경
        }
        else
        {
            cinemachineCamera.Follow = cameraTransform;
            characterController.height = standingColliderHeight; // 서 있을 때 캐릭터 컨트롤러 높이로 변경
            characterController.center = new Vector3(0, standingColliderCenterY, 0); // 서 있을 때 캐릭터 컨트롤러 중심 Y 좌표로 변경
        }
    }

    #endregion
}