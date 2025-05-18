
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

        public void Enter(PlayerController player)
        {
            // Debug.Log("Entering Sliding State");
            _player = player; // PlayerController 참조 설정
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Sliding, true); // 슬라이딩 애니메이션 (Sliding true)
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Moving, false);
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
            }

            // 슬라이드 입력 확인
            if (!_player.CrouchActive || !_player.CharacterControllerComponent.isGrounded) // 웅크리기 해제 또는 공중에 있을 때
            {
                _player.TransitionToState(_player.IdleState, PlayerState.Idle);
                return;
            }

            // 이동 입력 확인
            if (_player.MoveInput != Vector2.zero)
            {
                _player.TransitionToState(_player.MovingState, PlayerState.Moving);
                return;
            }
        }

        public void Exit()
        {
            // Debug.Log("Exiting Sliding State");
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Sliding, false); // 슬라이딩 애니메이션 종료
        }
    }
}