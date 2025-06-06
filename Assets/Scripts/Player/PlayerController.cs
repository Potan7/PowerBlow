using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using Player.State;
using Player.Component;

namespace Player
{
    public class PlayerController : MonoBehaviour
    {
        #region Singleton
        public static PlayerController Instance { get; private set; }
        #endregion

        #region Component References
        // 인스펙터가 길기 때문에 직접 할당이 아닌 자동으로 할당하도록
        public CharacterController CharacterControllerComponent { get; private set; }
        public PlayerAnimator PlayerAnimatorComponent { get; private set; }
        public PlayerUIManager PlayerUIComponent { get; private set; }
        public PlayerAudioManager PlayerAudioComponent { get; private set; }

        // 분리된 컴포넌트들
        public PlayerInputSystem InputManagerComponent { get; private set; }
        public PlayerCameraController CameraControllerComponent { get; private set; }
        public PlayerAttackController AttackControllerComponent { get; private set; }
        public PlayerStatsManager StatsManagerComponent { get; private set; }
        #endregion

        #region Variables

        // 상태 패턴 관련 (PlayerController가 직접 관리)
        private PlayerStateEntity _currentState = null;
        private PlayerStateEntity[] _states;
        public PlayerState CurrentStateType { get; private set; }

        // 내부 요청 플래그 (PlayerController가 직접 관리, 상태에서 사용)
        public bool JumpRequested { get; set; } = false;
        public bool CrouchActive { get; set; } = false;
        public bool IsVaultingInternal { get; set; } = false;

        // 카메라 설정 (PlayerCameraController로 이전될 값들, PlayerController에 public으로 남겨서 CameraController가 Init 시 가져가도록)
        [Header("Camera Settings (for CameraController)")]
        public Transform head; // 카메라 컨트롤러가 사용할 머리 트랜스폼
        public Transform cameraTransform; // 카메라 컨트롤러가 사용할 카메라 리그 트랜스폼
        public CinemachineCamera cinemachineCamera; // 시네머신 카메라
        public GameObject speedParticle; // 속도 파티클
        // public float mouseSensitivity = 2.0f;
        public float pitchMin = -85f;
        public float pitchMax = 85f;
        public float idleFOV = 75f;
        public float movingFOV = 80f;
        public float fovChangeSpeed = 10f;

        // 이동 파라미터 (PlayerController가 직접 관리)
        [Header("Movement")]
        public Vector2 MoveInput { get; private set; } // InputManager가 업데이트
        private Vector3 _lastGroundedPosition;  // 낙하시 리셋용 위치
        public float moveSpeed = 5f;    // 이동속도
        public float acceleration = 10f; // 초당 증가할 속도
        public float deceleration = 15f; // 초당 감소할 속도
        public float currentHorizontalSpeed = 0f; // 현재 수평 속도
        public float rapidTurnRatio = 0.8f; // 급회전 시 속도 감소 비율
        public float jumpPower = 5.0f;  // 점프 힘
        public float slideInitialSpeedMultiplier = 1.5f; // 슬라이딩 시작 시 초기 속도 배율
        public float slideDeceleration = 2.0f;
        public float slidingColliderHeight = 0.5f;
        public float jumpTimeMargin = 0.5f; // FallingState여도 점프가 가능한 시간
        public float StandingColliderHeight { get; private set; }
        public float StandingColliderCenterY { get; private set; }
        public float VerticalVelocity { get; set; }
        public float FallingStartTime { get; set; } = 0f; // FallingState가 된 시간 (점프 가능 시간 계산용)
        public bool isOnSpeedBlock = false; // 스피드 블록 위에 있는지 여부
        public Vector3 lastHorizontalMoveDirection = Vector3.zero; // 마지막 수평 이동 방향 저장

