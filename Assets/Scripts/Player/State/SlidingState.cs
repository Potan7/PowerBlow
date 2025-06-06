using UnityEngine;

namespace Player.State
{
    public class SlidingState : PlayerStateEntity
    {
        public SlidingState(PlayerController player) : base(player)
        {
        }

        public override void Enter()
        {
            Vector3 initialSlideDirection = _player.transform.forward; // 기본 슬라이드 방향은 플레이어 정면

            // 입력이 있다면 해당 방향으로, 없다면 현재 이동 방향이나 정면을 사용
            if (_player.MoveInput != Vector2.zero)
            {
                initialSlideDirection = _player.transform.TransformDirection(new Vector3(_player.MoveInput.x, 0, _player.MoveInput.y)).normalized;
            }
            else if (_player.CharacterControllerComponent.velocity.magnitude > 0.1f) // 이전 프레임의 관성이 있다면 해당 방향 사용
            {
                Vector3 currentVelocityDirection = _player.CharacterControllerComponent.velocity.normalized;
                currentVelocityDirection.y = 0;
                if (currentVelocityDirection.sqrMagnitude > 0.01f)
                {
                    initialSlideDirection = currentVelocityDirection;
                }
            }
            _player.lastHorizontalMoveDirection = initialSlideDirection;

            // 슬라이딩 시작 시 초기 속도 설정
            float startSpeedBase = _player.moveSpeed;
            // 스피드 블록 위에서 슬라이딩 시작 시 추가 속도
            if (_player.isOnSpeedBlock && _player.CharacterControllerComponent.isGrounded)
            {
                startSpeedBase *= 3f; // PlayerController의 스피드 블록 배율과 일치
            }
            // 현재 속도와 계산된 시작 속도 중 더 큰 값을 기준으로 슬라이드 배율 적용, 또는 최소 슬라이드 속도 보장
            _player.currentHorizontalSpeed = Mathf.Max(_player.currentHorizontalSpeed, startSpeedBase) * _player.slideInitialSpeedMultiplier;


            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Sliding, true);
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Moving, false); 

            _player.ChangeViewAndCollider(true); 
            _player.SetCameraFOV(_player.movingFOV); 
            _player.PlayerAudioComponent.PlaySound(PlayerAudioManager.PlayerAudioType.Slide);
        }

        public override void Execute()
        {
            if (!_player.CharacterControllerComponent.isGrounded)
            {
                _player.TransitionToState(PlayerState.Falling);
                return;
            }
            // VerticalVelocity는 PlayerController의 Update에서 이미 처리됨 (땅에 붙이는 로직 등)

            bool isObstacleAbove = CheckObstacleOnTop();
            if (_player.JumpRequested && !isObstacleAbove)
            {
                _player.DoJump(); // DoJump는 VerticalVelocity를 설정하고 FallingState로 전환
                return;
            }

            // 슬라이딩 종료 조건: 웅크리기 해제 또는 속도 매우 낮음 (그리고 머리 위에 장애물 없음)
            if ((!_player.CrouchActive || _player.currentHorizontalSpeed <= 0.1f) && !isObstacleAbove)
            {
                if (_player.MoveInput != Vector2.zero)
                {
                    _player.TransitionToState(PlayerState.Moving);
                }
                else
                {
                    _player.TransitionToState(PlayerState.Idle);
                }
                return;
            }

            // 슬라이딩 속도 감속은 PlayerController의 Update에서 currentHorizontalSpeed를 통해 처리됨
            // 이동 적용도 PlayerController의 Update에서 CharacterController.Move를 통해 처리됨
        }

        public override void Exit()
        {
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Sliding, false);
            // _player.CurrentSlidingVelocity = Vector3.zero; // 제거됨
            _player.CrouchActive = false;
            _player.ChangeViewAndCollider(false);
            // currentHorizontalSpeed는 PlayerController에서 계속 관리되므로 여기서 초기화하지 않음.
            // 슬라이딩 종료 후 바로 달릴 수 있도록 현재 속도 유지.
        }

        public bool CheckObstacleOnTop()
        {
            Vector3 topRayCheckOrigin = _player.transform.position;
            // Debug.DrawRay(topRayCheckOrigin, Vector3.up * (StandingColliderHeight + 0.2f), Color.magenta, 2f);

            if (Physics.Raycast(topRayCheckOrigin, Vector3.up, out RaycastHit topSurfaceHit,  _player.StandingColliderHeight + 0.2f,  _player.vaultableLayers))
            {
                // Debug.Log("Obstacle detected above player.");
                return true;
            }
            else
            {
                // Debug.Log("No obstacle detected above player.");
                return false;
            }
        }
    }
}