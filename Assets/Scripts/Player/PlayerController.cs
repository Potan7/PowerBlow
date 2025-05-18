using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    #region Variables
    // -- 카메라 관련 변수 --
    [Header("Camera and Reference")]
    public Transform head;
    public Transform cameraTransform;
    public CinemachineCamera cinemachineCamera;

    // 이동 관련 변수
    Vector2 moveDirection; // 현재 프레임의 이동 입력 값

    MyInputActions playerInput;
    CharacterController characterController;

    // --- 애니메이션 관련 변수 ---
    public PlayerAnimator playerAnimator; // PlayerAnimator 참조

    [Header("Player State")]
    [SerializeField]
    private PlayerState currentState;
    public PlayerState CurrentState
    {
        get { return currentState; }
        set
        {
            if (currentState != value)
            {
                PlayerState previousState = currentState;
                currentState = value;
                OnStateChanged(previousState, currentState);
            }
        }
    }

    // --- 내부 요청 플래그 ---
    private bool jumpRequested = false;
    private bool crouchActive = false; // 슬라이드 버튼 토글 상태 (이전의 앉기 버튼)
    private bool isVaultingInternal = false; // 뛰어넘기 중인지 여부

    [Header("Movement")]
    // --- 슬라이딩 및 이동 관련 변수 ---
    public Vector3 slidingState; // 슬라이딩 중일 때의 월드 속도 벡터
    public float moveSpeed = 5f;
    public float jumpPower = 5.0f;
    public float slideInitialSpeedMultiplier = 1.5f; // 슬라이딩 시작 시 속도 배율 (moveSpeed * 이 값)
    public float slideDeceleration = 2.0f; // 슬라이딩 감속량 (값이 클수록 빨리 멈춤)
    public float slidingColliderHeight = 0.5f; // 슬라이딩 시 캐릭터 컨트롤러 높이
    private float standingColliderHeight; // 서 있을 때 캐릭터 컨트롤러 높이
    private float standingColliderCenterY; // 서 있을 때 캐릭터 컨트롤러 중심 Y 좌표

    [Header("Mouse and Camera Settings")]
    // --- 마우스 및 카메라 ---
    public float mouseSensitivity = 2.0f;
    public float pitchMin = -85f;
    public float pitchMax = 85f;
    private float currentPitch = 0.0f;

    // --- 수직 속도 ---
    private float verticalVelocity;

    // --- 뛰어넘기 관련 변수 ---
    [Header("Vaulting")]
    public float vaultCheckDistance = 1.5f; // 앞 장애물 인식 거리
    public float canVaultHeightRatio = 0.7f; // 뛰어넘기 가능 높이 비율 (캐릭터 키 대비, 예: 0.7 = 70%)
    // public float vaultJumpHeight = 0.5f; // 고정 값 대신 동적으로 계산
    public int vaultLayerMask = 6; // 뛰어넘기 중 사용할 레이어 인덱스 (Physics Collision Matrix 설정 필요)
    public LayerMask vaultableLayers; // 뛰어넘기 가능한 레이어 마스크
    private int originalPlayerLayer;
    private Vector3 vaultStartPosition;
    private Vector3 vaultUpPosition;
    private Vector3 vaultEndPosition;
    public float depthThreshold = 1.0f; // 이 값보다 깊으면 "올라가기" (현재는 사용 안 함)
    private float vaultStartTime;
    // public float vaultDuration = 1f; // 고정 값 대신 동적으로 계산

    [Header("Dynamic Vault Parameters")]
    public float baseVaultDuration = 0.4f; // 기본 뛰어넘기 시간
    public float vaultDurationPerMeterDepth = 0.15f; // 장애물 깊이 1미터당 추가되는 시간
    public float vaultDurationPerMeterHeight = 0.1f; // 장애물 높이 1미터당 추가되는 시간
    public float minVaultDuration = 0.3f;    // 최소 뛰어넘기 시간
    public float maxVaultDuration = 0.8f;    // 최대 뛰어넘기 시간

    public float minVaultClearance = 0.15f;  // 장애물 위로 최소한 확보할 여유 높이
    public float vaultHeightMultiplier = 0.3f; // 장애물 높이에 따라 추가될 여유 높이의 배율 (0.0 ~ 1.0)
    private float currentVaultDuration; // 현재 뛰어넘기에 사용될 실제 시간
    private float currentVaultJumpHeight; // 현재 뛰어넘기에 사용될 실제 점프 높이 (장애물 위 추가 높이)
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        originalPlayerLayer = gameObject.layer; // 원래 레이어 마스크 저장

        characterController = GetComponent<CharacterController>();
        standingColliderHeight = characterController.height; // 서 있을 때 캐릭터 컨트롤러 높이 저장
        standingColliderCenterY = characterController.center.y; // 서 있을 때 캐릭터 컨트롤러 중심 Y 좌표 저장

        playerInput = new MyInputActions();
        SetupInputActions();
        playerInput.Enable();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        CurrentState = PlayerState.Idle;
        InitializeAnimator();
    }

    void Update()
    {
        if (isVaultingInternal)
        {
            PerformVault();
            return;
        }

        bool isGrounded = characterController.isGrounded;
        bool hasMoveInput = moveDirection != Vector2.zero;

        TryInitiateVaultIfConditionsMet(isGrounded, hasMoveInput);
        if (isVaultingInternal) return;

        UpdatePlayerMovementState(isGrounded, hasMoveInput);
        ApplyMovement(isGrounded, hasMoveInput);
        UpdateAnimatorParameters(isGrounded, hasMoveInput);
    }
    #endregion

    #region Initialization
    void SetupInputActions()
    {
        playerInput.Player.Move.performed += OnMove;
        playerInput.Player.Move.canceled += OnMove;
        playerInput.Player.Look.performed += OnLook;
        playerInput.Player.Jump.performed += OnJump;
        playerInput.Player.Crouch.performed += OnCrouch;
    }

    void InitializeAnimator()
    {
        playerAnimator.SetAnim(PlayerState.Moving, false);
        playerAnimator.SetAnim(PlayerState.Falling, false);
        playerAnimator.SetAnim(PlayerState.Sliding, false);
    }
    #endregion

    #region State Management
    void OnStateChanged(PlayerState previousState, PlayerState newState)
    {
        // 슬라이딩 상태에 따른 카메라 및 콜라이더 변경
        if (newState == PlayerState.Sliding)
        {
            ChangeViewAndCollider(true);
        }
        else if (previousState == PlayerState.Sliding && newState != PlayerState.Sliding)
        {
            ChangeViewAndCollider(false);
        }
    }

    void UpdatePlayerMovementState(bool isGrounded, bool hasMoveInput)
    {
        if (!isGrounded)
        {
            HandleAirborneLogic();
        }
        else
        {
            HandleGroundedLogic(hasMoveInput);
        }
    }

    void HandleAirborneLogic()
    {
        if (CurrentState != PlayerState.Jumping && CurrentState != PlayerState.Falling)
        {
            CurrentState = PlayerState.Falling;
        }
        verticalVelocity += Physics.gravity.y * Time.deltaTime;
        if (slidingState.magnitude > 0) slidingState = Vector3.zero;
        crouchActive = false;
    }

    void HandleGroundedLogic(bool hasMoveInput)
    {
        if (CurrentState == PlayerState.Falling || CurrentState == PlayerState.Jumping) // 착지
        {
            ProcessLanding(hasMoveInput);
        }

        if (jumpRequested)
        {
            ProcessJump();
        }
        else if (crouchActive)
        {
            ProcessSlide(hasMoveInput);
        }
        else // 일반 이동 또는 슬라이드 종료
        {
            ProcessNormalMovement(hasMoveInput);
        }
    }

    void ProcessLanding(bool hasMoveInput)
    {
        verticalVelocity = -2f; // 안정적인 착지를 위해 약간의 하강 속도 유지
        if (crouchActive && hasMoveInput)
        {
            StartSliding();
        }
        else
        {
            CurrentState = hasMoveInput ? PlayerState.Moving : PlayerState.Idle;
            if (crouchActive) crouchActive = false; // 이동 입력 없으면 슬라이드 비활성화
        }
    }

    void ProcessJump()
    {
        verticalVelocity = jumpPower;
        CurrentState = PlayerState.Jumping;
        crouchActive = false;
        slidingState = Vector3.zero;
        jumpRequested = false;
        playerAnimator.SetAnim(PlayerState.Jumping); // 점프 애니메이션 트리거
    }

    void ProcessSlide(bool hasMoveInput)
    {
        if (CurrentState != PlayerState.Sliding)
        {
            if (hasMoveInput && (CurrentState == PlayerState.Moving || CurrentState == PlayerState.Idle))
            {
                StartSliding();
            }
            else // 이동 입력 없이 슬라이드 시도 시
            {
                CurrentState = PlayerState.Idle;
                crouchActive = false;
            }
        }
        else // 현재 슬라이딩 중
        {
            UpdateSlidingMovement(hasMoveInput);
        }
    }

    void StartSliding()
    {
        CurrentState = PlayerState.Sliding;
        Vector3 worldMoveDir = transform.TransformDirection(new Vector3(moveDirection.x, 0, moveDirection.y)).normalized;
        slidingState = worldMoveDir * moveSpeed * slideInitialSpeedMultiplier;
    }

    void UpdateSlidingMovement(bool hasMoveInput)
    {
        if (slidingState.magnitude > 0.1f)
        {
            slidingState -= slidingState.normalized * slideDeceleration * Time.deltaTime;
            if (slidingState.magnitude < 0.1f) slidingState = Vector3.zero;
        }

        if (slidingState.magnitude <= 0.1f) // 슬라이딩 종료
        {
            CurrentState = hasMoveInput ? PlayerState.Moving : PlayerState.Idle;
            crouchActive = false;
        }
    }

    void ProcessNormalMovement(bool hasMoveInput)
    {
        if (CurrentState == PlayerState.Sliding) // 슬라이드 중 crouchActive가 false가 되면
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
    #endregion

    #region Movement Execution
    void ApplyMovement(bool isGrounded, bool hasMoveInput)
    {
        Vector3 horizontalDisplacement = Vector3.zero;
        float currentEffectiveSpeed = moveSpeed;

        switch (CurrentState)
        {
            case PlayerState.Idle:
            case PlayerState.Moving:
                if (hasMoveInput && isGrounded) // Idle 상태에서도 즉시 반응하도록
                {
                    Vector3 worldMoveDir = transform.TransformDirection(new Vector3(moveDirection.x, 0, moveDirection.y)).normalized;
                    horizontalDisplacement = worldMoveDir * currentEffectiveSpeed * Time.deltaTime;
                }
                break;
            case PlayerState.Sliding:
                horizontalDisplacement = slidingState * Time.deltaTime;
                break;
            case PlayerState.Jumping:
            case PlayerState.Falling:
                if (hasMoveInput) // 공중 제어
                {
                    Vector3 worldMoveDir = transform.TransformDirection(new Vector3(moveDirection.x, 0, moveDirection.y)).normalized;
                    horizontalDisplacement = worldMoveDir * currentEffectiveSpeed * Time.deltaTime;
                }
                break;
            // Vaulting 상태의 이동은 PerformVault에서 처리
        }

        Vector3 verticalDisplacement = verticalVelocity * Time.deltaTime * Vector3.up;
        characterController.Move(horizontalDisplacement + verticalDisplacement);
    }
    #endregion

    #region Vaulting
    void TryInitiateVaultIfConditionsMet(bool isGrounded, bool hasMoveInput)
    {
        if (isGrounded && hasMoveInput && CurrentState != PlayerState.Jumping && CurrentState != PlayerState.Sliding && CurrentState != PlayerState.Vaulting)
        {
            TryInitiateVaultInternal();
        }
    }

    void TryInitiateVaultInternal()
    {
        Vector3 rayOriginFeet = transform.position + characterController.center + Vector3.up * (-standingColliderHeight / 2f + 0.05f);
        Debug.DrawRay(rayOriginFeet, transform.forward * vaultCheckDistance, Color.cyan, 0.5f);

        if (Physics.Raycast(rayOriginFeet, transform.forward, out RaycastHit hitInfo, vaultCheckDistance, vaultableLayers))
        {
            if (!hitInfo.collider.CompareTag("Wall")) return;

            if (!TryGetObstacleTopSurface(hitInfo, out float obstacleActualTopY)) return;

            float obstacleHeightFromPlayerFeet = obstacleActualTopY - rayOriginFeet.y;
            float maxVaultableHeight = standingColliderHeight * canVaultHeightRatio;

            if (obstacleHeightFromPlayerFeet < 0.1f || obstacleHeightFromPlayerFeet >= maxVaultableHeight)
            {
                Debug.LogWarning($"Vault: Obstacle height {obstacleHeightFromPlayerFeet} is not vaultable. Vault aborted.");
                return;
            }

            float obstacleDepth = CalculateObstacleDepth(hitInfo);
            CalculateDynamicVaultParameters(obstacleDepth, obstacleHeightFromPlayerFeet);
            StartVaultSequence(hitInfo, obstacleActualTopY, obstacleDepth);
        }
    }

    bool TryGetObstacleTopSurface(RaycastHit frontHit, out float topY)
    {
        topY = 0f;
        // 이전 코드의 topRayCheckOrigin 로직 사용, 필요시 이전 답변의 개선된 로직 적용
        Vector3 topRayCheckOrigin = frontHit.point + Vector3.up * (standingColliderHeight + 0.1f) + transform.forward * 0.05f;
        Debug.DrawRay(topRayCheckOrigin, Vector3.down * (standingColliderHeight + 0.2f), Color.magenta, 0.5f);

        if (Physics.Raycast(topRayCheckOrigin, Vector3.down, out RaycastHit topSurfaceHit, standingColliderHeight + 0.2f, vaultableLayers))
        {
            topY = topSurfaceHit.point.y;
            return true;
        }
        else
        {
            Debug.LogWarning("Vault: Could not determine obstacle's top surface. Vault aborted.");
            return false;
        }
    }

    float CalculateObstacleDepth(RaycastHit hitInfo)
    {
        Bounds obstacleBounds = hitInfo.collider.bounds;
        return Mathf.Abs(Vector3.Dot(obstacleBounds.extents, transform.forward.normalized)) * 2f;
    }

    void CalculateDynamicVaultParameters(float depth, float height)
    {
        currentVaultDuration = baseVaultDuration + (depth * vaultDurationPerMeterDepth) + (height * vaultDurationPerMeterHeight);
        currentVaultDuration = Mathf.Clamp(currentVaultDuration, minVaultDuration, maxVaultDuration);
        currentVaultJumpHeight = minVaultClearance + (height * vaultHeightMultiplier);
    }

    void StartVaultSequence(RaycastHit hitInfo, float obstacleActualTopY, float obstacleDepth)
    {
        CurrentState = PlayerState.Vaulting;
        isVaultingInternal = true;
        gameObject.layer = vaultLayerMask;
        vaultStartTime = Time.time;
        verticalVelocity = 0; // 뛰어넘기 중에는 중력 및 기존 수직 속도 무시

        vaultStartPosition = transform.position;
        Vector3 peakHorizontalBase = hitInfo.point + transform.forward * (characterController.radius + 0.1f);
        vaultUpPosition = new Vector3(peakHorizontalBase.x, obstacleActualTopY + currentVaultJumpHeight, peakHorizontalBase.z);

        float vaultForwardClearance = obstacleDepth + characterController.radius + 0.2f;
        // 이전 코드의 vaultEndPosition 계산에서 vaultCheckDistance를 더하는 부분은 의도에 따라 조정 필요
        // 여기서는 이전 코드와 유사하게 유지하되, 일반적으로는 vaultCheckDistance를 더하지 않음
        vaultEndPosition = hitInfo.point - transform.forward * hitInfo.distance +
                           transform.forward * (hitInfo.distance + vaultForwardClearance); // + vaultCheckDistance 제거 또는 조정
        vaultEndPosition.y = vaultStartPosition.y;

        playerAnimator.SetAnim(PlayerState.Vaulting); // 뛰어넘기 애니메이션 트리거
    }

    void PerformVault()
    {
        float elapsed = Time.time - vaultStartTime;
        float progress = Mathf.Clamp01(elapsed / currentVaultDuration);

        Vector3 targetPosition;
        if (progress < 0.5f) // 0.3f 대신 0.5f로 변경하여 정점을 중간 지점으로
        {
            targetPosition = Vector3.Lerp(vaultStartPosition, vaultUpPosition, progress * 2f);
        }
        else
        {
            targetPosition = Vector3.Lerp(vaultUpPosition, vaultEndPosition, (progress - 0.5f) * 2f);
        }

        Vector3 movement = targetPosition - transform.position;
        characterController.Move(movement);

        if (progress >= 1.0f)
        {
            EndVault();
        }
    }

    void EndVault()
    {
        isVaultingInternal = false;
        gameObject.layer = originalPlayerLayer;
        CurrentState = characterController.isGrounded ? PlayerState.Idle : PlayerState.Falling;
        // Vaulting 애니메이션이 Trigger라면 별도로 false로 설정할 필요 없음
    }
    #endregion

    #region Animation
    void UpdateAnimatorParameters(bool isGrounded, bool hasMoveInput)
    {
        // Vaulting 애니메이션은 StartVaultSequence에서 Trigger로 처리
        if (CurrentState != PlayerState.Vaulting)
        {
            bool isActuallyMovingOnGround = hasMoveInput && (CurrentState == PlayerState.Moving || CurrentState == PlayerState.Idle) && isGrounded;
            playerAnimator.SetAnim(PlayerState.Moving, isActuallyMovingOnGround);
            playerAnimator.SetAnim(PlayerState.Falling, !isGrounded && CurrentState != PlayerState.Jumping); // 점프 중에는 점프 애니메이션 우선
            playerAnimator.SetAnim(PlayerState.Sliding, CurrentState == PlayerState.Sliding);
        }
        playerAnimator.SetDirection(moveDirection);
    }
    #endregion

    #region Input Callbacks
    void OnMove(InputAction.CallbackContext callback)
    {
        moveDirection = callback.ReadValue<Vector2>();
    }

    void OnLook(InputAction.CallbackContext callback)
    {
        Vector2 mouseDelta = callback.ReadValue<Vector2>();
        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        currentPitch -= mouseY;
        currentPitch = Mathf.Clamp(currentPitch, pitchMin, pitchMax);
        head.localRotation = Quaternion.Euler(currentPitch, 0, 0); // Y, Z 회전은 부모에서 처리되므로 0으로 설정
        cameraTransform.localRotation = Quaternion.Euler(currentPitch, 0, 0);
    }

    void OnJump(InputAction.CallbackContext callback)
    {
        if (callback.performed && characterController.isGrounded &&
            (CurrentState == PlayerState.Idle || CurrentState == PlayerState.Moving || CurrentState == PlayerState.Sliding))
        {
            jumpRequested = true;
        }
    }

    void OnCrouch(InputAction.CallbackContext callback)
    {
        if (callback.performed)
        {
            crouchActive = !crouchActive;
            // 실제 슬라이딩 시작/종료는 UpdatePlayerMovementState에서 처리
        }
    }
    #endregion

    #region Methods
    void ChangeViewAndCollider(bool isSlidingView)
    {
        if (isSlidingView)
        {
            cinemachineCamera.Follow = head;
            characterController.height = slidingColliderHeight;
            characterController.center = new Vector3(0, slidingColliderHeight / 2, 0);
        }
        else
        {
            cinemachineCamera.Follow = cameraTransform;
            characterController.height = standingColliderHeight;
            characterController.center = new Vector3(0, standingColliderCenterY, 0);
        }
    }
    #endregion
}