        // Vaulting 파라미터 (PlayerController가 직접 관리)
        [Header("Vaulting")]
        public float vaultCheckDistance = 1.5f;
        public float canVaultHeightRatio = 0.7f;
        public int vaultLayerMask = 6; // 실제 레이어 마스크 값 (예: 1 << 6) 또는 LayerMask 타입 사용
        public LayerMask vaultableLayers;
        public int OriginalPlayerLayer { get; private set; }
        public Vector3 VaultStartPosition { get; set; }
        public Vector3 VaultUpPosition { get; set; }
        public Vector3 VaultEndPosition { get; set; }
        public float VaultStartTime { get; set; }
        public float maxVaultableDepth = 1.0f;

        [Header("Dynamic Vault Parameters")]
        public float baseVaultDuration = 0.4f;
        public float vaultDurationPerMeterDepth = 0.15f;
        public float vaultDurationPerMeterHeight = 0.1f;
        public float minVaultDuration = 0.3f;
        public float maxVaultDuration = 0.8f;
        public float minVaultClearance = 0.15f;
        public float vaultHeightMultiplier = 0.3f;
        public float CurrentVaultDuration { get; set; }
        public float CurrentVaultJumpHeight { get; set; }

        // Wall Climb 파라미터 (PlayerController가 직접 관리)
        [Header("Wall Climb Settings")]
        public float wallClimbCheckDistance = 0.8f;
        public float minWallClimbHeight = 0.5f;
        public float maxWallClimbHeight = 1.8f;
        public float wallClimbLedgeOffset = 0.5f;

        // 공격 파라미터 (PlayerAttackController로 이전될 값들)
        [Header("Attack Settings (for AttackController)")]
        public float attackRange = 1.5f;
        public float attackPower = 10f;
        public float attackCooldown = 1f;
        public float attackMinChargeTime = 0.5f;
        public float attackMaxChargeTime = 2f;
        public float attackJumpPower = 5f; // 공격 시 위로 튕겨오르는 힘
        public bool attackOvercharge = false; // 공격 과충전 여부

        // 스탯 파라미터 (PlayerStatsManager로 이전될 값들)
        [Header("Stat Settings (for StatsManager)")]
        public int maxHealth = 100;
        public int warningHp = 30;
        public int fallDamage = 10; // 낙하 시 기본 데미지
        public float regenerationCooldown = 5f;
        public int regenerationAmount = 1;
        public float invincibilityDuration = 0.5f;
        #endregion

        #region  Unity Methods

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            // 기본 컴포넌트 가져오기
            CharacterControllerComponent = GetComponent<CharacterController>();
            PlayerAnimatorComponent = GetComponentInChildren<PlayerAnimator>();
            PlayerUIComponent = FindFirstObjectByType<PlayerUIManager>();
            PlayerAudioComponent = GetComponentInChildren<PlayerAudioManager>();

            InputManagerComponent = GetComponent<PlayerInputSystem>();
        }

        void Start()
        {
            // 컴포넌트 초기화
            InputManagerComponent.Init(this, CameraControllerComponent, AttackControllerComponent);
            CameraControllerComponent = new PlayerCameraController(this);
            AttackControllerComponent = new PlayerAttackController(this, PlayerAnimatorComponent, PlayerUIComponent, CharacterControllerComponent);
            StatsManagerComponent = new PlayerStatsManager(this, PlayerUIComponent);


            // 상태 인스턴스 생성
            _states = new PlayerStateEntity[]
            {
                new IdleState(this), new MovingState(this), new FallingState(this),
                new SlidingState(this), new VaultingState(this), new ClimbingUpState(this),
            };

            OriginalPlayerLayer = gameObject.layer;
            StandingColliderHeight = CharacterControllerComponent.height;
            StandingColliderCenterY = CharacterControllerComponent.center.y;

            CameraControllerComponent.SetCameraFOV(idleFOV, true); // 초기 FOV 설정

            TransitionToState(PlayerState.Idle);

            // 입력 이벤트 구독
            InputManagerComponent.MoveEvent += OnMoveInputReceived;
            InputManagerComponent.LookEvent += CameraControllerComponent.HandleLookInput;
            InputManagerComponent.JumpEvent += OnJumpInputReceived;
            InputManagerComponent.CrouchEvent += OnCrouchInputReceived;
            InputManagerComponent.AttackEvent += AttackControllerComponent.HandleAttackInput;
            InputManagerComponent.MenuEvent += OnMenuInputReceived;

            InputManagerComponent.DisablePlayerActions(); // 초기 입력 비활성화
            FadeSystem.StartFadeOut(0.5f, () => InputManagerComponent.EnablePlayerActions());
        }

