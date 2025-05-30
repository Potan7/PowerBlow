using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using Player.State;
using System.Collections;

namespace Player
{
    public class PlayerController : MonoBehaviour
    {
        #region Singleton
        public static PlayerController Instance { get; private set; }
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            CharacterControllerComponent = GetComponent<CharacterController>();
            PlayerAnimatorComponent = GetComponentInChildren<PlayerAnimator>();

            PlayerUIComponent = FindAnyObjectByType<PlayerUIManager>();
            PlayerInput = new MyInputActions();
        }
        #endregion

        #region Variables

        // -- 카메라 관련 변수 --
        [Header("Camera References")]
        public Transform head;
        public Transform cameraTransform;
        public CinemachineCamera cinemachineCamera;

        [Header("Camera FOV Settings")]
        public float idleFOV = 75f;
        public float movingFOV = 80f;
        private float _currentTargetFOV; // 부드러운 전환을 위한 목표 FOV
        private float _fovChangeSpeed = 10f; // FOV 변경 속도
        public GameObject speedParticle;

        // 이동 관련 변수
        public Vector2 MoveInput { get; private set; }
        private Vector3 _lastGroundedPosition;

        public PlayerUIManager PlayerUIComponent { get; private set; } // UI 매니저 인스턴스
        public MyInputActions PlayerInput { get; private set; }
        public CharacterController CharacterControllerComponent { get; private set; } // 상태 클래스에서 접근 가능하도록
        public PlayerAnimator PlayerAnimatorComponent { get; private set; } // 상태 클래스에서 접근 가능하도록
        // private Collider[] _overlapResults = new Collider[5]; // OverlapBoxNonAlloc 결과 저장용

        // --- 상태 패턴 관련 ---
        private PlayerStateEntity _currentState = null;
        private PlayerStateEntity[] _states; // 상태 인스턴스 배열 (상태 전환 시 사용)
        public PlayerState CurrentStateType { get; private set; } // 애니메이터나 UI 등에서 현재 상태 종류를 쉽게 알 수 있도록

        // --- 내부 요청 플래그 (상태 클래스에서 사용) ---
        public bool JumpRequested { get; set; } = false;
        public bool CrouchActive { get; set; } = false; // 슬라이드 버튼 토글 상태
        public bool IsVaultingInternal { get; set; } = false; // 뛰어넘기 중인지 여부 (VaultingState에서 관리)

        [Header("Movement")]
        public Vector3 CurrentSlidingVelocity; // 슬라이딩 중일 때의 월드 속도 벡터 (SlidingState에서 관리)
        public float moveSpeed = 5f;
        public float jumpPower = 5.0f;
        public float slideInitialSpeedMultiplier = 1.5f;
        public float slideDeceleration = 2.0f;
        public float slidingColliderHeight = 0.5f;
        public float StandingColliderHeight { get; private set; }
        public float StandingColliderCenterY { get; private set; }

        [Header("Mouse and Camera Settings")]
        public float mouseSensitivity = 2.0f;
        public float pitchMin = -85f;
        public float pitchMax = 85f;
        private float currentPitch = 0.0f;

        public float VerticalVelocity { get; set; } // 수직 속도 (상태 클래스에서 관리)

        [Header("Vaulting")]
        public float vaultCheckDistance = 1.5f;
        public float canVaultHeightRatio = 0.7f;
        public int vaultLayerMask = 6;
        public LayerMask vaultableLayers;
        public int OriginalPlayerLayer { get; private set; }
        public Vector3 VaultStartPosition { get; set; }
        public Vector3 VaultUpPosition { get; set; }
        public Vector3 VaultEndPosition { get; set; }
        public float VaultStartTime { get; set; }
        public float maxVaultableDepth = 1.0f;  // 장애물 뛰어넘을 최대 길이

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

        [Header("Wall Climb Settings")] // 벽 타기 설정
        public float wallClimbCheckDistance = 0.8f; // 벽 감지 거리
        public float minWallClimbHeight = 0.5f;    // 최소 벽 타기 높이 (발 기준)
        public float maxWallClimbHeight = 1.8f;    // 최대 벽 타기 높이 (발 기준)
        public float wallClimbLedgeOffset = 0.5f;  // 벽을 오른 후 앞쪽으로 이동할 거리

