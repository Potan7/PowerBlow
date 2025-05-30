using UnityEngine;

namespace Player.State
{
    public class ClimbingUpState : PlayerStateEntity
    {
        private Vector3 _climbStartPosition;
        private Vector3 _climbOverPosition; // 장애물 바로 위 가장자리
        private Vector3 _climbEndPosition;   // 장애물 위 안전한 착지 지점
        private float _climbDuration = 0.7f; // 기어오르기 지속 시간 (조정 가능)
        private float _startTime;

        public ClimbingUpState(PlayerController player) : base(player)
        {
        }

        public override void Enter()
        {
            _player.IsVaultingInternal = true; // 유사한 플래그 사용 또는 별도 플래그 추가 가능
            _player.gameObject.layer = _player.vaultLayerMask; // 충돌 방지 레이어
            _startTime = Time.time;
            _player.VerticalVelocity = 0;
            _player.PlayerInput.Disable();
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.ClimbingUp, true); // ClimbingUp 애니메이션 (새로 만들어야 함)

            // 시작 위치는 현재 플레이어 위치
            _climbStartPosition = _player.transform.position;

            // 장애물 정보는 MovingState에서 TryVault/TryClimb 시점에 계산되어 PlayerController에 저장되어 있어야 함
            // 여기서는 VaultUpPosition을 장애물 위 가장자리로, VaultEndPosition을 장애물 위 최종 착지 지점으로 사용한다고 가정
            // MovingState에서 이 값들을 올바르게 설정해야 함
            _climbOverPosition = _player.VaultUpPosition; // 장애물 위 가장자리 (MovingState에서 계산된 값)
            _climbEndPosition = _player.VaultEndPosition;   // 장애물 위 최종 착지 지점 (MovingState에서 계산된 값)
        }

        public override void Execute()
        {
            float elapsed = Time.time - _startTime;
            float progress = Mathf.Clamp01(elapsed / _climbDuration);

            Vector3 targetPosition;
            // 3단계 이동: 1. 장애물 위 가장자리로, 2. 장애물 위로 수평 이동
            if (progress < 0.6f) // 60%는 위로 올라가는 데 사용
            {
                // 시작점에서 장애물 위 가장자리(_climbOverPosition)로 이동
                targetPosition = Vector3.Lerp(_climbStartPosition, _climbOverPosition, progress / 0.6f);
            }
            else // 나머지 40%는 장애물 위에서 앞으로 이동
            {
                // 장애물 위 가장자리에서 최종 착지 지점(_climbEndPosition)으로 이동
                targetPosition = Vector3.Lerp(_climbOverPosition, _climbEndPosition, (progress - 0.6f) / 0.4f);
            }

            Vector3 movement = targetPosition - _player.transform.position;
            _player.CharacterControllerComponent.Move(movement);

            if (progress >= 1.0f)
            {
                if (_player.CharacterControllerComponent.isGrounded)
                {
                    _player.TransitionToState(PlayerState.Idle);
                }
                else
                {
                    // 만약을 위해 Falling 상태로 전환 (원래는 땅에 있는게 맞음)
                    _player.TransitionToState(PlayerState.Falling);
                }
            }
        }

        public override void Exit()
        {
            _player.IsVaultingInternal = false;
            _player.gameObject.layer = _player.OriginalPlayerLayer;
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.ClimbingUp, false);
            _player.PlayerInput.Enable();
        }
    }
}