        void Update()
        {
            _currentState?.Execute();

            // --- Ground Check, Speed Block Detection, and Last Grounded Position ---
            if (CharacterControllerComponent.isGrounded)
            {
                if (VerticalVelocity < 0) VerticalVelocity = -2f;

                isOnSpeedBlock = false; 
                if (Physics.Raycast(transform.position + CharacterControllerComponent.center, Vector3.down, out RaycastHit groundHitInfo, CharacterControllerComponent.height / 2f + CharacterControllerComponent.skinWidth + 0.2f))
                {
                    if (groundHitInfo.collider.CompareTag("Speed"))
                    {
                        isOnSpeedBlock = true;
                    }
                    if ((vaultableLayers.value & (1 << groundHitInfo.collider.gameObject.layer)) > 0)
                    {
                        _lastGroundedPosition = transform.position;
                    }
                }
            }
            else 
            {
                VerticalVelocity += Physics.gravity.y * Time.deltaTime;
            }

            // --- 이동 처리 로직 ---
            Vector3 horizontalMovement = Vector3.zero;
            Vector3 currentFrameWorldMoveDirection = lastHorizontalMoveDirection; // 기본적으로 마지막 이동 방향 유지

            if (CurrentStateType == PlayerState.Sliding)
            {
                // 슬라이딩 중에는 currentHorizontalSpeed가 slideDeceleration에 의해 감속됨
                currentHorizontalSpeed = Mathf.MoveTowards(currentHorizontalSpeed, 0f, slideDeceleration * Time.deltaTime);
                // 슬라이딩 방향은 SlidingState 진입 시 _lastHorizontalMoveDirection에 설정된 값을 사용
                currentFrameWorldMoveDirection = lastHorizontalMoveDirection;
            }
            else if (CurrentStateType == PlayerState.Moving || CurrentStateType == PlayerState.Idle || CurrentStateType == PlayerState.Falling)
            {
                float targetSpeed;
                if (MoveInput != Vector2.zero)
                {
                    float baseSpeedForInputDirection;
                    if (MoveInput.y > 0) 
                    {
                        baseSpeedForInputDirection = moveSpeed;
                    }
                    else 
                    {
                        baseSpeedForInputDirection = moveSpeed * 0.8f;
                    }

                    if (isOnSpeedBlock && CharacterControllerComponent.isGrounded)
                    {
                        baseSpeedForInputDirection *= 3f;
                    }

                    targetSpeed = baseSpeedForInputDirection;

                    if (CameraControllerComponent.IsRapidTurn) // 급회전 감속은 슬라이딩 중에는 적용 안 함
                    {
                        targetSpeed *= rapidTurnRatio;
                    }
                    
                    currentFrameWorldMoveDirection = transform.TransformDirection(new Vector3(MoveInput.x, 0, MoveInput.y)).normalized;
                    lastHorizontalMoveDirection = currentFrameWorldMoveDirection; // 마지막 유효 이동 방향 업데이트

                    if (currentHorizontalSpeed < 0.1f && targetSpeed > 0.01f) 
                    {
                        currentHorizontalSpeed = Mathf.Max(currentHorizontalSpeed, 0.1f); 
                    }
                    
                    float actualAccelerationRate = (targetSpeed > currentHorizontalSpeed) ? acceleration : deceleration;
                    currentHorizontalSpeed = Mathf.MoveTowards(currentHorizontalSpeed, targetSpeed, actualAccelerationRate * Time.deltaTime);
                }
                else // MoveInput == Vector2.zero (입력이 없을 때)
                {
                    if (CurrentStateType == PlayerState.Falling)
                    {
                        // 공중이고 입력이 없을 때: currentHorizontalSpeed와 _lastHorizontalMoveDirection 유지
                        // currentFrameWorldMoveDirection은 이미 _lastHorizontalMoveDirection으로 설정됨
                    }
                    else // 땅 위이고 입력이 없을 때 (Idle 상태로 가는 중)
                    {
                        targetSpeed = 0f; 
                        currentHorizontalSpeed = Mathf.MoveTowards(currentHorizontalSpeed, targetSpeed, deceleration * Time.deltaTime);
                        // currentFrameWorldMoveDirection은 _lastHorizontalMoveDirection으로 유지, 속도에 따라 스케일됨
                    }
                }
            }
            // Vaulting, ClimbingUp 같은 다른 상태들은 필요시 currentHorizontalSpeed를 직접 관리하거나 0으로 설정할 수 있음

            // 최종 수평 속도 및 방향 결정
            if (currentHorizontalSpeed < 0.01f && CurrentStateType != PlayerState.Falling) // 공중 관성 제외
            {
                currentHorizontalSpeed = 0f;
            }

            if (currentHorizontalSpeed == 0f && CharacterControllerComponent.isGrounded)
            {
                 currentFrameWorldMoveDirection = Vector3.zero; // 완전히 멈췄으면 이동 방향 없음
            }
            horizontalMovement = currentFrameWorldMoveDirection * currentHorizontalSpeed;

            // 속도가 어느정도 커지면 파티클 활성화
            speedParticle.SetActive(currentHorizontalSpeed > 0.3f);


            // --- 최종 이동 적용 ---
            // 슬라이딩 상태도 이 공통 이동 로직을 사용
            if (CurrentStateType != PlayerState.Vaulting && CurrentStateType != PlayerState.ClimbingUp)
            {
                CharacterControllerComponent.Move((horizontalMovement + Vector3.up * VerticalVelocity) * Time.deltaTime);
            }
            // Vaulting, Climbing은 상태 내에서 CharacterController.Move를 직접 호출하거나, PlayerController에 목표 위치를 전달하여 이동


            // --- 분리된 컴포넌트들의 Update 호출 ---
            CameraControllerComponent.UpdateFOV();
            AttackControllerComponent.UpdateAttackCharge();
            StatsManagerComponent.UpdateTimers();


            // 낙하 시 리셋 로직
            if (transform.position.y < -5f)
            {
                transform.position = _lastGroundedPosition;
                VerticalVelocity = 0f;
                currentHorizontalSpeed = 0f;
                StatsManagerComponent.TakeDamage(fallDamage);
                TransitionToState(PlayerState.Idle);
                JumpRequested = false;
                CrouchActive = false;
            }
        }

