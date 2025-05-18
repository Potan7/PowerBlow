using UnityEngine;

namespace Player.State
{
    public class MovingState : IPlayerState
    {
        private PlayerController _player;

        public MovingState(PlayerController player)
        {
            _player = player;
        }

        public void Enter(PlayerController player)
        {
            // Debug.Log("Entering Moving State");
            _player = player;
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Moving, true);
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Sliding, false);
        }

        public void Execute()
        {
            // 중력 적용
            if (!_player.CharacterControllerComponent.isGrounded)
            {
                _player.VerticalVelocity += Physics.gravity.y * Time.deltaTime;
            }
            else if (_player.VerticalVelocity < 0)
            {
                _player.VerticalVelocity = -2f;
            }

            // 이동 입력이 없으면 Idle 상태로 전환
            if (_player.MoveInput == Vector2.zero && _player.CharacterControllerComponent.isGrounded)
            {
                _player.TransitionToState(_player.IdleState, PlayerState.Idle);
                return;
            }

            // 점프 입력 확인
            if (_player.JumpRequested && _player.CharacterControllerComponent.isGrounded)
            {
                _player.TransitionToState(_player.JumpingState, PlayerState.Jumping);
                _player.JumpRequested = false;
                return;
            }

            // 슬라이드 입력 확인
            if (_player.CrouchActive && _player.CharacterControllerComponent.isGrounded)
            {
                _player.TransitionToState(_player.SlidingState, PlayerState.Sliding);
                return;
            }

            // 뛰어넘기 조건 확인
            TryVault();


            // 이동 처리
            Vector3 move = new Vector3(_player.MoveInput.x, 0, _player.MoveInput.y);
            move = _player.transform.TransformDirection(move);
            Vector3 horizontalMovement = move * _player.moveSpeed * Time.deltaTime;
            Vector3 verticalMovement = Vector3.up * _player.VerticalVelocity * Time.deltaTime;
            _player.CharacterControllerComponent.Move(horizontalMovement + verticalMovement);

            // 애니메이터 방향 업데이트
            _player.PlayerAnimatorComponent.SetDirection(_player.MoveInput);


            // 땅에 있지 않다면 Falling 상태로 전환
            if (!_player.CharacterControllerComponent.isGrounded && _player.VerticalVelocity <= 0)
            {
                _player.TransitionToState(_player.FallingState, PlayerState.Falling);
                return;
            }
        }

        public void Exit()
        {
            // Debug.Log("Exiting Moving State");
            // 필요하다면 이동 애니메이션 false 처리, 하지만 다음 상태의 Enter에서 덮어쓰는 것이 일반적
            // _player.PlayerAnimatorComponent.SetAnim(PlayerState.Moving, false);
        }

        private void TryVault()
        {
            if (!_player.CharacterControllerComponent.isGrounded || _player.MoveInput == Vector2.zero) return;

            Vector3 rayOriginFeet = _player.transform.position + _player.CharacterControllerComponent.center + Vector3.up * (-_player.StandingColliderHeight / 2f + 0.05f);
            // Debug.DrawRay(rayOriginFeet, _player.transform.forward * _player.vaultCheckDistance, Color.cyan, 0.5f);

            if (Physics.Raycast(rayOriginFeet, _player.transform.forward, out RaycastHit hitInfo, _player.vaultCheckDistance, _player.vaultableLayers))
            {
                if (!hitInfo.collider.CompareTag("Wall")) return;

                if (!_player.TryGetObstacleTopSurface(hitInfo, out float obstacleActualTopY)) return;

                float obstacleHeightFromPlayerFeet = obstacleActualTopY - rayOriginFeet.y;
                float maxVaultableHeight = _player.StandingColliderHeight * _player.canVaultHeightRatio;

                if (obstacleHeightFromPlayerFeet < 0.1f || obstacleHeightFromPlayerFeet >= maxVaultableHeight)
                {
                    // Debug.LogWarning($"Vault: Obstacle height {obstacleHeightFromPlayerFeet} is not vaultable. Vault aborted.");
                    return;
                }

                float obstacleDepth = _player.CalculateObstacleDepth(hitInfo);
                _player.CalculateDynamicVaultParameters(obstacleDepth, obstacleHeightFromPlayerFeet);

                // VaultingState로 전환하고, 필요한 정보 전달 또는 VaultingState.Enter에서 설정
                _player.VaultStartPosition = _player.transform.position;
                _player.VaultUpPosition = new Vector3(
                    hitInfo.point.x + _player.transform.forward.x * (_player.CharacterControllerComponent.radius + 0.1f),
                    obstacleActualTopY + _player.CurrentVaultJumpHeight,
                    hitInfo.point.z + _player.transform.forward.z * (_player.CharacterControllerComponent.radius + 0.1f)
                );

                float vaultForwardClearance = obstacleDepth + _player.CharacterControllerComponent.radius + 0.2f;
                _player.VaultEndPosition = hitInfo.point - _player.transform.forward * hitInfo.distance +
                                        _player.transform.forward * (hitInfo.distance + vaultForwardClearance);
                _player.VaultEndPosition = new Vector3(_player.VaultEndPosition.x, _player.VaultStartPosition.y, _player.VaultEndPosition.z);


                _player.TransitionToState(_player.VaultingState, PlayerState.Vaulting);
            }
        }
    }
}