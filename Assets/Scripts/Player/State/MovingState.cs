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

        public void Enter(PlayerController playerController)
        {
            _player = playerController;
            // Debug.Log("Entering Moving State");

            // 이동 애니메이션 활성화
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Moving, true);
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Sliding, false); // 혹시 모를 슬라이딩 애니메이션 해제
        }

        public void Execute()
        {
            // 1. 지상 상태 유지 및 중력 처리
            if (!_player.CharacterControllerComponent.isGrounded)
            {
                _player.VerticalVelocity += Physics.gravity.y * Time.deltaTime;
                // 공중에 뜨면 즉시 Falling 상태로 전환 (점프가 아닌 경우)
                if (_player.VerticalVelocity < -0.1f) // 점프 직후가 아닌, 일반적인 낙하 시작 감지
                {
                     _player.TransitionToState(PlayerState.Falling);
                     return;
                }
            }
            else if (_player.VerticalVelocity < 0)
            {
                _player.VerticalVelocity = -2f;
            }

            // 2. 이동 입력 중단 감지: 이동 입력이 없고 땅에 있다면 IdleState로 전환
            if (_player.MoveInput == Vector2.zero && _player.CharacterControllerComponent.isGrounded)
            {
                _player.TransitionToState(PlayerState.Idle);
                return;
            }

            // 3. 점프 요청 감지: 점프 버튼이 눌렸고 땅에 있다면 JumpingState로 전환
            if (_player.JumpRequested && _player.CharacterControllerComponent.isGrounded)
            {
                _player.TransitionToState(PlayerState.Jumping);
                // JumpRequested는 JumpingState의 Enter에서 false로 설정
                return;
            }

            // 4. 슬라이드 요청 감지: 웅크리기 버튼이 활성화되어 있고, 땅에 있으며, 이동 입력이 있다면 SlidingState로 전환
            // (oldController의 ProcessSlide 참조 - 이동 중 슬라이드 시작)
            if (_player.CrouchActive && _player.CharacterControllerComponent.isGrounded && _player.MoveInput != Vector2.zero)
            {
                _player.TransitionToState(PlayerState.Sliding);
                return;
            }

            // 5. 뛰어넘기 시도: 조건이 맞으면 VaultingState로 전환 (기존 TryVault 로직 사용)
            TryVault();
            if (_player.CurrentStateType == PlayerState.Vaulting) return; // 뛰어넘기가 시작되었으면 나머지 로직 중단


            // 6. 수평 이동 처리
            Vector3 move = new Vector3(_player.MoveInput.x, 0, _player.MoveInput.y);
            move = _player.transform.TransformDirection(move).normalized; // 정규화 추가
            Vector3 horizontalMovement = move * _player.moveSpeed * Time.deltaTime;

            // 7. 최종 이동 적용: 수평 이동과 수직 이동(중력에 의한 안정화)을 합쳐 적용
            Vector3 verticalMovement = Vector3.up * _player.VerticalVelocity * Time.deltaTime;
            _player.CharacterControllerComponent.Move(horizontalMovement + verticalMovement);

            // 8. 애니메이터 방향 업데이트
            _player.PlayerAnimatorComponent.SetDirection(_player.MoveInput);
        }

        public void Exit()
        {
            // Debug.Log("Exiting Moving State");
            // Moving 상태를 나갈 때 이동 애니메이션을 false로 할 수 있지만,
            // 보통 다음 상태의 Enter에서 필요한 애니메이션을 설정하므로 필수는 아님.
            // _player.PlayerAnimatorComponent.SetAnim(PlayerState.Moving, false);
        }

        // 뛰어넘기 시도 로직 (oldController의 TryInitiateVaultInternal 일부)
        private void TryVault()
        {
            // 뛰어넘기는 땅에 있고, 이동 입력이 있을 때만 시도
            if (!_player.CharacterControllerComponent.isGrounded || _player.MoveInput == Vector2.zero) return;

            // 캐릭터 발 근처에서 약간 앞에서 레이캐스트 시작
            Vector3 rayOriginFeet = _player.transform.position + _player.CharacterControllerComponent.center + Vector3.up * (-_player.StandingColliderHeight / 2f + 0.05f);

            if (Physics.Raycast(rayOriginFeet, _player.transform.forward, out RaycastHit hitInfo, _player.vaultCheckDistance, _player.vaultableLayers))
            {
                if (!hitInfo.collider.CompareTag("Wall")) return; // "Wall" 태그가 있는 장애물만

                // 장애물의 실제 상단 표면 찾기
                if (!_player.TryGetObstacleTopSurface(hitInfo, out float obstacleActualTopY)) return;

                // 장애물 높이 계산 및 조건 확인
                float obstacleHeightFromPlayerFeet = obstacleActualTopY - rayOriginFeet.y;
                float maxVaultableHeight = _player.StandingColliderHeight * _player.canVaultHeightRatio;

                if (obstacleHeightFromPlayerFeet < 0.1f || obstacleHeightFromPlayerFeet >= maxVaultableHeight)
                {
                    // Debug.LogWarning($"Vault: Obstacle height {obstacleHeightFromPlayerFeet} is not vaultable. Vault aborted.");
                    return;
                }

                // 장애물 깊이 및 동적 파라미터 계산
                float obstacleDepth = _player.CalculateObstacleDepth(hitInfo);
                _player.CalculateDynamicVaultParameters(obstacleDepth, obstacleHeightFromPlayerFeet);

                // 뛰어넘기 시작 위치, 정점, 끝점 설정 (PlayerController의 프로퍼티 사용)
                _player.VaultStartPosition = _player.transform.position;

                Vector3 peakHorizontalBase = hitInfo.point + _player.transform.forward * (_player.CharacterControllerComponent.radius + 0.1f);
                _player.VaultUpPosition = new Vector3(peakHorizontalBase.x, obstacleActualTopY + _player.CurrentVaultJumpHeight, peakHorizontalBase.z);

                float vaultForwardClearance = obstacleDepth + _player.CharacterControllerComponent.radius + 0.2f;
                _player.VaultEndPosition = hitInfo.point - _player.transform.forward * hitInfo.distance +
                                           _player.transform.forward * (hitInfo.distance + vaultForwardClearance);
                _player.VaultEndPosition = new Vector3(_player.VaultEndPosition.x, _player.VaultStartPosition.y, _player.VaultEndPosition.z); // 착지 높이는 시작 높이와 동일하게

                // VaultingState로 전환
                _player.TransitionToState(PlayerState.Vaulting);
            }
        }
    }
}