using UnityEngine;

namespace Player.State
{
    public class FallingState : PlayerStateEntity
    {
        private bool _wasMovingLastFrameInAir; // 공중에서 이전 프레임 이동 입력 상태

        public FallingState(PlayerController player) : base(player)
        {
        }

        // 상태 진입 시 호출되는 메서드
        public override void Enter()
        {
            // Debug.Log("Entering Falling State");
            // 낙하 애니메이션 활성화
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Falling, true);

            // 진입 시 이동 입력에 따라 초기 FOV 설정
            _wasMovingLastFrameInAir = _player.MoveInput != Vector2.zero;
            _player.SetCameraFOV(_wasMovingLastFrameInAir ? _player.movingFOV : _player.idleFOV);
        }

        // 매 프레임 호출되는 메서드 (상태의 주요 로직)
        public override void Execute()
        {
            // 1. 중력 적용: 제거
            // _player.VerticalVelocity += Physics.gravity.y * Time.deltaTime; 

            // 2. 수평 이동 (공중 제어): 플레이어 입력에 따라 공중에서도 약간의 수평 이동을 허용합니다.
            Vector3 horizontalMovement = Vector3.zero;
            Vector3 worldMoveDirection = Vector3.zero;
            bool isCurrentlyMovingInAir = _player.MoveInput != Vector2.zero;

            // if (isCurrentlyMovingInAir)
            // {
            //     // 입력 방향을 월드 좌표 기준으로 변환
            //     worldMoveDirection = _player.transform.TransformDirection(new Vector3(_player.MoveInput.x, 0, _player.MoveInput.y)).normalized;
            //     horizontalMovement = _player.moveSpeed * Time.deltaTime * worldMoveDirection;
            // }

            // 플레이어의 카메라가 바라보는 방향을 구함
            Vector3 cameraForward = _player.head.transform.forward;

            // 공중에서 이동 입력 상태가 변경되면 FOV 업데이트
            if (isCurrentlyMovingInAir != _wasMovingLastFrameInAir)
            {
                _player.SetCameraFOV(isCurrentlyMovingInAir ? _player.movingFOV : _player.idleFOV);
                _wasMovingLastFrameInAir = isCurrentlyMovingInAir;
            }
            
            // 3. 최종 이동 적용: 제거
            // Vector3 verticalMovement = Vector3.up * _player.VerticalVelocity * Time.deltaTime;
            // _player.CharacterControllerComponent.Move(horizontalMovement + verticalMovement);

            // 4. 착지 감지
            if (_player.CharacterControllerComponent.isGrounded && _player.VerticalVelocity < 0) // VerticalVelocity 조건 추가 (하강 중 착지)
            {
                ProcessLanding();
                return;
            }

            // 벽 타기 시도 (플레이어가 벽 쪽으로 이동 입력을 할 때)
            if (TryWallClimb(cameraForward))
            {
                return; // 벽 타기 성공 시 ClimbingUpState로 전환됨
            }
        }

