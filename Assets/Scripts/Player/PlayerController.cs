using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

// TODO: 벽 뛰어넘기 구현중

public class PlayerController : MonoBehaviour
{
    #region Variables
    // -- 카메라 관련 변수 --
    [Header("Camera and Reference")]
    public Transform head;  // 머리 카메라 트랜스폼
    public Transform cameraTransform; // 카메라 트랜스폼
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

    #region Start

    void Start()
    {
        originalPlayerLayer = gameObject.layer; // 원래 레이어 마스크 저장

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
    #endregion

    #region Update (Movement)

    void Update()
    {
        if (isVaultingInternal)
        {
            PerformVault();
            return;
        }

        bool isGrounded = characterController.isGrounded;
        bool hasMoveInput = moveDirection != Vector2.zero;

        // 뛰어넘기 시도 (다른 상태 업데이트 전에 체크)
        if (isGrounded && hasMoveInput && CurrentState != PlayerState.Jumping && CurrentState != PlayerState.Sliding)
        {
            TryInitiateVault();
            if (isVaultingInternal) return; // 뛰어넘기가 시작되었으면 Update 종료
        }

        // --- 상태 전환 로직 ---
        // 1. 공중 상태 처리
        if (!isGrounded)
        {
            if (CurrentState != PlayerState.Jumping && CurrentState != PlayerState.Falling)
            {
                CurrentState = PlayerState.Falling;
            }
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            if (slidingState.magnitude > 0) slidingState = Vector3.zero; // 공중에서는 슬라이딩 중단
            crouchActive = false; // 공중에서는 슬라이드 시도 비활성화
            // TODO - 구르기 기능 추가할거면 위 줄 지우고 구르기 기능 추가하기
        }
        // 2. 지상 상태 처리
        else
        {
            if (CurrentState == PlayerState.Falling || CurrentState == PlayerState.Jumping) // 착지
            {
                verticalVelocity = -2f;
                if (crouchActive) // 착지 시 슬라이드 버튼이 눌려있었다면
                {
                    if (hasMoveInput)
                    {
                        CurrentState = PlayerState.Sliding;
                        Vector3 worldMoveDir = transform.TransformDirection(new Vector3(moveDirection.x, 0, moveDirection.y)).normalized;
                        slidingState = moveSpeed * slideInitialSpeedMultiplier * worldMoveDir;
                    }
                    else
                    {
                        CurrentState = PlayerState.Idle;
                        crouchActive = false; // 이동 입력 없으면 슬라이드 활성화 해제
                    }
                }
                else
                {
                    CurrentState = hasMoveInput ? PlayerState.Moving : PlayerState.Idle;
                }
            }
            // 점프 요청시
            if (jumpRequested)
            {
                // 아니면 그냥 점프
                verticalVelocity = jumpPower;
                CurrentState = PlayerState.Jumping;
                crouchActive = false; // 점프 시 슬라이드 활성화 해제
                slidingState = Vector3.zero;
                jumpRequested = false;
                playerAnimator.SetAnim(PlayerState.Jumping);
            }
            else if (crouchActive) // 슬라이드 버튼이 활성화 되어 있다면
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
                        crouchActive = false;
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
                        crouchActive = false; // 슬라이드 액션 완료
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
                    horizontalDisplacement = currentEffectiveSpeed * Time.deltaTime * worldMoveDir;
                }
                break;
            case PlayerState.Moving:
                if (hasMoveInput)
                {
                    Vector3 worldMoveDir = transform.TransformDirection(new Vector3(moveDirection.x, 0, moveDirection.y)).normalized;
                    horizontalDisplacement = currentEffectiveSpeed * Time.deltaTime * worldMoveDir;
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
            case PlayerState.Vaulting:
                // 입력과 관계없이 정면으로 이동
                Vector3 vaultDirection = transform.TransformDirection(Vector3.forward).normalized;
                horizontalDisplacement = currentEffectiveSpeed * Time.deltaTime * vaultDirection;

                // 앞에 장애물이 없으면 뛰어넘기 모드 해제
                if (!Physics.Raycast(transform.position + Vector3.up * 0.2f, transform.TransformDirection(Vector3.forward), out var hit, vaultCheckDistance, 8) || !hit.collider.CompareTag("Wall"))
                {
                    gameObject.layer = originalPlayerLayer;
                    CurrentState = PlayerState.Idle; // 장애물 뛰어넘기 완료 후 Idle 상태로 전환
                }
                break;
        }

        Vector3 verticalDisplacement = verticalVelocity * Time.deltaTime * Vector3.up;
        characterController.Move(horizontalDisplacement + verticalDisplacement);

        // --- 애니메이터 업데이트 (PlayerAnimator 사용) ---
        bool isActuallyMovingOnGround = hasMoveInput && (CurrentState == PlayerState.Moving || CurrentState == PlayerState.Idle) && isGrounded;
        playerAnimator.SetAnim(PlayerState.Moving, isActuallyMovingOnGround);

        playerAnimator.SetAnim(PlayerState.Falling, !isGrounded);

        playerAnimator.SetAnim(PlayerState.Sliding, CurrentState == PlayerState.Sliding);

        playerAnimator.SetDirection(moveDirection); // 애니메이터에 이동 방향 전달
    }

    private void PerformVault()
    {
        float elapsed = Time.time - vaultStartTime;
        float progress = Mathf.Clamp01(elapsed / currentVaultDuration); // 여기를 currentVaultDuration으로 변경

        Vector3 targetPosition;

        if (progress < 0.5f)
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

    private void TryInitiateVault()
    {
        // 캐릭터 발 근처에서 약간 앞에서 레이캐스트 시작
        Vector3 rayOriginFeet = transform.position + characterController.center + Vector3.up * (-standingColliderHeight / 2f + 0.05f);

        Debug.DrawRay(rayOriginFeet, transform.forward * vaultCheckDistance, Color.cyan, 0.5f);

        if (Physics.Raycast(rayOriginFeet, transform.forward, out RaycastHit hitInfo, vaultCheckDistance, vaultableLayers))
        {
            if (!hitInfo.collider.CompareTag("Wall"))
            {
                return;
            }

            // 장애물의 실제 상단 표면 찾기
            RaycastHit topSurfaceHit;
            Vector3 topRayCheckOrigin = hitInfo.point + Vector3.up * (standingColliderHeight + 0.1f); // 충돌 지점보다 충분히 위에서 시작
            topRayCheckOrigin += transform.forward * 0.05f;

            float obstacleActualTopY;
            Debug.DrawRay(topRayCheckOrigin, Vector3.down * (standingColliderHeight + 0.2f), Color.magenta, 0.5f);

            if (Physics.Raycast(topRayCheckOrigin, Vector3.down, out topSurfaceHit, standingColliderHeight + 0.2f, vaultableLayers))
            {
                obstacleActualTopY = topSurfaceHit.point.y;
            }
            else
            {
                Debug.LogWarning("Vault: Could not determine obstacle's top surface. Vault aborted.");
                return;
            }

            float obstacleHeightFromPlayerFeet = obstacleActualTopY - rayOriginFeet.y;
            float maxVaultableHeight = standingColliderHeight * canVaultHeightRatio;

            if (obstacleHeightFromPlayerFeet < 0.1f || obstacleHeightFromPlayerFeet >= maxVaultableHeight)
            {
                Debug.LogWarning($"Vault: Obstacle height {obstacleHeightFromPlayerFeet} is not vaultable. Vault aborted.");
                return;
            }

            // 장애물의 깊이 측정
            Bounds obstacleBounds = hitInfo.collider.bounds;
            // 장애물의 로컬 Z축 방향 벡터를 월드 방향으로 변환한 뒤, 플레이어의 전방 벡터와 내적하여 깊이 방향 성분을 구함.
            // 좀 더 정확한 방법은 OBB(Oriented Bounding Box)를 사용하거나, 여러 지점에서 레이캐스트를 하여 깊이를 추정하는 것.
            // 여기서는 간단하게 AABB의 extents를 사용.
            float obstacleDepth = Mathf.Abs(Vector3.Dot(obstacleBounds.extents, transform.forward.normalized)) * 2f;
            // depthThreshold 검사는 현재 사용하지 않으므로 생략

            // --- 동적 값 계산 ---
            // 1. 뛰어넘기 시간 (currentVaultDuration)
            currentVaultDuration = baseVaultDuration +
                                   (obstacleDepth * vaultDurationPerMeterDepth) +
                                   (obstacleHeightFromPlayerFeet * vaultDurationPerMeterHeight);
            currentVaultDuration = Mathf.Clamp(currentVaultDuration, minVaultDuration, maxVaultDuration);

            // 2. 뛰어넘기 시 장애물 위 추가 높이 (currentVaultJumpHeight)
            currentVaultJumpHeight = minVaultClearance + (obstacleHeightFromPlayerFeet * vaultHeightMultiplier);


            // --- 뛰어넘기 시작 ---
            CurrentState = PlayerState.Vaulting;
            isVaultingInternal = true;
            gameObject.layer = vaultLayerMask;
            vaultStartTime = Time.time;
            verticalVelocity = 0;

            vaultStartPosition = transform.position; // 현재 플레이어 위치에서 시작

            // 정점(vaultUpPosition) 계산:
            Vector3 peakHorizontalBase = hitInfo.point + transform.forward * (characterController.radius + 0.1f); // 장애물 표면 약간 너머
            vaultUpPosition = new Vector3(peakHorizontalBase.x, obstacleActualTopY + currentVaultJumpHeight, peakHorizontalBase.z);

            // 끝점(vaultEndPosition) 계산:
            // 장애물 앞면에서 (측정된 깊이 + 캐릭터 반지름 + 약간의 여유 공간) 만큼 앞으로
            float vaultForwardClearance = obstacleDepth + characterController.radius + 0.2f; // 장애물 깊이 + 반지름 + 착지 여유
            vaultEndPosition = hitInfo.point - transform.forward * hitInfo.distance + // 플레이어를 장애물 표면으로 이동
                               transform.forward * (hitInfo.distance + vaultForwardClearance);
            vaultEndPosition.y = vaultStartPosition.y;

            return;
        }
    }

    void EndVault()
    {
        isVaultingInternal = false;
        gameObject.layer = originalPlayerLayer; // 원래 레이어로 복원
        // characterController.Move(vaultEndPosition - transform.position); // 최종 위치 보정 (선택적)
        CurrentState = characterController.isGrounded ? PlayerState.Idle : PlayerState.Falling;
        // ChangeViewToHead(false)는 CurrentState setter에 의해 호출되어 콜라이더와 카메라를 복원합니다.
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
                jumpRequested = true;
            }
        }
    }

    void OnCrouch(InputAction.CallbackContext callback) // 이 입력은 이제 "슬라이드" 버튼으로 작동
    {
        if (callback.performed)
        {
            crouchActive = !crouchActive; // 슬라이드 활성화 상태 토글

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