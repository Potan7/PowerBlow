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
                // _player.VerticalVelocity += Physics.gravity.y * Time.deltaTime;
                if (_player.VerticalVelocity < -0.1f) // 약간의 하강 속도가 생기면 확실히 낙하로 간주
                {
                    _player.TransitionToState(PlayerState.Falling);
                    return;
                }
            }

            // 2. 이동 입력 감지: 이동 입력이 있으면 MovingState로 전환
            if (_player.MoveInput != Vector2.zero)
            {
                _player.TransitionToState(PlayerState.Moving);
                return;
            }

            if (_player.CrouchActive)
            {
                _player.CrouchActive = false;
            }
        }

        public override void Exit()
        {
            // Debug.Log("Exiting Idle State");
        }
    }
}