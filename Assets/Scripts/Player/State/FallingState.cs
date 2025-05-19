using UnityEngine;

namespace Player.State
{
    public class FallingState : IPlayerState
    {
        private PlayerController _player;

        // 생성자: PlayerController 참조를 받아 초기화합니다.
        public FallingState(PlayerController player)
        {
            _player = player;
        }

        // 상태 진입 시 호출되는 메서드
        public void Enter()
        {
            // Debug.Log("Entering Falling State");
            // 낙하 애니메이션 활성화
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Falling, true);

            // 공중에 있을 때는 슬라이딩 상태나 웅크리기 요청을 해제하는 것이 일반적입니다.
            // (oldController의 HandleAirborneLogic 참조)
            _player.CurrentSlidingVelocity = Vector3.zero; // 혹시 모를 슬라이딩 속도 초기화
            // _player.CrouchActive = false; // CrouchActive는 점프 시작 시 또는 다른 상태에서 해제될 수 있음
                                         // Falling 진입 시점에 항상 false로 할지 여부는 게임 디자인에 따라 결정
        }

        // 매 프레임 호출되는 메서드 (상태의 주요 로직)
        public void Execute()
        {
            // 1. 중력 적용: 수직 속도에 중력을 계속 더해줍니다.
            _player.VerticalVelocity += Physics.gravity.y * Time.deltaTime;

            // 2. 수평 이동 (공중 제어): 플레이어 입력에 따라 공중에서도 약간의 수평 이동을 허용합니다.
            Vector3 horizontalMovement = Vector3.zero;
            if (_player.MoveInput != Vector2.zero)
            {
                // 입력 방향을 월드 좌표 기준으로 변환
                Vector3 worldMoveDirection = _player.transform.TransformDirection(new Vector3(_player.MoveInput.x, 0, _player.MoveInput.y)).normalized;
                horizontalMovement = worldMoveDirection * _player.moveSpeed * Time.deltaTime; // 공중 제어 시 속도 계수를 다르게 할 수도 있음

                // 공중 이동 시 애니메이터 방향 업데이트
                _player.PlayerAnimatorComponent.SetDirection(_player.MoveInput);
            }
            else
            {
                // 이동 입력이 없을 때 애니메이터 방향 초기화 (선택적)
                _player.PlayerAnimatorComponent.SetDirection(Vector2.zero);
            }


            // 3. 최종 이동 적용: 수평 이동과 수직 이동(중력)을 합쳐 CharacterController에 적용합니다.
            Vector3 verticalMovement = Vector3.up * _player.VerticalVelocity * Time.deltaTime;
            _player.CharacterControllerComponent.Move(horizontalMovement + verticalMovement);

            // 4. 착지 감지: CharacterController가 땅에 닿았는지 확인합니다.
            if (_player.CharacterControllerComponent.isGrounded)
            {
                // 땅에 닿았다면 착지 처리 및 상태 전환
                ProcessLanding();
            }
        }

        // 상태 종료 시 호출되는 메서드
        public void Exit()
        {
            // Debug.Log("Exiting Falling State");
            // 낙하 애니메이션 비활성화
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Falling, false);
        }

        // 착지 처리 로직 (oldController의 ProcessLanding 참조)
        private void ProcessLanding()
        {
            // 착지 시 수직 속도를 약간 아래로 설정하여 안정적으로 땅에 붙도록 합니다.
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
                // 만약 착지 시 웅크리기 버튼만 눌려있고 이동 입력이 없다면, 웅크리기 상태를 해제할 수 있습니다.
                // (oldController: if (crouchActive) crouchActive = false;)
                // 이 로직은 게임 디자인에 따라 IdleState의 Enter에서 처리하거나 여기서 직접 CrouchActive를 false로 설정할 수 있습니다.
                // 여기서는 CrouchActive 상태를 유지하고, 각 상태에서 웅크리기 해제 조건을 명확히 하는 것을 선호합니다.
            }
        }
    }
}