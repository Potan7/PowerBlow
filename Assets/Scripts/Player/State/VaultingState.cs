using UnityEngine;

namespace Player.State
{
    public class VaultingState : PlayerStateEntity
    {
        public VaultingState(PlayerController player) : base(player)
        {
        }

        public override void Enter()
        {
            _player.IsVaultingInternal = true; // PlayerController의 플래그와 동기화
            _player.gameObject.layer = _player.vaultLayerMask;
            _player.VaultStartTime = Time.time;
            _player.VerticalVelocity = 0; // 뛰어넘기 중에는 중력 및 기존 수직 속도 무시

            _player.InputManagerComponent.DisablePlayerActions();
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Vaulting, true); // 뛰어넘기 애니메이션 트리거
        }

        public override void Execute()
        {
            float elapsed = Time.time - _player.VaultStartTime;
            float progress = Mathf.Clamp01(elapsed / _player.CurrentVaultDuration);

            Vector3 targetPosition;
            if (progress < 0.5f)
            {
                targetPosition = Vector3.Lerp(_player.VaultStartPosition, _player.VaultUpPosition, progress * 2f);
            }
            else
            {
                targetPosition = Vector3.Lerp(_player.VaultUpPosition, _player.VaultEndPosition, (progress - 0.5f) * 2f);
            }

            Vector3 movement = targetPosition - _player.transform.position;
            _player.CharacterControllerComponent.Move(movement);

            if (progress >= 1.0f)
            {
                // 뛰어넘기 완료 후 상태 전환
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
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Vaulting, false);
            _player.InputManagerComponent.EnablePlayerActions();
            // 필요한 경우 VerticalVelocity 재설정
        }
    }
}