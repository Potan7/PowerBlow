using UnityEngine;

namespace Player.State
{
    public class JumpingState : PlayerStateEntity
    {
        public JumpingState(PlayerController player) : base(player)
        {
        }

        public override void Enter()
        {
            // 점프 시작 시 수직 속도 설정 (oldController의 ProcessJump)
            _player.VerticalVelocity = _player.jumpPower;
            _player.JumpRequested = false; // 점프 요청 처리 완료

            // 점프 시에는 웅크리기(슬라이드) 상태 해제 및 관련 속도 초기화
            _player.CrouchActive = false;
            _player.CurrentSlidingVelocity = Vector3.zero;

            // 점프 애니메이션 실행
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Jumping, true); // Trigger 방식이므로 value는 무관할 수 있음
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Moving, false); // 다른 상태 애니메이션 비활성화
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Sliding, false);

            _player.SetCameraFOV(_player.movingFOV); // Jumping 상태의 카메라 FOV 설정
        }

        public override void Execute()
        {
            // 1. 중력 적용: 점프 후에는 계속 중력의 영향을 받음
            _player.VerticalVelocity += Physics.gravity.y * Time.deltaTime;

            // 2. 수평 이동 (공중 제어)
            Vector3 horizontalMovement = Vector3.zero;
            if (_player.MoveInput != Vector2.zero)
            {
                Vector3 worldMoveDirection = _player.transform.TransformDirection(new Vector3(_player.MoveInput.x, 0, _player.MoveInput.y)).normalized;
                horizontalMovement = worldMoveDirection * _player.moveSpeed * Time.deltaTime; // 공중 제어 속도 조절 가능

                // 애니메이터 방향 업데이트
                _player.PlayerAnimatorComponent.SetDirection(_player.MoveInput);
            }
            else
            {
                _player.PlayerAnimatorComponent.SetDirection(Vector2.zero);
            }

            // 3. 최종 이동 적용
            Vector3 verticalMovement = Vector3.up * _player.VerticalVelocity * Time.deltaTime;
            _player.CharacterControllerComponent.Move(horizontalMovement + verticalMovement);

            // 4. 하강 시작 감지: 수직 속도가 0 이하가 되면 FallingState로 전환
            if (_player.VerticalVelocity <= 0.0f)
            {
                _player.TransitionToState(PlayerState.Falling);
                return;
            }

            // 5. (선택적) 점프 중 착지 감지: 매우 낮은 점프의 경우 여기서 바로 착지할 수도 있음
            // 하지만 보통은 FallingState에서 착지를 처리하는 것이 더 일반적임.
            // if (_player.CharacterControllerComponent.isGrounded && _player.VerticalVelocity < 0)
            // {
            //     // ProcessLanding 로직 호출 또는 Idle/Moving으로 전환
            // }
        }

        public override void Exit()
        {
            // Debug.Log("Exiting Jumping State");
            // JumpingState를 나갈 때 점프 애니메이션을 명시적으로 false로 할 필요는 없음 (Trigger 방식이므로)
            // 만약 Bool 방식이라면 여기서 false 처리
            // _player.PlayerAnimatorComponent.SetAnim(PlayerState.Jumping, false);
        }
    }
}