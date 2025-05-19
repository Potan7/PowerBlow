using UnityEngine;

namespace Player.State
{
    public class SlidingState : IPlayerState
    {
        private PlayerController _player;

        public SlidingState(PlayerController player)
        {
            _player = player;
        }

        public void Enter()
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

        public void Execute()
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
            if (_player.JumpRequested)
            {
                _player.TransitionToState(PlayerState.Jumping);
                return;
            }
            if (!_player.CrouchActive || _player.CurrentSlidingVelocity.magnitude <= 0.1f)
            {
                if (_player.MoveInput != Vector2.zero)
                {
                    _player.TransitionToState(PlayerState.Moving);
                }
                else
                {
                    _player.TransitionToState(PlayerState.Idle);
                }
                // CrouchActive는 PlayerController의 입력 콜백에서 토글되므로,
                // 여기서 false로 설정할 필요는 없음. 상태 전환 시 자연스럽게 슬라이딩이 종료됨.
                return;
            }

            // 3. 슬라이딩 속도 감속 (oldController의 UpdateSlidingMovement)
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
            Vector3 verticalMovement = Vector3.up * _player.VerticalVelocity * Time.deltaTime;
            _player.CharacterControllerComponent.Move(horizontalMovement + verticalMovement);
        }

        public void Exit()
        {
            // Debug.Log("Exiting Sliding State");
            // 슬라이딩 애니메이션 비활성화
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Sliding, false);

            // 슬라이딩 속도 초기화
            _player.CurrentSlidingVelocity = Vector3.zero;

            _player.ChangeViewAndCollider(false); // 여기서 직접 호출해도 무방
        }
    }
}