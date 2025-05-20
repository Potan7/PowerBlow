using System;

namespace Player.State
{
    public enum PlayerState
    {
        Idle = 0,
        Moving,
        Jumping,
        Falling,
        Sliding,
        Vaulting,
        ClimbingUp,
    }
}
