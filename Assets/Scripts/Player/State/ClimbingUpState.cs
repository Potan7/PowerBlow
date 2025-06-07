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
            _player.IsVaultingInternal = true;
            _player.gameObject.layer = _player.vaultLayerMask; // 충돌 방지 레이어
            _startTime = Time.time;
            _player.VerticalVelocity = 0;
            _player.InputManagerComponent.DisablePlayerActions();
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.ClimbingUp, true); // ClimbingUp 애니메이션

            _climbStartPosition = _player.transform.position;
            _climbOverPosition = _player.VaultUpPosition;
            _climbEndPosition = _player.VaultEndPosition;

            _player.PlayerAudioComponent.PlaySound(PlayerAudioManager.PlayerAudioType.Vaulting); // 뛰어넘기 사운드 재생
        }

        public override void Execute()
        {
            float elapsed = Time.time - _startTime;
            float progress = Mathf.Clamp01(elapsed / _climbDuration);

            Vector3 targetPosition;
            if (progress < 0.6f)
            {
                targetPosition = Vector3.Lerp(_climbStartPosition, _climbOverPosition, progress / 0.6f);
            }
            else
            {
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
                    _player.TransitionToState(PlayerState.Falling);
                }
            }
        }

        public override void Exit()
        {
            _player.IsVaultingInternal = false;
            _player.gameObject.layer = _player.OriginalPlayerLayer;
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.ClimbingUp, false);
            _player.InputManagerComponent.EnablePlayerActions();

            _player.currentHorizontalSpeed = 0;
            _player.VerticalVelocity = 0;
        }
    }
}