using UnityEngine;

namespace Player.State
{
    public class IdleState : PlayerStateEntity
    {
        public IdleState(PlayerController player) : base(player)
        {
        }

        public override void Enter()
        {
            // Idle 상태에 맞는 애니메이션 설정
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Moving, false);
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Sliding, false);
            _player.PlayerAnimatorComponent.SetDirection(Vector2.zero); // 방향 애니메이션 초기화

            _player.SetCameraFOV(_player.idleFOV); // Idle 상태의 카메라 FOV 설정
        }

        public override void Execute()
        {
            // 1. 지상 상태 유지 및 중력 처리: 땅에 붙어있도록 하고, 혹시 공중에 뜨면 중력 적용
            if (!_player.CharacterControllerComponent.isGrounded)
            {
                // 예상치 못하게 공중에 뜬 경우 (예: 경사면에서 미끄러짐) Falling 상태로 전환
                _player.VerticalVelocity += Physics.gravity.y * Time.deltaTime; // 즉시 Falling으로 보내기 전에 중력 누적
                if (_player.VerticalVelocity < -0.1f) // 약간의 하강 속도가 생기면 확실히 낙하로 간주
                {
                    _player.TransitionToState(PlayerState.Falling);
                    return;
                }
            }
            else if (_player.VerticalVelocity < 0) // 땅에 있고, 이전 프레임에서 하강 중이었다면 속도 안정화
            {
                _player.VerticalVelocity = -2f; // 땅에 안정적으로 붙도록 함
            }

            // 2. 이동 입력 감지: 이동 입력이 있으면 MovingState로 전환
            if (_player.MoveInput != Vector2.zero)
            {
                _player.TransitionToState(PlayerState.Moving);
                return;
            }

            // 3. 점프 요청 감지: 점프 버튼이 눌렸고 땅에 있다면 JumpingState로 전환
            if (_player.JumpRequested && _player.CharacterControllerComponent.isGrounded)
            {
                _player.TransitionToState(PlayerState.Jumping);
                // JumpRequested는 JumpingState의 Enter에서 false로 설정
                return;
            }

            if (_player.CrouchActive)
            {
                _player.CrouchActive = false;
            }

            // 4. 웅크리기(슬라이드) 버튼 활성화 시 처리:
                // Idle 상태에서 웅크리기 버튼이 활성화되어도 바로 슬라이딩으로 가진 않음 (이동 입력이 없으므로).
                // 만약 "CrouchIdle" 상태가 있다면 그쪽으로 전환할 수 있음.
                // 현재 구조에서는 MovingState에서 CrouchActive와 MoveInput을 함께 확인하여 SlidingState로 전환.
                // 여기서 CrouchActive가 true이고, 이후 MoveInput이 들어오면 MovingState를 거쳐 SlidingState로 감.

                // 5. 최종 이동 적용: Idle 상태에서는 주로 수직 이동(중력에 의한 안정화)만 적용됨
                Vector3 verticalMovement = Vector3.up * _player.VerticalVelocity * Time.deltaTime;
            _player.CharacterControllerComponent.Move(verticalMovement);
        }

        public override void Exit()
        {
            // Debug.Log("Exiting Idle State");
        }
    }
}