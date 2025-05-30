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
        public CharacterController CharacterControllerComponent { get; private set; }
        public PlayerAnimator PlayerAnimatorComponent { get; private set; }
        public PlayerUIManager PlayerUIComponent { get; private set; }

        // 분리된 컴포넌트들
        public PlayerInputSystem InputManagerComponent { get; private set; }
        public PlayerCameraController CameraControllerComponent { get; private set; }
        public PlayerAttackController AttackControllerComponent { get; private set; }
        public PlayerStatsManager StatsManagerComponent { get; private set; }
        #endregion

        #region Variables
        // 이동 관련 변수 (PlayerController가 직접 관리)
        public Vector2 MoveInput { get; private set; } // InputManager가 업데이트
        private Vector3 _lastGroundedPosition;

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
        public float mouseSensitivity = 2.0f;
        public float pitchMin = -85f;
        public float pitchMax = 85f;
        public float idleFOV = 75f;
        public float movingFOV = 80f;
        public float fovChangeSpeed = 10f;

        // 이동 파라미터 (PlayerController가 직접 관리)
        [Header("Movement")]
        public Vector3 CurrentSlidingVelocity;
        public float moveSpeed = 5f;
        public float jumpPower = 5.0f;
        public float slideInitialSpeedMultiplier = 1.5f;
        public float slideDeceleration = 2.0f;
        public float slidingColliderHeight = 0.5f;
        public float StandingColliderHeight { get; private set; }
        public float StandingColliderCenterY { get; private set; }
        public float VerticalVelocity { get; set; }

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
        // private float attackChargeTime = 0f; // AttackController가 관리
        // private bool isAttackCharging = false; // AttackController가 관리
        // private readonly Collider[] _attackOverlapResults = new Collider[30]; // AttackController가 관리

        // 스탯 파라미터 (PlayerStatsManager로 이전될 값들)
        [Header("Stat Settings (for StatsManager)")]
        public int maxHealth = 100;
        public int warningHp = 30;
        public int enemyDamage = 10; // 피격 시 기본 데미지
        public float regenerationCooldown = 5f;
        public int regenerationAmount = 1;
        public float invincibilityDuration = 0.5f;
        // [SerializeField] private int health; // StatsManager가 관리
        // internal float regenerationTimer = 0f; // StatsManager가 관리
        // private float invincibilityTimer = 0f; // StatsManager가 관리
        #endregion

        #region  Unity Methods

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            // 기본 컴포넌트 가져오기
            CharacterControllerComponent = GetComponent<CharacterController>();
            PlayerAnimatorComponent = GetComponentInChildren<PlayerAnimator>();
            PlayerUIComponent = FindFirstObjectByType<PlayerUIManager>(); // Scene에 하나만 있다고 가정

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
            InputManagerComponent.LookEvent += CameraControllerComponent.HandleLookInput; // 직접 연결
            InputManagerComponent.JumpEvent += OnJumpInputReceived;
            InputManagerComponent.CrouchEvent += OnCrouchInputReceived;
            InputManagerComponent.AttackEvent += AttackControllerComponent.HandleAttackInput; // 직접 연결
            InputManagerComponent.MenuEvent += OnMenuInputReceived;

            InputManagerComponent.DisablePlayerActions(); // 초기 입력 비활성화
            FadeSystem.StartFadeOut(0.5f, () => InputManagerComponent.EnablePlayerActions());
        }

        void Update()
        {
            _currentState?.Execute();

            // --- 이동 처리 로직 ---
            Vector3 horizontalMovement = Vector3.zero;
            if (CurrentStateType == PlayerState.Moving || CurrentStateType == PlayerState.Idle || CurrentStateType == PlayerState.Falling)
            {
                if (MoveInput != Vector2.zero)
                {
                    Vector3 worldMoveDirection = transform.TransformDirection(new Vector3(MoveInput.x, 0, MoveInput.y)).normalized;
                    horizontalMovement = worldMoveDirection * moveSpeed;
                }
            }

            if (CharacterControllerComponent.isGrounded)
            {
                if (VerticalVelocity < 0) VerticalVelocity = -2f;
                if (Physics.Raycast(transform.position + CharacterControllerComponent.center, Vector3.down, out RaycastHit hitInfo, CharacterControllerComponent.height / 2f + CharacterControllerComponent.skinWidth + 0.1f))
                {
                    if ((vaultableLayers.value & (1 << hitInfo.collider.gameObject.layer)) > 0)
                    {
                        _lastGroundedPosition = transform.position;
                    }
                }
            }
            else
            {
                VerticalVelocity += Physics.gravity.y * Time.deltaTime;
            }

            if (CurrentStateType != PlayerState.Sliding && CurrentStateType != PlayerState.Vaulting && CurrentStateType != PlayerState.ClimbingUp)
            {
                CharacterControllerComponent.Move((horizontalMovement + Vector3.up * VerticalVelocity) * Time.deltaTime);
            }
            else if (CurrentStateType == PlayerState.Sliding) // 슬라이딩은 자체 속도 사용
            {
                CharacterControllerComponent.Move((CurrentSlidingVelocity + Vector3.up * VerticalVelocity) * Time.deltaTime);
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
                StatsManagerComponent.TakeDamage(enemyDamage); // StatsManager를 통해 데미지 처리
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
        public void TransitionToState(PlayerState stateType)
        {
            _currentState?.Exit();
            _currentState = _states[(int)stateType];
            CurrentStateType = stateType;
            _currentState.Enter();
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
            JumpRequested = true;
            VerticalVelocity = jumpPower;
            PlayerAnimatorComponent.TriggerJump();
            TransitionToState(PlayerState.Falling);
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
            CameraControllerComponent.ChangeCameraFollowTarget(isSlidingView); // 카메라 부분 위임

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
            CameraControllerComponent.SetCameraFOV(targetFOV, immediate); // 카메라 부분 위임
        }
        #endregion
    }
}