        private void OnDestroy()
        {
            InputManagerComponent.MoveEvent -= OnMoveInputReceived;
            InputManagerComponent.LookEvent -= CameraControllerComponent.HandleLookInput;
            InputManagerComponent.JumpEvent -= OnJumpInputReceived;
            InputManagerComponent.CrouchEvent -= OnCrouchInputReceived;
            InputManagerComponent.AttackEvent -= AttackControllerComponent.HandleAttackInput;
            InputManagerComponent.MenuEvent -= OnMenuInputReceived;

            if (Instance == this) Instance = null;
        }
        #endregion

        #region State Management
        public void TransitionToState(PlayerState newState)
        {
            var oldState = CurrentStateType;

            _currentState?.Exit();
            _currentState = _states[(int)newState];
            CurrentStateType = newState;
            _currentState.Enter();

            // 슬라이딩, 이동 중 공중에 떠도 잠깐의 시간동안 점프를 허용
            if ((oldState == PlayerState.Sliding || oldState == PlayerState.Moving) && newState == PlayerState.Falling)
            {
                FallingStartTime = Time.time;
            }
        }
        #endregion

        #region Input Event Handlers
        private void OnMoveInputReceived(Vector2 moveData)
        {
            MoveInput = moveData;
        }

        private void OnJumpInputReceived()
        {
            if (CurrentStateType == PlayerState.Sliding)
            {
                // 슬라이딩 중 점프는 슬라이딩 상태에게 위임
                JumpRequested = true;
                return;
            }

            if (CharacterControllerComponent.isGrounded &&
                (CurrentStateType == PlayerState.Idle || CurrentStateType == PlayerState.Moving))
            {
                DoJump();
                return;
            }

            if (CurrentStateType == PlayerState.Falling && Time.time - FallingStartTime < jumpTimeMargin)
            {
                // Falling 상태에서 점프 요청
                DoJump();
                return;
            }

            
        }