        [Header("Attack")]
        public float attackRange = 1.5f;
        public float attackPower = 10f;
        public float attackCooldown = 1f;
        public float attackMinChargeTime = 0.5f;
        public float attackMaxChargeTime = 2f;
        private float attackChargeTime = 0f;
        private bool isAttackCharging = false;

        private readonly Collider[] _attackOverlapResults = new Collider[30]; // OverlapSphereNonAlloc 결과 저장용

        [Header("Stat")]
        [SerializeField]
        private int health;
        public int Health
        {
            get => health;
            set
            {
                if (value < health)
                {
                    if (invincibilityTimer > 0f) return;
                    // 체력이 감소할 때 무적 시간 초기화 및 재생 대기 시간 초기화
                    invincibilityTimer = invincibilityDuration;
                    regenerationTimer = regenerationCooldown;
                }

                PlayerUIComponent.PlayerHpChanged(value, health);
                health = Mathf.Clamp(value, 0, maxHealth);

                if (health <= 0)
                {
                    PlayerUIComponent.EndGame(false); // 게임 오버 처리
                }
            }
        }
        public int maxHealth = 100;
        public int warningHp = 30;
        public int enemyDamage = 10;
        public float regenerationCooldown = 5f; // 재생 대기 시간
        public int regenerationAmount = 1; // 재생량
        internal float regenerationTimer = 0f; // 재생 타이머
        public float invincibilityDuration = 0.5f; // 무적 시간
        private float invincibilityTimer = 0f; // 무적 타이머
        #endregion

        #region Unity Methods
        void Start()
        {

            health = maxHealth;

            // 상태 인스턴스 생성
            _states = new PlayerStateEntity[]
            {
                new IdleState(this),
                new MovingState(this),
                new JumpingState(this),
                new FallingState(this),
                new SlidingState(this),
                new VaultingState(this),
                new ClimbingUpState(this),
            };

            OriginalPlayerLayer = gameObject.layer;
            StandingColliderHeight = CharacterControllerComponent.height;
            StandingColliderCenterY = CharacterControllerComponent.center.y;

            // 초기 상태 설정
            TransitionToState(PlayerState.Idle);

            PlayerInput.Disable();
            FadeSystem.StartFadeOut(1.5f, () => PlayerInput.Enable());
        }


        void Update()
        {
            // 현재 상태의 Execute 메서드 호출
            _currentState?.Execute();

            if (CharacterControllerComponent.isGrounded)
            {
                // 플레이어가 땅에 닿았을 때, 밟고 있는 지면의 레이어를 확인
                if (Physics.Raycast(transform.position + CharacterControllerComponent.center, Vector3.down, out RaycastHit hitInfo, CharacterControllerComponent.height / 2f + CharacterControllerComponent.skinWidth + 0.1f))
                {
                    // vaultableLayers에 해당 레이어가 포함되어 있는지 확인 (비트마스크 연산)
                    if ((vaultableLayers.value & (1 << hitInfo.collider.gameObject.layer)) > 0)
                    {
                        _lastGroundedPosition = transform.position; // 마지막 착지 위치 업데이트
                    }
                }
            }

            if (transform.position.y < -10f)
            {
                // 플레이어가 너무 아래로 떨어지면 초기 위치로 리셋
                transform.position = _lastGroundedPosition;
                VerticalVelocity = 0f; // 수직 속도 초기화
                Health -= enemyDamage; // 체력 감소
                TransitionToState(PlayerState.Idle); // Idle 상태로 전환

                JumpRequested = false;
                CrouchActive = false; // 웅크리기 상태 초기화
            }

            if (isAttackCharging && attackChargeTime < attackMaxChargeTime)
            {
                attackChargeTime += Time.deltaTime;
                PlayerUIComponent.SetSkillBarCharge(attackChargeTime / attackMaxChargeTime);
            }

            if (Health < maxHealth && regenerationTimer <= 0f)
            {
                // 체력이 재생 대기 시간 이상으로 감소했을 때 재생 시작
                Health = Mathf.Min(maxHealth, Health + regenerationAmount);
                // Debug.Log($"Health regenerated to {Health}");
            }
            else if (regenerationTimer > 0f)
            {
                regenerationTimer -= Time.deltaTime;
            }

            if (invincibilityTimer > 0f)
            {
                invincibilityTimer -= Time.deltaTime;
            }

            // 부드러운 FOV 변경 (선택적)
            if (Mathf.Abs(cinemachineCamera.Lens.FieldOfView - _currentTargetFOV) > 0.01f)
            {
                cinemachineCamera.Lens.FieldOfView = Mathf.Lerp(cinemachineCamera.Lens.FieldOfView, _currentTargetFOV, Time.deltaTime * _fovChangeSpeed);
            }
        }

