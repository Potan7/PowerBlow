using UnityEngine;

namespace Player.State
{
    public class MovingState : PlayerStateEntity
    {
        // private const float VaultMargin = 0.1f; // VaultEndPosition 계산 시 장애물로부터의 여유 공간
        private const float ClimbUpClearance = 0.5f; // 기어오르기 시 장애물 위에서의 전방 여유 공간

        private float _footstepTimer = 0f; // 발소리 타이머
        private const float FootstepInterval = 0.4f; // 발소리 간격 (초 단위)

        public MovingState(PlayerController player) : base(player)
        {
        }

        public override void Enter()
        {
            // 이동 애니메이션 활성화
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Moving, true);
            _player.JumpRequested = false; // Moving 상태 진입 시 점프 요청 초기화

            _player.SetCameraFOV(_player.movingFOV); // Moving 상태의 카메라 FOV 설정
        }

        public override void Execute()
        {
            // 1. 지상 상태 유지 및 중력 처리
            if (!_player.CharacterControllerComponent.isGrounded)
            {
                _player.TransitionToState(PlayerState.Falling);
                return;
            }

            // 2. 이동 입력 중단 감지: 이동 입력이 없고 땅에 있다면 IdleState로 전환
            if (_player.MoveInput == Vector2.zero && _player.CharacterControllerComponent.isGrounded)
            {
                _player.TransitionToState(PlayerState.Idle);
                return;
            }

            // 5. 뛰어넘기 또는 기어오르기 시도
            // 전방에 장애물이 있고, 플레이어가 충분히 가까이 있으며, 점프 입력이 있었을 때
            // 또는 특정 조건 (예: 계속 앞으로 이동 중)에서 자동으로 시도할 수도 있음
            // 여기서는 점프 입력과 함께 전방 장애물 감지 시 시도한다고 가정

            if (TryAttemptVaultOrClimb())
            {
                // TryAttemptVaultOrClimb 내부에서 상태 전환이 일어나므로 여기서 return
                return;
            }

            // 4. 슬라이드 요청 감지: 웅크리기 버튼이 활성화되어 있고, 땅에 있으며, 이동 입력이 있다면 SlidingState로 전환
            if (_player.CrouchActive && _player.CharacterControllerComponent.isGrounded && _player.MoveInput != Vector2.zero)
            {
                _player.TransitionToState(PlayerState.Sliding);
                return;
            }

            // 일반 이동 로직
            // Vector3 moveDirection = _player.transform.TransformDirection(new Vector3(_player.MoveInput.x, 0, _player.MoveInput.y)).normalized;
            // Vector3 horizontalMovement = _player.moveSpeed * Time.deltaTime * moveDirection;

            // Vector3 verticalMovement = _player.VerticalVelocity * Time.deltaTime * Vector3.up;
            // _player.CharacterControllerComponent.Move(horizontalMovement + verticalMovement);


            // 상태 전환 로직
            if (!_player.CharacterControllerComponent.isGrounded && _player.VerticalVelocity < -0.1f) // 떨어지기 시작하면
            {
                _player.TransitionToState(PlayerState.Falling);
                return;
            }

            if (_player.CrouchActive && _player.CharacterControllerComponent.isGrounded && _player.CharacterControllerComponent.velocity.magnitude > 0.1f)
            {
                _player.TransitionToState(PlayerState.Sliding);
                return;
            }

            if (_player.MoveInput == Vector2.zero && _player.CharacterControllerComponent.isGrounded)
            {
                _player.TransitionToState(PlayerState.Idle);
                return;
            }

            // 애니메이션 파라미터 업데이트
            _player.PlayerAnimatorComponent.SetDirection(_player.MoveInput);

            if (_footstepTimer <= 0f)
            {
                // 발소리 재생
                _player.PlayerAudioComponent.PlaySound(PlayerAudioManager.PlayerAudioType.Footstep);
                _footstepTimer = FootstepInterval; // 타이머 초기화
            }
            else
            {
                _footstepTimer -= Time.deltaTime; // 타이머 감소
            }
        }


