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
        // 플레이어 컨트롤러 싱글톤 인스턴스
        public static PlayerController Instance { get; private set; }
        #endregion

        #region Component References
        // 주요 컴포넌트 참조 변수들
        public CharacterController CharacterControllerComponent { get; private set; }
        public PlayerAnimator PlayerAnimatorComponent { get; private set; }
        public PlayerUIManager PlayerUIComponent { get; private set; }
        public PlayerAudioManager PlayerAudioComponent { get; private set; }
        public PlayerInputSystem InputManagerComponent { get; private set; }
        public PlayerCameraController CameraControllerComponent { get; private set; }
        public PlayerAttackController AttackControllerComponent { get; private set; }
        public PlayerStatsManager StatsManagerComponent { get; private set; }
        #endregion

        #region Variables

        // 현재 플레이어 상태
        private PlayerStateEntity _currentState = null;
        // 모든 플레이어 상태 배열
        private PlayerStateEntity[] _states;
        // 현재 플레이어 상태 타입 (enum)
        public PlayerState CurrentStateType { get; private set; }

        // 점프 요청 플래그
        public bool JumpRequested { get; set; } = false;
        // 웅크리기 활성화 플래그
        public bool CrouchActive { get; set; } = false;
        // 볼팅 중인지 여부 (내부용)
        public bool IsVaultingInternal { get; set; } = false;

        [Header("Camera Settings")]
        // 카메라 관련 설정값
        public Transform head; // 플레이어 머리 트랜스폼
        public Transform cameraTransform; // 카메라 리그 트랜스폼
        public CinemachineCamera cinemachineCamera; // 시네머신 카메라
        public GameObject speedParticle; // 속도 파티클 효과
        public float pitchMin = -85f; // 카메라 최소 Pitch 각도
        public float pitchMax = 85f; // 카메라 최대 Pitch 각도
        public float idleFOV = 75f; // 기본 시야각
        public float movingFOV = 80f; // 이동 시 시야각
        public float fovChangeSpeed = 10f; // 시야각 변경 속도

        [Header("Movement Settings")]
        // 이동 관련 설정값
        public Vector2 MoveInput { get; private set; } // 이동 입력 값
        private Vector3 _lastGroundedPosition; // 마지막으로 땅에 있었던 위치 (리스폰용)
        public float moveSpeed = 5f;    // 기본 이동 속도
        public float acceleration = 10f; // 가속도
        public float deceleration = 15f; // 감속도 (지상)
        public float airDeceleration = 5f; // 공중 감속도 (입력 없을 시)
        public float airControlAcceleration = 8f; // 공중에서 새 입력 방향으로 속도 변경 가속도
        public float airSteeringRate = 5f; // 공중에서 방향 전환 시 회전 속도
        public float currentHorizontalSpeed = 0f; // 현재 수평 속도
        public float rapidTurnRatio = 0.8f; // 급회전 시 속도 감소 비율
        public float jumpPower = 5.0f; // 점프 힘
        public float slideInitialSpeedMultiplier = 1.5f; // 슬라이딩 시작 시 속도 배율
        public float slideDeceleration = 2.0f; // 슬라이딩 감속도
        public float slidingColliderHeight = 0.5f; // 슬라이딩 시 콜라이더 높이
        public float jumpTimeMargin = 0.5f; // 코요테 타임 (점프 유예 시간)
        public float StandingColliderHeight { get; private set; } // 서 있을 때 콜라이더 높이
        public float StandingColliderCenterY { get; private set; } // 서 있을 때 콜라이더 중심 Y 오프셋
        public float VerticalVelocity { get; set; } // 현재 수직 속도
        public float FallingStartTime { get; set; } = 0f; // 낙하 시작 시간 (코요테 타임용)
        public bool isOnSpeedBlock = false; // 스피드 블록 위에 있는지 여부
        public Vector3 lastHorizontalMoveDirection = Vector3.zero; // 마지막 수평 이동 방향
        // private bool _maintainInertiaOnJump = false; // W키 점프 관성 유지 여부 - 제거됨

        [Header("Vaulting")]
        // 볼팅 관련 설정값
        public float vaultCheckDistance = 1.5f; // 볼팅 가능 장애물 감지 거리
        public float canVaultHeightRatio = 0.7f; // 볼팅 가능한 장애물 높이 비율 (플레이어 키 대비)
        public LayerMask vaultableLayers; // 볼팅 가능한 레이어
        public int OriginalPlayerLayer { get; private set; } // 플레이어 원래 레이어
        public Vector3 VaultStartPosition { get; set; } // 볼팅 시작 위치
        public Vector3 VaultUpPosition { get; set; } // 볼팅 중 정점 위치
        public Vector3 VaultEndPosition { get; set; } // 볼팅 종료 위치
        public float VaultStartTime { get; set; } // 볼팅 시작 시간
        public float maxVaultableDepth = 1.0f; // 볼팅 가능한 최대 장애물 깊이
        public int vaultLayerMask = 6; // 볼팅 시 플레이어가 임시로 변경될 레이어

        [Header("Dynamic Vault Parameters")]
        // 동적 볼팅 파라미터 (장애물 크기에 따라 볼팅 시간/높이 조절)
        public float baseVaultDuration = 0.4f; // 기본 볼팅 지속 시간
        public float vaultDurationPerMeterDepth = 0.15f; // 장애물 깊이 1미터당 추가되는 볼팅 시간
        public float vaultDurationPerMeterHeight = 0.1f; // 장애물 높이 1미터당 추가되는 볼팅 시간
        public float minVaultDuration = 0.3f; // 최소 볼팅 지속 시간
        public float maxVaultDuration = 0.8f; // 최대 볼팅 지속 시간
        public float minVaultClearance = 0.15f; // 볼팅 시 최소 장애물 통과 높이
        public float vaultHeightMultiplier = 0.3f; // 볼팅 점프 높이 배율 (장애물 높이 기준)
        public float CurrentVaultDuration { get; set; } // 현재 계산된 볼팅 지속 시간
        public float CurrentVaultJumpHeight { get; set; } // 현재 계산된 볼팅 점프 높이

        [Header("Wall Climb Settings")]
        // 벽 오르기 관련 설정값
        public float wallClimbCheckDistance = 0.8f; // 벽 오르기 감지 거리
        public float minWallClimbHeight = 0.5f; // 최소 벽 오르기 높이
        public float maxWallClimbHeight = 1.8f; // 최대 벽 오르기 높이
        public float wallClimbLedgeOffset = 0.5f; // 벽 오르기 후 착지 시 전방 오프셋

        [Header("Attack Settings")]
        // 공격 관련 설정값
        public float attackRange = 1.5f;
        public float attackPower = 10f;
        public float attackCooldown = 1f;
        public float attackMinChargeTime = 0.5f;
        public float attackMaxChargeTime = 2f;
        public float attackJumpPower = 5f; // 공격 시 위로 튕겨오르는 힘
        public float attackForwardPower = 5f; // 공격 시 앞으로 나아가는 힘
        public bool attackOvercharge = false;

        [Header("Stat Settings")]
        // 스탯 관련 설정값
        public int maxHealth = 100;
        public int warningHp = 30;
        public int fallDamage = 10;
        public float regenerationCooldown = 5f;
        public int regenerationAmount = 1;
        public float invincibilityDuration = 0.5f;
        #endregion

        #region Unity Methods

        // Awake: 싱글톤 인스턴스 설정 및 필수 컴포넌트 초기 참조 할당
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; } // 중복 인스턴스 방지

            // 필수 컴포넌트 가져오기
            CharacterControllerComponent = GetComponent<CharacterController>();
            PlayerAnimatorComponent = GetComponentInChildren<PlayerAnimator>(); // 자식 오브젝트에서 Animator 컴포넌트 검색
            PlayerUIComponent = FindFirstObjectByType<PlayerUIManager>(); // 씬에서 UI 매니저 검색
            PlayerAudioComponent = GetComponentInChildren<PlayerAudioManager>(); // 자식 오브젝트에서 Audio 매니저 검색
            InputManagerComponent = GetComponent<PlayerInputSystem>();
        }

        // Start: 컴포넌트 초기화, 상태 설정, 이벤트 구독
        void Start()
        {
            // 각 컨트롤러 컴포넌트 초기화
            InputManagerComponent.Init(this, CameraControllerComponent, AttackControllerComponent);
            CameraControllerComponent = new PlayerCameraController(this);
            AttackControllerComponent = new PlayerAttackController(this, PlayerAnimatorComponent, PlayerUIComponent, CharacterControllerComponent);
            StatsManagerComponent = new PlayerStatsManager(this, PlayerUIComponent);

            // 플레이어 상태 배열 초기화
            _states = new PlayerStateEntity[]
            {
                new IdleState(this), new MovingState(this), new FallingState(this),
                new SlidingState(this), new VaultingState(this), new ClimbingUpState(this),
            };

            // 초기 플레이어 설정값 저장
            OriginalPlayerLayer = gameObject.layer;
            StandingColliderHeight = CharacterControllerComponent.height;
            StandingColliderCenterY = CharacterControllerComponent.center.y;

            // 카메라 초기 FOV 설정 및 초기 상태 전환
            CameraControllerComponent.SetCameraFOV(idleFOV, true);
            TransitionToState(PlayerState.Idle);
 
            // 입력 이벤트 구독
            InputManagerComponent.MoveEvent += OnMoveInputReceived;
            InputManagerComponent.LookEvent += CameraControllerComponent.HandleLookInput;
            InputManagerComponent.JumpEvent += OnJumpInputReceived;
            InputManagerComponent.CrouchEvent += OnCrouchInputReceived;
            InputManagerComponent.AttackEvent += AttackControllerComponent.HandleAttackInput;
            InputManagerComponent.MenuEvent += OnMenuInputReceived;

            // 게임 시작 시 플레이어 입력 비활성화 후 페이드 아웃 효과와 함께 활성화
            InputManagerComponent.DisablePlayerActions();
            FadeSystem.StartFadeOut(0.5f, () => InputManagerComponent.EnablePlayerActions());
        }

        // Update: 매 프레임 호출, 주요 로직 처리 순서 정의
        void Update()
        {
            _currentState?.Execute(); // 현재 상태의 Execute 로직 실행

            HandleGroundedCheck();    // 지면 감지 및 관련 처리
            ProcessMovementStates();  // 현재 상태에 따른 이동 로직 분기
            ApplyFinalMovement();     // 계산된 이동 값 적용
            UpdatePlayerComponents(); // 다른 플레이어 컴포넌트 업데이트
            CheckFallRespawn();       // 낙하 시 리스폰 처리
        }

        // OnDestroy: 오브젝트 파괴 시 이벤트 구독 해제
        private void OnDestroy()
        {
            if (InputManagerComponent != null)
            {
                InputManagerComponent.MoveEvent -= OnMoveInputReceived;
                InputManagerComponent.LookEvent -= CameraControllerComponent.HandleLookInput;
                InputManagerComponent.JumpEvent -= OnJumpInputReceived;
                InputManagerComponent.CrouchEvent -= OnCrouchInputReceived;
                InputManagerComponent.AttackEvent -= AttackControllerComponent.HandleAttackInput;
                InputManagerComponent.MenuEvent -= OnMenuInputReceived;
            }
            if (Instance == this) Instance = null; // 싱글톤 인스턴스 정리
        }

        #endregion

        #region Movement Logic Sub-methods

        // HandleGroundedCheck: 지면 감지, 스피드 블록 확인, 마지막 지면 위치 업데이트
        private void HandleGroundedCheck()
        {
            if (CharacterControllerComponent.isGrounded) // 캐릭터 컨트롤러가 지면에 닿아있는지 확인
            {
                if (VerticalVelocity < 0) VerticalVelocity = -2f; // 지면에 붙어있도록 수직 속도 조절
                // _maintainInertiaOnJump = false; // 지면에 닿으면 W키 점프 관성 해제 - 지워야함

                isOnSpeedBlock = false; // 스피드 블록 위에 있는지 기본값 false로 초기화
                // 캐릭터 발밑으로 레이캐스트하여 지면 정보 확인
                if (Physics.Raycast(transform.position + CharacterControllerComponent.center, Vector3.down, out RaycastHit groundHitInfo, CharacterControllerComponent.height / 2f + CharacterControllerComponent.skinWidth + 0.2f))
                {
                    if (groundHitInfo.collider.CompareTag("Speed")) // 지면 태그가 "Speed"인지 확인
                    {
                        isOnSpeedBlock = true;
                    }
                    // 볼팅 가능한 레이어에 해당하는 지면이면 마지막 지면 위치 업데이트
                    if ((vaultableLayers.value & (1 << groundHitInfo.collider.gameObject.layer)) > 0)
                    {
                        _lastGroundedPosition = transform.position;
                    }
                }
            }
            else // 공중에 있을 때
            {
                VerticalVelocity += Physics.gravity.y * Time.deltaTime; // 중력 적용
            }
        }

        // ProcessMovementStates: 현재 상태에 따라 적절한 이동 처리 함수 호출
        private void ProcessMovementStates()
        {
            if (CurrentStateType == PlayerState.Sliding)
            {
                HandleSlidingMovement(); // 슬라이딩 상태 이동 처리
            }
            else if (CurrentStateType == PlayerState.Falling)
            {
                HandleAirMovement(); // 낙하(공중) 상태 이동 처리
            }
            else // 지상 상태 (Moving 또는 Idle)
            {
                HandleGroundLocomotion(); // 지상 이동 처리
            }
        }

        // HandleSlidingMovement: 슬라이딩 중 속도 감속 처리
        private void HandleSlidingMovement()
        {
            currentHorizontalSpeed = Mathf.MoveTowards(currentHorizontalSpeed, 0f, slideDeceleration * Time.deltaTime);
            // lastHorizontalMoveDirection은 SlidingState.Enter()에서 설정된 값을 유지
        }

        // HandleAirMovement: 공중 상태에서의 이동 및 관성 처리
        private void HandleAirMovement()
        {
            float targetAirSpeed = moveSpeed * 0.8f; // 공중 목표 속도

            if (MoveInput != Vector2.zero) // 공중에서 이동 입력이 있을 때
            {
                // 입력 방향을 월드 좌표 기준으로 변환 및 정규화
                Vector3 targetInputDirection = transform.TransformDirection(new Vector3(MoveInput.x, 0, MoveInput.y)).normalized;

                if (targetInputDirection.sqrMagnitude > 0.001f) // 유효한 입력 방향일 때
                {
                    // 현재 플레이어의 수평 속도 벡터
                    Vector3 currentHorizontalVelocity = lastHorizontalMoveDirection * currentHorizontalSpeed;

                    // 입력에 따른 목표 수평 속도 벡터
                    Vector3 targetHorizontalVelocity = targetInputDirection * Mathf.Max(targetAirSpeed, currentHorizontalSpeed);

                    // 공중에서 입력 방향으로 속도 변경
                    currentHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, targetHorizontalVelocity, airControlAcceleration * Time.deltaTime);

                    // 변경된 속도 벡터에서 실제 속력과 방향을 다시 추출
                    currentHorizontalSpeed = currentHorizontalVelocity.magnitude;

                    if (currentHorizontalSpeed > 0.01f)
                    {
                        lastHorizontalMoveDirection = currentHorizontalVelocity.normalized;
                    }
                    else
                    {
                        // 속도가 거의 0이 되었고 입력이 있다면, 다음 프레임부터 해당 입력 방향으로 움직일 수 있도록 방향 설정
                        lastHorizontalMoveDirection = targetInputDirection;
                    }
                }
            }
            else // 공중에서 이동 입력이 없을 때
            {
                // 입력이 없으면 공중 감속 적용
                currentHorizontalSpeed = Mathf.MoveTowards(currentHorizontalSpeed, 0f, airDeceleration * Time.deltaTime);
            }

            // 속도가 매우 작으면 0으로 처리
            if (currentHorizontalSpeed < 0.01f)
            {
                currentHorizontalSpeed = 0f;
                // 입력이 없고 속도가 0이 되면, lastHorizontalMoveDirection은 이전 값을 유지
            }
        }

        // HandleGroundLocomotion: 지상에서의 이동 처리 (가속, 감속, 방향 전환)
        private void HandleGroundLocomotion()
        {
            float targetSpeedOnGround; // 목표 지상 속도

            if (MoveInput != Vector2.zero) // 이동 입력이 있을 때
            {
                // W키(앞으로) 입력 여부에 따라 기본 속도 차등 적용
                float baseSpeedForInputDirection = (MoveInput.y > 0) ? moveSpeed : moveSpeed * 0.8f;
                if (isOnSpeedBlock) baseSpeedForInputDirection *= 3f; // 스피드 블록 위면 속도 증가

                targetSpeedOnGround = baseSpeedForInputDirection;
                if (CameraControllerComponent.IsRapidTurn) targetSpeedOnGround *= rapidTurnRatio; // 급회전 시 속도 감소

                // 입력 방향을 월드 좌표 기준으로 변환 및 정규화
                Vector3 currentInputWorldDirection = transform.TransformDirection(new Vector3(MoveInput.x, 0, MoveInput.y)).normalized;
                lastHorizontalMoveDirection = currentInputWorldDirection; // 마지막 이동 방향 업데이트

                // 멈췄다가 다시 움직이기 시작할 때 최소 속도 보정
                if (currentHorizontalSpeed < 0.1f && targetSpeedOnGround > 0.01f)
                    currentHorizontalSpeed = Mathf.Max(currentHorizontalSpeed, 0.1f);

                // 목표 속도에 따라 가속 또는 감속 적용
                float actualAccelerationRate = (targetSpeedOnGround > currentHorizontalSpeed) ? acceleration : deceleration;
                currentHorizontalSpeed = Mathf.MoveTowards(currentHorizontalSpeed, targetSpeedOnGround, actualAccelerationRate * Time.deltaTime);
            }
            else // 지상, 입력 없음
            {
                targetSpeedOnGround = 0f; // 목표 속도는 0
                currentHorizontalSpeed = Mathf.MoveTowards(currentHorizontalSpeed, targetSpeedOnGround, deceleration * Time.deltaTime); // 감속
                // _maintainInertiaOnJump = false; // 지상에서 입력 없으면 W키 관성 해제 - 지워야함
            }
        }

        // ApplyFinalMovement: 계산된 수평/수직 속도를 캐릭터 컨트롤러에 최종 적용
        private void ApplyFinalMovement()
        {
            // 매우 작은 속도는 0으로 처리하여 미끄러짐 방지
            if (currentHorizontalSpeed < 0.01f)
            {
                currentHorizontalSpeed = 0f;
            }

            // 최종 수평 이동 벡터 계산
            Vector3 horizontalMovement = lastHorizontalMoveDirection * currentHorizontalSpeed;

            // 속도에 따라 스피드 파티클 활성화/비활성화
            if (speedParticle != null)
            {
                speedParticle.SetActive(currentHorizontalSpeed > 0.3f);
            }

            // 볼팅이나 벽 오르기 중이 아닐 때만 CharacterController.Move 호출
            if (CurrentStateType != PlayerState.Vaulting && CurrentStateType != PlayerState.ClimbingUp)
            {
                CharacterControllerComponent.Move((horizontalMovement + Vector3.up * VerticalVelocity) * Time.deltaTime);
            }
        }

        // UpdatePlayerComponents: 다른 플레이어 관련 컴포넌트들의 Update 함수 호출
        private void UpdatePlayerComponents()
        {
            CameraControllerComponent.UpdateFOV(); // 카메라 FOV 업데이트
            AttackControllerComponent.UpdateAttackCharge(); // 공격 차지 상태 업데이트
            StatsManagerComponent.UpdateTimers(); // 스탯 관련 타이머 업데이트 (체력 재생 등)
        }

        // CheckFallRespawn: 플레이어가 특정 높이 이하로 떨어졌을 때 리스폰 처리
        private void CheckFallRespawn()
        {
            if (transform.position.y < -5f) // 임의의 낙하 한계 높이
            {
                transform.position = _lastGroundedPosition; // 마지막 지상 위치로 이동
                VerticalVelocity = 0f; // 수직 속도 초기화
                currentHorizontalSpeed = 0f; // 수평 속도 초기화
                StatsManagerComponent.TakeDamage(fallDamage); // 낙하 데미지 적용
                TransitionToState(PlayerState.Idle); // Idle 상태로 전환
                JumpRequested = false; // 점프 요청 초기화
                CrouchActive = false; // 웅크리기 상태 초기화
            }
        }

        #endregion

        #region State Management
        // TransitionToState: 플레이어 상태 전환 로직
        public void TransitionToState(PlayerState newState)
        {
            var oldState = CurrentStateType; // 이전 상태 저장

            _currentState?.Exit(); // 현재 상태의 Exit 로직 실행
            _currentState = _states[(int)newState]; // 새 상태로 변경
            CurrentStateType = newState; // 현재 상태 타입 업데이트
            _currentState.Enter(); // 새 상태의 Enter 로직 실행

            // 특정 상태 전환 시 추가 로직 (예: 낙하 시작 시간 기록)
            if ((oldState == PlayerState.Sliding || oldState == PlayerState.Moving) && newState == PlayerState.Falling)
            {
                FallingStartTime = Time.time;
            }
        }
        #endregion

        #region Input Event Handlers
        // OnMoveInputReceived: 이동 입력 이벤트 처리
        private void OnMoveInputReceived(Vector2 moveData)
        {
            MoveInput = moveData; // 입력 값 저장
        }

        // OnJumpInputReceived: 점프 입력 이벤트 처리
        private void OnJumpInputReceived()
        {
            if (CurrentStateType == PlayerState.Sliding) // 슬라이딩 중 점프는 상태에게 위임
            {
                JumpRequested = true;
                return;
            }

            // 일반 점프 가능 조건: 지상에 있고 Idle 또는 Moving 상태
            bool canStandardJump = CharacterControllerComponent.isGrounded &&
                                   (CurrentStateType == PlayerState.Idle || CurrentStateType == PlayerState.Moving);
            // 코요테 타임 점프 가능 조건: 낙하 중이고 낙하 시작 후 유예 시간 이내
            bool canCoyoteJump = CurrentStateType == PlayerState.Falling && Time.time - FallingStartTime < jumpTimeMargin;

            if (canStandardJump || canCoyoteJump) // 점프 가능하면
            {
                // _maintainInertiaOnJump = (MoveInput.y > 0); // W키(앞으로) 누르고 점프 시 관성 유지 플래그 설정 - 지워야함
                DoJump(); // 점프 실행
            }
        }

        // OnCrouchInputReceived: 웅크리기 입력 이벤트 처리
        private void OnCrouchInputReceived()
        {
            CrouchActive = !CrouchActive; // 웅크리기 상태 토글
            // _maintainInertiaOnJump = false; // 웅크리면 W키 관성 해제 - 지워야함
        }

        // OnMenuInputReceived: 메뉴 입력 이벤트 처리
        private void OnMenuInputReceived()
        {
            if (MenuManager.Instance != null)
                MenuManager.SetMenuActive(!MenuManager.Instance.IsMenuActive); // 메뉴 활성화/비활성화 토글
        }
        #endregion

        #region Methods
        // TryGetObstacleTopSurface: 장애물 상단 표면 높이 감지 시도
        public bool TryGetObstacleTopSurface(Vector3 front, out float topY)
        {
            topY = 0f;
            // 장애물 상단 감지를 위한 레이캐스트 시작점 계산
            Vector3 topRayCheckOrigin = front + Vector3.up * (StandingColliderHeight + 0.1f) + transform.forward * 0.05f;
            // 아래 방향으로 레이캐스트하여 장애물 상단 표면 감지
            if (Physics.Raycast(topRayCheckOrigin, Vector3.down, out RaycastHit topSurfaceHit, StandingColliderHeight + 0.2f, vaultableLayers))
            {
                topY = topSurfaceHit.point.y; // 감지된 표면의 Y 좌표 반환
                return true;
            }
            return false;
        }

        // DoJump: 실제 점프 실행 로직
        public void DoJump()
        {
            JumpRequested = false; // 점프 요청 플래그 리셋
            VerticalVelocity = jumpPower; // 수직 속도에 점프 힘 적용
            PlayerAnimatorComponent.TriggerJump(); // 점프 애니메이션 트리거
            TransitionToState(PlayerState.Falling); // Falling 상태로 전환
            FallingStartTime = 0; // 코요테 타임 사용 후 리셋 (중복 코요테 점프 방지)
            PlayerAudioComponent.PlaySound(PlayerAudioManager.PlayerAudioType.Jump); // 점프 소리 재생
        }

        // CalculateObstacleDepth: 장애물의 깊이 계산
        public float CalculateObstacleDepth(RaycastHit hitInfo)
        {
            if (hitInfo.collider == null) return 0f;
            Bounds obstacleBounds = hitInfo.collider.bounds; // 장애물 바운딩 박스
            // 플레이어 정면 방향 기준으로 장애물 깊이 계산
            return Mathf.Abs(Vector3.Dot(obstacleBounds.extents, transform.forward.normalized)) * 2f;
        }

        // CalculateDynamicVaultParameters: 장애물 크기에 따라 동적 볼팅 파라미터 계산
        public void CalculateDynamicVaultParameters(float depth, float height)
        {
            // 장애물 깊이와 높이에 따라 볼팅 지속 시간 및 점프 높이 계산
            CurrentVaultDuration = baseVaultDuration + (depth * vaultDurationPerMeterDepth) + (height * vaultDurationPerMeterHeight);
            CurrentVaultDuration = Mathf.Clamp(CurrentVaultDuration, minVaultDuration, maxVaultDuration); // 최소/최대값 제한
            CurrentVaultJumpHeight = minVaultClearance + (height * vaultHeightMultiplier);
        }

        // ChangeViewAndCollider: 슬라이딩 상태에 따라 카메라 타겟 및 콜라이더 크기 변경
        public void ChangeViewAndCollider(bool isSlidingView)
        {
            CameraControllerComponent.ChangeCameraFollowTarget(isSlidingView); // 카메라 팔로우 타겟 변경
            if (isSlidingView) // 슬라이딩 뷰일 때
            {
                CharacterControllerComponent.height = slidingColliderHeight; // 콜라이더 높이 변경
                CharacterControllerComponent.center = new Vector3(0, slidingColliderHeight / 2, 0); // 콜라이더 중심 변경
            }
            else // 기본 뷰일 때
            {
                CharacterControllerComponent.height = StandingColliderHeight;
                CharacterControllerComponent.center = new Vector3(0, StandingColliderCenterY, 0);
            }
        }

        // EnemyIsDead: 적 사망 시 호출 (UI 스코어 업데이트 등)
        public void EnemyIsDead()
        {
            PlayerUIComponent.AddEnemyDeadScore();
        }

        // SetCameraFOV: 카메라 시야각 설정 (PlayerCameraController 호출)
        public void SetCameraFOV(float targetFOV, bool immediate = false)
        {
            CameraControllerComponent.SetCameraFOV(targetFOV, immediate);
        }
        #endregion
    }
}