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
            // 슬라이딩 시작 시 초기 속도 설정 (oldController의 StartSliding)
            Vector3 worldMoveDir = _player.transform.TransformDirection(new Vector3(_player.MoveInput.x, 0, _player.MoveInput.y)).normalized;
            // 만약 진입 시 MoveInput이 없다면 (예: 착지 슬라이드), 이전 속도나 기본 전방 속도를 사용할 수 있음
            // 현재는 MovingState에서 MoveInput이 있을 때만 진입하므로 worldMoveDir이 유효함.
            if (worldMoveDir == Vector3.zero && _player.CharacterControllerComponent.velocity.magnitude > 0.1f) // 제자리 슬라이드 방지, 이전 이동 방향 유지 시도
            {
                worldMoveDir = _player.CharacterControllerComponent.velocity.normalized;
                worldMoveDir.y = 0; // 수평 방향만 사용
            }
            else if (worldMoveDir == Vector3.zero) // 그래도 방향이 없으면 플레이어의 정면 사용
            {
                worldMoveDir = _player.transform.forward;
            }

            _player.CurrentSlidingVelocity = worldMoveDir * _player.moveSpeed * _player.slideInitialSpeedMultiplier;

            // 슬라이딩 애니메이션 활성화
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Sliding, true);
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Moving, false); // 다른 상태 애니메이션 비활성화

            _player.ChangeViewAndCollider(true); // 슬라이딩 뷰로 변경
        }

        public override void Execute()
        {
            // 1. 지상 상태 확인: 공중에 뜨면 FallingState로 전환
            if (!_player.CharacterControllerComponent.isGrounded)
            {
                _player.VerticalVelocity += Physics.gravity.y * Time.deltaTime; // 중력은 계속 누적
                _player.TransitionToState(PlayerState.Falling);
                return;
            }
            else if (_player.VerticalVelocity < 0) // 땅에 있고, 이전 프레임에서 하강 중이었다면 속도 안정화
            {
                _player.VerticalVelocity = -2f;
            }

            // 2. 슬라이딩 종료 조건 확인:
            //    - 웅크리기 버튼 해제
            //    - 슬라이딩 속도가 거의 0이 된 경우
            //    - 점프키가 입력된 경우
            //    - 플레이어 위에 장애물이 없어야함

            // 만약 플레이어 위에 장애물이 있다면 슬라이딩을 계속 진행
            // bool isObstacleAbove = _player.CheckObstacleTopSurface();
            bool isObstacleAbove = CheckObstacleOnTop();
            if (_player.JumpRequested && !isObstacleAbove)
            {
                _player.TransitionToState(PlayerState.Jumping);
                return;
            }

            if ((!_player.CrouchActive || _player.CurrentSlidingVelocity.magnitude <= 0.1f) && !isObstacleAbove)
            {
                // 슬라이딩 종료: 웅크리기 버튼 해제 또는 슬라이딩 속도가 거의 0인 경우
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

            // 3. 슬라이딩 속도 감속
            if (_player.CurrentSlidingVelocity.magnitude > 0.1f)
            {
                _player.CurrentSlidingVelocity -= _player.CurrentSlidingVelocity.normalized * _player.slideDeceleration * Time.deltaTime;
                if (_player.CurrentSlidingVelocity.magnitude < 0.1f)
                {
                    _player.CurrentSlidingVelocity = Vector3.zero;
                }
            }

            // 4. 이동 적용: 슬라이딩 속도와 수직 안정화 속도를 합쳐 적용
            Vector3 horizontalMovement = _player.CurrentSlidingVelocity * Time.deltaTime;
            Vector3 verticalMovement = _player.VerticalVelocity * Time.deltaTime * Vector3.up;
            _player.CharacterControllerComponent.Move(horizontalMovement + verticalMovement);
        }

        public override void Exit()
        {
            // Debug.Log("Exiting Sliding State");
            // 슬라이딩 애니메이션 비활성화
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Sliding, false);

            // 슬라이딩 속도 초기화
            _player.CurrentSlidingVelocity = Vector3.zero;

            _player.CrouchActive = false;

            _player.ChangeViewAndCollider(false);
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