        private bool TryAttemptVaultOrClimb()
        {
            Vector3 rayOrigin = _player.transform.position + Vector3.up * 0.5f + (_player.transform.forward * _player.CharacterControllerComponent.radius);
            Debug.DrawRay(rayOrigin, _player.transform.forward * _player.vaultCheckDistance, Color.blue, 1f);

            if (Physics.Raycast(rayOrigin, _player.transform.forward, out RaycastHit hitInfo, _player.vaultCheckDistance, _player.vaultableLayers))
            {
                if (hitInfo.collider.CompareTag("Ground")) return false; // 바닥은 무시

                float obstacleHeight = hitInfo.point.y - _player.transform.position.y; // 대략적인 장애물 높이 (발밑 기준)

                if (obstacleHeight < _player.CharacterControllerComponent.height * _player.canVaultHeightRatio && obstacleHeight > 0.1f) // 너무 낮거나 높지 않은 장애물
                {
                    float obstacleDepth = _player.CalculateObstacleDepth(hitInfo);

                    Debug.Log($"Calculated Obstacle Depth: {obstacleDepth}, Max Vaultable Depth: {_player.maxVaultableDepth}");

                    if (obstacleDepth < _player.maxVaultableDepth) // 일반 뛰어넘기 조건
                    {
                        // --- 일반 Vaulting 로직 ---
                        if (_player.TryGetObstacleTopSurface(hitInfo.point, out float topY))
                        {
                            _player.VaultStartPosition = _player.transform.position;
                            _player.VaultUpPosition = new Vector3(hitInfo.point.x, topY + _player.minVaultClearance, hitInfo.point.z) + (_player.transform.forward * (obstacleDepth * 0.3f)); // 장애물 위로 살짝 올라감

                            // VaultEndPosition: 장애물 너머로 안전하게 착지할 위치
                            // float vaultForwardClearance = obstacleDepth + _player.CharacterControllerComponent.radius + 0.2f; // 장애물 깊이 + 플레이어 반지름 + 여유 공간
                            _player.VaultEndPosition = hitInfo.collider.ClosestPointOnBounds(_player.transform.position + _player.transform.forward * 10f); // 장애물 뒷면 근처
                            _player.VaultEndPosition = new Vector3(_player.VaultEndPosition.x, _player.VaultStartPosition.y, _player.VaultEndPosition.z) + _player.transform.forward * (_player.CharacterControllerComponent.radius + 0.2f);


                            _player.CalculateDynamicVaultParameters(obstacleDepth, topY - _player.VaultStartPosition.y);
                            _player.TransitionToState(PlayerState.Vaulting);
                            _player.JumpRequested = false;
                            return true;
                        }
                    }
                    else // 기어오르기 조건 (장애물이 너무 깊을 때)
                    {
                        // --- ClimbingUp 로직 ---
                        if (_player.TryGetObstacleTopSurface(hitInfo.point, out float topY))
                        {
                            _player.VaultStartPosition = _player.transform.position; // 현재 위치에서 시작
                            // VaultUpPosition을 장애물 바로 위 가장자리로 설정
                            _player.VaultUpPosition = new Vector3(hitInfo.point.x, topY, hitInfo.point.z) + (_player.transform.forward * (_player.CharacterControllerComponent.radius * 0.5f)); // 장애물 표면에 가깝게

                            // VaultEndPosition을 장애물 위 안전한 착지 지점으로 설정
                            _player.VaultEndPosition = _player.VaultUpPosition + _player.transform.forward * ClimbUpClearance; // 장애물 위에서 앞으로 약간 이동

                            _player.TransitionToState(PlayerState.ClimbingUp);
                            _player.JumpRequested = false;
                            return true;
                        }
                    }
                }
                else Debug.Log($"Obstacle too high or too low for vaulting: {obstacleHeight} (height: {_player.CharacterControllerComponent.height * _player.canVaultHeightRatio})");
            }
            return false;
        }


        public override void Exit()
        {
            // 이동 애니메이션 비활성화
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Moving, false);
        }
    }
}