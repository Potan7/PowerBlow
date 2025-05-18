using UnityEngine;

namespace Player.State
{
    public class FallingState : IPlayerState
    {
        private PlayerController _player;

        public FallingState(PlayerController player)
        {
            _player = player;
        }

        public void Enter(PlayerController player)
        {
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Falling, true);
        }

        public void Execute()
        {

        }

        public void Exit()
        {
            _player.PlayerAnimatorComponent.SetAnim(PlayerState.Falling, false);
        }
    }
}