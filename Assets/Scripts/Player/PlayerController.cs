using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using Player.State;

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
            }
        }
        #endregion

        #region Variables
        // -- 카메라 관련 변수 --
        [Header("Camera and Reference")]
        public Transform head;
        public Transform cameraTransform;
        public CinemachineCamera cinemachineCamera;

        // 이동 관련 변수
        public Vector2 MoveInput { get; private set; }

        public MyInputActions PlayerInput { get; private set; }
        public CharacterController CharacterControllerComponent { get; private set; } // 상태 클래스에서 접근 가능하도록
        public PlayerAnimator PlayerAnimatorComponent { get; private set; } // 상태 클래스에서 접근 가능하도록
        private Collider[] _overlapResults = new Collider[5]; // OverlapBoxNonAlloc 결과 저장용

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

        [Header("Attack")]
        public float attackRange = 1.5f;
        public float attackPower = 10f;
        public float attackCooldown = 1f;
        public float attackMinChargeTime = 0.5f;
        public float attackMaxChargeTime = 2f;
        private float attackChargeTime = 0f;
        private bool isAttackCharging = false;
        private readonly Collider[] _attackOverlapResults = new Collider[20]; // OverlapSphereNonAlloc 결과 저장용
        #endregion

        #region Unity Methods
        void Start()
        {
            CharacterControllerComponent = GetComponent<CharacterController>();
            PlayerAnimatorComponent = GetComponentInChildren<PlayerAnimator>();

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
            PlayerInput = new MyInputActions();

            Initialization();

            PlayerInput.Enable();
            // 마우스 설정
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // 초기 상태 설정
            TransitionToState(PlayerState.Idle);
        }


        void Update()
        {
            // 현재 상태의 Execute 메서드 호출
            _currentState?.Execute();

            if (isAttackCharging && attackChargeTime < attackMaxChargeTime)
            {
                attackChargeTime += Time.deltaTime;
            }
        }
        #endregion

        #region Initialization
        void Initialization()
        {
            PlayerInput.Player.Move.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
            PlayerInput.Player.Move.canceled += ctx => MoveInput = Vector2.zero;
            PlayerInput.Player.Look.performed += OnLook; // OnLook은 카메라 직접 제어
            PlayerInput.Player.Jump.performed += OnJumpInput;
            PlayerInput.Player.Crouch.performed += OnCrouchInput;
            PlayerInput.Player.Attack.performed += OnAttackInput;
            PlayerInput.Player.Attack.canceled += OnAttackInput;

            OriginalPlayerLayer = gameObject.layer;
            StandingColliderHeight = CharacterControllerComponent.height;
            StandingColliderCenterY = CharacterControllerComponent.center.y;
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

                    var hitCount = Physics.OverlapSphereNonAlloc(transform.position, attackRange * attackChargeTime, _attackOverlapResults);

                    for (int i = 0; i < hitCount; i++)
                    {
                        Collider hitCollider = _attackOverlapResults[i];
                        if (hitCollider.CompareTag("Enemy"))
                        {
                            EnemyController enemyController = hitCollider.GetComponent<EnemyController>();

                            // 플레이어 -> 적 방향으로 힘 가하기
                            Vector3 direction = (hitCollider.   transform.position - transform.position).normalized;
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
        #endregion
    }
}