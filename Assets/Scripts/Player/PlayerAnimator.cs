using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    readonly int isMovingHash = Animator.StringToHash("isMoving");
    readonly int isJumpHash = Animator.StringToHash("isJump");
    readonly int isFallingHash = Animator.StringToHash("isFalling");
    readonly int isSlidingHash = Animator.StringToHash("isSliding");
    
    readonly int fowardMovingHash = Animator.StringToHash("forwardMoveState");
    readonly int rightMovingHash = Animator.StringToHash("rightMoveState");

    public void SetAnim(PlayerState state, bool value = true)
    {
        switch (state)
        {
            case PlayerState.Idle:
                animator.SetBool(isMovingHash, value);
                break;
            case PlayerState.Moving:
                animator.SetBool(isMovingHash, value);
                break;
            case PlayerState.Jumping:
                animator.SetTrigger(isJumpHash);
                break;
            case PlayerState.Falling:
                animator.SetBool(isFallingHash, value);
                break;
            case PlayerState.Sliding:
                animator.SetBool(isSlidingHash, value);
                break;
        }
    }

    public void SetDirection(Vector2 direction)
    {
        animator.SetFloat(fowardMovingHash, direction.y);
        animator.SetFloat(rightMovingHash, direction.x);
    }

    void OnFootstep(AnimationEvent animationEvent)
    {
        
    }
    
}