        // 벽 타기 시도 로직
        private bool TryWallClimb(Vector3 moveDirection)
        {
            // 1. 전방 벽 감지 (캐릭터 컨트롤러의 중심에서)
            Vector3 rayOrigin = _player.transform.position + _player.CharacterControllerComponent.center;
            Debug.DrawRay(rayOrigin, moveDirection * _player.wallClimbCheckDistance, Color.cyan, 0.1f);

            if (Physics.Raycast(rayOrigin, moveDirection, out RaycastHit wallHit, _player.wallClimbCheckDistance, _player.vaultableLayers))
            {

                // 2. 벽의 법선 확인 (너무 바닥이나 천장이 아니어야 함)
                if (Mathf.Abs(wallHit.normal.y) > 0.3f) return false;

                // 3. 플레이어가 벽을 향해 이동 중인지 확인 (벽의 법선과 이동 방향 내적)
                if (Vector3.Dot(moveDirection, -wallHit.normal) < 0.5f) return false;

                // 4. 벽 상단 표면(턱) 찾기 시도
                //    벽 충돌 지점 약간 위에서 아래로 레이캐스트하여 턱을 찾음

                // ledgeCheckOrigin 계산 방식 수정
                float forwardOffset = _player.CharacterControllerComponent.radius + 0.1f; // 벽을 통과해서 쏠 수 있도록 플레이어 반지름 + 약간의 여유
                Vector3 horizontalCheckStart = wallHit.point + moveDirection * forwardOffset; // 벽면보다 살짝 앞에서 시작

                // Y 시작 위치 수정: 첫 번째 벽 충돌 지점(wallHit.point)의 Y를 기준으로 최대 올라갈 높이만큼 위에서 시작
                // 이렇게 하면 플레이어의 현재 Y 위치와 관계없이 벽의 실제 충돌 지점을 기준으로 탐색
                float startY = wallHit.point.y + _player.maxWallClimbHeight + 0.1f; // wallHit.point.y 기준 + 최대 높이 + 약간의 여유
                
                Vector3 ledgeCheckOrigin = new Vector3(horizontalCheckStart.x, startY, horizontalCheckStart.z);

                // 레이캐스트 길이: 최대 높이에서 최소 높이까지의 범위 + 여유
                // startY가 wallHit.point.y + maxWallClimbHeight 이므로, 여기서 minWallClimbHeight까지 내려오려면
                // (maxWallClimbHeight - minWallClimbHeight) + (턱의 두께나 감지 여유) 만큼의 길이가 필요
                float ledgeRayLength = _player.maxWallClimbHeight - _player.minWallClimbHeight + 0.3f;
                if (ledgeRayLength <= 0.1f) ledgeRayLength = _player.maxWallClimbHeight + 0.1f; 

                Debug.DrawRay(ledgeCheckOrigin, Vector3.down * ledgeRayLength, Color.yellow, 0.2f); 

                if (Physics.Raycast(ledgeCheckOrigin, Vector3.down, out RaycastHit ledgeHit, ledgeRayLength, _player.vaultableLayers))
                {
                    // 추가: 감지된 턱(ledgeHit)이 처음 충돌한 벽면(wallHit)보다 너무 높지 않은지 확인
                    // 예를 들어, maxWallClimbHeight 이상으로 차이나면 잘못된 감지일 수 있음
                    if (ledgeHit.point.y > wallHit.point.y + _player.maxWallClimbHeight + 0.2f) // 0.2f는 약간의 오차 허용
                    {
                        // Debug.Log("WallClimb Fail: Detected ledge is too high compared to initial wall hit point.");
                        return false;
                    }

                    // 발밑에서부터 턱까지의 실제 높이 계산
                    float playerFeetY = _player.transform.position.y - _player.CharacterControllerComponent.height / 2f + _player.CharacterControllerComponent.skinWidth;
                    float wallActualHeight = ledgeHit.point.y - playerFeetY;

                    // 5. 벽 높이가 적절한지 확인
                    if (wallActualHeight >= _player.minWallClimbHeight && wallActualHeight <= _player.maxWallClimbHeight)
                    {
                        // 6. 벽 위 공간 확인 (플레이어가 일어설 수 있는지)
                        Vector3 clearanceCheckOrigin = ledgeHit.point + Vector3.up * 0.1f; // 턱 바로 위
                        Debug.DrawRay(clearanceCheckOrigin, Vector3.up * _player.StandingColliderHeight, Color.magenta, 0.1f);
                        if (!Physics.CapsuleCast(clearanceCheckOrigin + Vector3.up * (_player.StandingColliderHeight - _player.CharacterControllerComponent.radius),
                                                clearanceCheckOrigin + Vector3.up * _player.CharacterControllerComponent.radius,
                                                _player.CharacterControllerComponent.radius - 0.05f, // 약간 작은 반지름
                                                Vector3.up, 0.01f, _player.vaultableLayers))
                        {
                            // 모든 조건 만족: 벽 타기 파라미터 설정 및 ClimbingUpState로 전환
                            _player.VaultStartPosition = _player.transform.position;
                            // VaultUpPosition: 벽의 턱 부분, 플레이어가 손을 짚을 위치
                            _player.VaultUpPosition = new Vector3(ledgeHit.point.x, ledgeHit.point.y, ledgeHit.point.z) - (moveDirection * (_player.CharacterControllerComponent.radius * 0.1f)); // 턱에 살짝 걸치도록
                            
                            // 플레이어가 턱 바로 위에 안정적으로 서는 데 필요한 오프셋 사용
                            float climbUpSpecificForwardOffset = _player.CharacterControllerComponent.radius + 0.1f; // 플레이어 반지름 + 약간의 여유로 턱 바로 위에 위치하도록 설정
                            _player.VaultEndPosition = new Vector3(ledgeHit.point.x, ledgeHit.point.y, ledgeHit.point.z) + (moveDirection * climbUpSpecificForwardOffset);

                            _player.TransitionToState(PlayerState.ClimbingUp);
                            return true;
                        }
                        // else Debug.Log("WallClimb Fail: No clearance above ledge.");
                    }
                    // else Debug.Log($"WallClimb Fail: Height not suitable. Actual: {wallActualHeight}, Min: {_player.minWallClimbHeight}, Max: {_player.maxWallClimbHeight}");
                }
                // else Debug.Log("WallClimb Fail: Could not find ledge top.");
            }
            return false;
        }

        // 상태 종료 시 호출되는 메서드
        public override void Exit()
        {
            // Debug.Log("Exiting Falling State");
            // 낙하 애니메이션 비활성화
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Falling, false);
        }

        // 착지 처리 로직 (oldController의 ProcessLanding 참조)
        private void ProcessLanding()
        {
            // 착지 시 수직 속도를 약간 아래로 설정하여 안정적으로 땅에 붙도록
            _player.VerticalVelocity = -2f;

            // 착지 시 웅크리기(슬라이드) 버튼이 눌려있고 이동 입력이 있다면 슬라이딩 상태로 전환
            if (_player.CrouchActive && _player.MoveInput != Vector2.zero)
            {
                _player.TransitionToState(PlayerState.Sliding);
            }
            // 그 외의 경우, 이동 입력 유무에 따라 Moving 또는 Idle 상태로 전환
            else
            {
                if (_player.MoveInput != Vector2.zero)
                {
                    _player.TransitionToState(PlayerState.Moving);
                }
                else
                {
                    _player.TransitionToState(PlayerState.Idle);
                }

                _player.PlayerAudioComponent.PlaySound(PlayerAudioManager.PlayerAudioType.Land);
            }
        }
    }
}