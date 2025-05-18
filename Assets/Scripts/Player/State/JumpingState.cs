
using UnityEngine;

namespace Player.State
{
    public class JumpingState : IPlayerState
    {
        private PlayerController _player;

        public JumpingState(PlayerController player)
        {
            _player = player;
        }

        public void Enter(PlayerController player)
        {
            // Debug.Log("Entering Jumping State");
            _player = player; // PlayerController 참조 설정
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Jumping, true); // Jumping 애니메이션 (Jumping true)
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Moving, false);
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Sliding, false);
        }

        public void Execute()
        {
            // 중력 적용 (항상 필요)
            if (!_player.CharacterControllerComponent.isGrounded)
            {
                _player.VerticalVelocity += Physics.gravity.y * Time.deltaTime;
            }
            else if (_player.VerticalVelocity < 0) // 땅에 닿았고, 하강 중이었다면 속도 리셋
            {
                _player.VerticalVelocity = -2f; // 안정적인 착지를 위해
                _player.TransitionToState(_player.IdleState, PlayerState.Idle); // Idle 상태로 전환
                return;
            }

            // 이동 입력 확인
            if (_player.MoveInput != Vector2.zero)
            {
                _player.TransitionToState(_player.MovingState, PlayerState.Moving);
                return;
            }

            // 슬라이드(웅크리기) 입력 확인
            if (_player.CrouchActive && _player.CharacterControllerComponent.isGrounded) // Jumping 상태에서 웅크리기 시도
            {
                _player.TransitionToState(_player.SlidingState, PlayerState.Sliding);
                return;
            }
        }

        public void Exit()
        {
            // Debug.Log("Exiting Jumping State");
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Jumping, false); // Jumping 애니메이션 종료
        }
    }
}