        private void OnCrouchInputReceived()
        {
            CrouchActive = !CrouchActive;
        }

        private void OnMenuInputReceived()
        {
            if (MenuManager.Instance != null) // MenuManager 싱글턴 사용 가정
                MenuManager.SetMenuActive(!MenuManager.Instance.IsMenuActive);
        }
        #endregion

        #region Methods
        // Vault/Climb 관련 유틸리티 메서드는 PlayerController에 남겨두거나, PlayerMovementUtils 등으로 분리 가능
        public bool TryGetObstacleTopSurface(Vector3 front, out float topY)
        {
            topY = 0f;
            Vector3 topRayCheckOrigin = front + Vector3.up * (StandingColliderHeight + 0.1f) + transform.forward * 0.05f;
            if (Physics.Raycast(topRayCheckOrigin, Vector3.down, out RaycastHit topSurfaceHit, StandingColliderHeight + 0.2f, vaultableLayers))
            {
                topY = topSurfaceHit.point.y;
                return true;
            }
            return false;
        }

        public void DoJump()
        {
            JumpRequested = false;
            VerticalVelocity = jumpPower;
            PlayerAnimatorComponent.TriggerJump();

            TransitionToState(PlayerState.Falling);

            // 점프로 FallingState 진입시 점프 여유시간 제거
            FallingStartTime = 0;

            PlayerAudioComponent.PlaySound(PlayerAudioManager.PlayerAudioType.Jump);
        }

        public float CalculateObstacleDepth(RaycastHit hitInfo)
        {
            if (hitInfo.collider == null) return 0f;
            Bounds obstacleBounds = hitInfo.collider.bounds;
            return Mathf.Abs(Vector3.Dot(obstacleBounds.extents, transform.forward.normalized)) * 2f;
        }

        public void CalculateDynamicVaultParameters(float depth, float height)
        {
            CurrentVaultDuration = baseVaultDuration + (depth * vaultDurationPerMeterDepth) + (height * vaultDurationPerMeterHeight);
            CurrentVaultDuration = Mathf.Clamp(CurrentVaultDuration, minVaultDuration, maxVaultDuration);
            CurrentVaultJumpHeight = minVaultClearance + (height * vaultHeightMultiplier);
        }

        public void ChangeViewAndCollider(bool isSlidingView) // 주로 상태에서 호출
        {
            CameraControllerComponent.ChangeCameraFollowTarget(isSlidingView); // 카메라 부분 처리

            // 콜라이더 변경은 PlayerController가 직접
            if (isSlidingView)
            {
                CharacterControllerComponent.height = slidingColliderHeight;
                CharacterControllerComponent.center = new Vector3(0, slidingColliderHeight / 2, 0);
            }
            else
            {
                CharacterControllerComponent.height = StandingColliderHeight;
                CharacterControllerComponent.center = new Vector3(0, StandingColliderCenterY, 0);
            }
        }

        public void EnemyIsDead() // 외부(Enemy)에서 호출 가능
        {
            PlayerUIComponent.AddEnemyDeadScore();
        }

        public void SetCameraFOV(float targetFOV, bool immediate = false) // 상태에서 호출
        {
            CameraControllerComponent.SetCameraFOV(targetFOV, immediate); // 카메라 부분 처리
        }
        #endregion
    }
}