        void OnEnable()
        {
            // 이벤트 구독
            PlayerInput.Player.Move.performed += OnMoveInput;
            PlayerInput.Player.Move.canceled += OnMoveInput;
            PlayerInput.Player.Look.performed += OnLook;
            PlayerInput.Player.Jump.performed += OnJumpInput;
            PlayerInput.Player.Crouch.performed += OnCrouchInput;
            PlayerInput.Player.Attack.performed += OnAttackInput;
            PlayerInput.Player.Attack.canceled += OnAttackInput;
            PlayerInput.Player.Menu.performed += OnMenuInput;
            PlayerInput.Enable(); // 입력 활성화

            // 마우스 설정
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void OnDisable()
        {
            // PlayerInput.Disable();

            PlayerInput.Player.Move.performed -= OnMoveInput;
            PlayerInput.Player.Move.canceled -= OnMoveInput;
            PlayerInput.Player.Look.performed -= OnLook;
            PlayerInput.Player.Jump.performed -= OnJumpInput;
            PlayerInput.Player.Crouch.performed -= OnCrouchInput;
            PlayerInput.Player.Attack.performed -= OnAttackInput;
            PlayerInput.Player.Attack.canceled -= OnAttackInput;

            // 마우스 설정 초기화
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }


        public void OnDestroy()
        {
            PlayerInput.Disable();
            PlayerInput.Player.Menu.performed -= OnMenuInput;
            PlayerInput.Dispose();
            PlayerInput = null;
            if (Instance == this)
                Instance = null;
        }
        #endregion

        #region State Management
        public void TransitionToState(PlayerState stateType)
        {
            // 이전 상태 Exit
            _currentState?.Exit();

            _currentState = _states[(int)stateType];
            CurrentStateType = stateType;
            _currentState.Enter(); // 새 상태의 Enter 호출
        }
        #endregion

        #region Input Callbacks
        void OnMenuInput(InputAction.CallbackContext callback)
        {
            if (callback.performed)
            {
                // 메뉴 버튼이 눌렸을 때
                MenuManager.SetMenuActive(!MenuManager.Instance.IsMenuActive);   
            }
        }
        void OnMoveInput(InputAction.CallbackContext callback)
        {
            MoveInput = callback.ReadValue<Vector2>();
        }

        // OnMove는 SetupInputActions에서 직접 MoveInput 업데이트
        void OnLook(InputAction.CallbackContext callback)
        {
            Vector2 mouseDelta = callback.ReadValue<Vector2>();
            float mouseX = mouseDelta.x * mouseSensitivity;
            float mouseY = mouseDelta.y * mouseSensitivity;

            transform.Rotate(Vector3.up * mouseX);

            currentPitch -= mouseY;
            currentPitch = Mathf.Clamp(currentPitch, pitchMin, pitchMax);
            head.localRotation = Quaternion.Euler(currentPitch, 0, 0);
            cameraTransform.localRotation = Quaternion.Euler(currentPitch, 0, 0);
        }

        void OnJumpInput(InputAction.CallbackContext callback)
        {
            if (callback.performed)
            {
                JumpRequested = true;
            }
        }

        void OnCrouchInput(InputAction.CallbackContext callback)
        {
            if (callback.performed)
            {
                CrouchActive = !CrouchActive;
            }
        }

        void OnAttackInput(InputAction.CallbackContext callback)
        {
            if (PlayerUIComponent.isCooldownActive || (callback.canceled && !isAttackCharging))
            {
                Debug.Log("Attack input ignored due to cooldown.");
                return; // 공격 입력이 쿨타임 중이면 무시
            }

            if (callback.performed)
            {
                isAttackCharging = true;
                attackChargeTime = 0f;
                Debug.Log("Attack Input Started");
            }
            else if (callback.canceled)
            {
                isAttackCharging = false;
                if (attackChargeTime >= attackMinChargeTime)
                {
                    // Perform attack logic here
                    Debug.Log("Attack performed with charge time: " + attackChargeTime);
                    PlayerAnimatorComponent.TriggerAttack();

                    var hitCount = Physics.OverlapSphereNonAlloc(transform.position, attackRange * attackChargeTime, _attackOverlapResults, LayerMask.GetMask("Enemy"));

                    for (int i = 0; i < hitCount; i++)
                    {
                        Collider hitCollider = _attackOverlapResults[i];
                        if (hitCollider.TryGetComponent(out EnemyController enemyController))
                        {
                            // 플레이어 -> 적 방향으로 힘 가하기
                            Vector3 direction = (hitCollider.transform.position - transform.position).normalized;
                            // 약간 윗방향으로 올리기
                            direction.y += 0.5f;
                            enemyController.Impact(attackChargeTime * attackPower * direction);
                        }
                    }
                }
                else
                {
                    Debug.Log("Attack too weak, not performed");
                }
                PlayerUIComponent.SetSkillBarCooldown(attackCooldown);
                attackChargeTime = 0f;
            }
        }
        #endregion

        #region Methods

        public bool TryGetObstacleTopSurface(Vector3 front, out float topY)
        {
            topY = 0f;
            Vector3 topRayCheckOrigin = front + Vector3.up * (StandingColliderHeight + 0.1f) + transform.forward * 0.05f;
            Debug.DrawRay(topRayCheckOrigin, Vector3.down * (StandingColliderHeight + 0.2f), Color.magenta, 0.5f);

            if (Physics.Raycast(topRayCheckOrigin, Vector3.down, out RaycastHit topSurfaceHit, StandingColliderHeight + 0.2f, vaultableLayers))
            {
                topY = topSurfaceHit.point.y;
                return true;
            }
            else
            {
                // Debug.LogWarning("Vault: Could not determine obstacle's top surface. Vault aborted.");
                return false;
            }
        }

        public float CalculateObstacleDepth(RaycastHit hitInfo)
        {
            Bounds obstacleBounds = hitInfo.collider.bounds;
            return Mathf.Abs(Vector3.Dot(obstacleBounds.extents, transform.forward.normalized)) * 2f;
        }

        public void CalculateDynamicVaultParameters(float depth, float height)
        {
            CurrentVaultDuration = baseVaultDuration + (depth * vaultDurationPerMeterDepth) + (height * vaultDurationPerMeterHeight);
            CurrentVaultDuration = Mathf.Clamp(CurrentVaultDuration, minVaultDuration, maxVaultDuration);
            CurrentVaultJumpHeight = minVaultClearance + (height * vaultHeightMultiplier);
        }

        public void ChangeViewAndCollider(bool isSlidingView)
        {
            if (isSlidingView)
            {
                cinemachineCamera.Follow = head;
                CharacterControllerComponent.height = slidingColliderHeight;
                CharacterControllerComponent.center = new Vector3(0, slidingColliderHeight / 2, 0);
            }
            else
            {
                cinemachineCamera.Follow = cameraTransform;
                CharacterControllerComponent.height = StandingColliderHeight;
                CharacterControllerComponent.center = new Vector3(0, StandingColliderCenterY, 0);
            }
        }

        public void EnemyIsDead()
        {
            PlayerUIComponent.AddEnemyDeadScore();
        }

        public void SetCameraFOV(float targetFOV, bool immediate = false)
        {
            _currentTargetFOV = targetFOV;
            if (immediate)
            {
                cinemachineCamera.Lens.FieldOfView = targetFOV;
            }

            if (targetFOV == movingFOV)
            {
                speedParticle.SetActive(true);
            }
            else
            {
                speedParticle.SetActive(false);
            }
        }
        #endregion
    }
}