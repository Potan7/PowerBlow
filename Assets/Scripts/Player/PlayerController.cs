using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using Player.State;

namespace Player
{
    public class PlayerController : MonoBehaviour
    {
        #region Variables
        // -- 카메라 관련 변수 --
        [Header("Camera and Reference")]
        public Transform head;
        public Transform cameraTransform;
        public CinemachineCamera cinemachineCamera;

        // 이동 관련 변수
        public Vector2 MoveInput { get; private set; } // 외부에서 읽기 전용으로 변경

        public MyInputActions PlayerInput { get; private set; }
        public CharacterController CharacterControllerComponent { get; private set; } // 상태 클래스에서 접근 가능하도록
        public PlayerAnimator PlayerAnimatorComponent { get; private set; } // 상태 클래스에서 접근 가능하도록

        // --- 상태 패턴 관련 ---
        private IPlayerState _currentState;
        public IdleState IdleState { get; private set; }
        public MovingState MovingState { get; private set; }
        public JumpingState JumpingState { get; private set; }
        public FallingState FallingState { get; private set; }
        public SlidingState SlidingState { get; private set; }
        public VaultingState VaultingState { get; private set; }
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
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            CharacterControllerComponent = GetComponent<CharacterController>();
            PlayerAnimatorComponent = GetComponentInChildren<PlayerAnimator>(); // PlayerAnimator 참조 방식에 따라 수정

            // 상태 인스턴스 생성
            IdleState = new IdleState(this);
            MovingState = new MovingState(this);
            JumpingState = new JumpingState(this);
            FallingState = new FallingState(this);
            SlidingState = new SlidingState(this);
            VaultingState = new VaultingState(this);

            OriginalPlayerLayer = gameObject.layer;
            StandingColliderHeight = CharacterControllerComponent.height;
            StandingColliderCenterY = CharacterControllerComponent.center.y;

            PlayerInput = new MyInputActions();
            SetupInputActions();
            PlayerInput.Enable();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // 초기 상태 설정
            TransitionToState(PlayerState.Idle);
        }


        void Update()
        {
            // 현재 상태의 Execute 메서드 호출
            _currentState?.Execute();

            // 카메라 회전은 PlayerController에서 계속 처리 (상태와 무관하게)
            // 단, 입력 처리는 OnLook에서 이미 수행됨
        }
        #endregion

        #region Initialization
        void SetupInputActions()
        {
            PlayerInput.Player.Move.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
            PlayerInput.Player.Move.canceled += ctx => MoveInput = Vector2.zero;
            PlayerInput.Player.Look.performed += OnLook; // OnLook은 카메라 직접 제어
            PlayerInput.Player.Jump.performed += OnJumpInput;
            PlayerInput.Player.Crouch.performed += OnCrouchInput;
        }
        #endregion

        #region State Management
        public void TransitionToState(PlayerState stateType)
        {
            // 이전 상태 Exit
            _currentState?.Exit();

            // 콜라이더/카메라 복구 (만약 이전 상태가 슬라이딩이었다면)
            if (CurrentStateType == PlayerState.Sliding && stateType != PlayerState.Sliding)
            {
                ChangeViewAndCollider(false);
            }

            _currentState = GetStateInstance(stateType);
            CurrentStateType = stateType;
            _currentState.Enter(this); // 새 상태의 Enter 호출

            // 콜라이더/카메라 변경 (새 상태가 슬라이딩이라면)
            if (stateType == PlayerState.Sliding)
            {
                ChangeViewAndCollider(true);
            }
        }

        private IPlayerState GetStateInstance(PlayerState stateType)
        {
            return stateType switch
            {
                PlayerState.Idle => IdleState,
                PlayerState.Moving => MovingState,
                PlayerState.Jumping => JumpingState,
                PlayerState.Falling => FallingState,
                PlayerState.Sliding => SlidingState,
                PlayerState.Vaulting => VaultingState,
                _ => throw new ArgumentOutOfRangeException(nameof(stateType), stateType, null)
            };
        }
        #endregion

        #region Vaulting Helpers
        public bool TryGetObstacleTopSurface(RaycastHit frontHit, out float topY)
        {
            topY = 0f;
            Vector3 topRayCheckOrigin = frontHit.point + Vector3.up * (StandingColliderHeight + 0.1f) + transform.forward * 0.05f;
            Debug.DrawRay(topRayCheckOrigin, Vector3.down * (StandingColliderHeight + 0.2f), Color.magenta, 0.5f);

            if (Physics.Raycast(topRayCheckOrigin, Vector3.down, out RaycastHit topSurfaceHit, StandingColliderHeight + 0.2f, vaultableLayers))
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
        #endregion

        #region Methods
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