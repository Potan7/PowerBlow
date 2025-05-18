using UnityEngine;

namespace Player.State
{
    public class IdleState : IPlayerState
{
    private PlayerController _player;

    public IdleState(PlayerController player)
    {
        _player = player;
    }

    public void Enter(PlayerController player)
    {
        // Debug.Log("Entering Idle State");
        _player = player; // PlayerController 참조 설정
        _player.PlayerAnimatorComponent.SetAnim(PlayerState.Moving, false); // Idle 애니메이션 (Moving false)
        _player.PlayerAnimatorComponent.SetAnim(PlayerState.Sliding, false);
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

        // 이동 입력 확인
        if (_player.MoveInput != Vector2.zero)
        {
            _player.TransitionToState(_player.MovingState, PlayerState.Moving);
            return;
        }

        // 점프 입력 확인
        if (_player.JumpRequested && _player.CharacterControllerComponent.isGrounded)
        {
            _player.TransitionToState(_player.JumpingState, PlayerState.Jumping);
            _player.JumpRequested = false; // 요청 처리됨
            return;
        }

        // 슬라이드(웅크리기) 입력 확인
        if (_player.CrouchActive && _player.CharacterControllerComponent.isGrounded) // Idle 상태에서 웅크리기 시도
        {
            // Idle 상태에서 웅크리기는 특별한 동작이 없다면 무시하거나, 웅크린 Idle 상태로 전환 가능
            // 여기서는 슬라이드로 바로 이어지지 않으므로, 만약 이동 입력이 들어오면 MovingState에서 슬라이드 처리
        }


        // 뛰어넘기 조건 확인
        if (_player.CharacterControllerComponent.isGrounded && _player.MoveInput != Vector2.zero) // 뛰어넘기는 이동 중에만 시도
        {
            // 이 로직은 MovingState로 옮기는 것이 더 적합할 수 있음
        }

        // 땅에 있지 않다면 Falling 상태로 전환
        if (!_player.CharacterControllerComponent.isGrounded && _player.VerticalVelocity <= 0) // 점프 직후가 아닌 낙하
        {
            _player.TransitionToState(_player.FallingState, PlayerState.Falling);
            return;
        }

        // 최종 이동 적용 (주로 수직 이동)
        Vector3 verticalMovement = Vector3.up * _player.VerticalVelocity * Time.deltaTime;
        _player.CharacterControllerComponent.Move(verticalMovement);
    }

    public void Exit()
    {
        // Debug.Log("Exiting Idle State");
    }
}
}