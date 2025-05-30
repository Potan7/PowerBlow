using System;

namespace Player.State
{
    public enum PlayerState
    {
        Idle = 0,
        Moving,
        Falling,
        Sliding,
        Vaulting,
        ClimbingUp,
    }
}
