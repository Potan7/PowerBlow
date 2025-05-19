using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    Animator _animator;
    CharacterController _characterController;

    public Rigidbody ragdollSpineRigidbody;

    private Collider[] ragdollColliders;
    private Rigidbody[] ragdollRigidbodies;

    Coroutine disableRagdollCoroutine;
    public float disableRagdollDelay = 4f;

    // int _animatorIsRagdollHash = Animator.StringToHash("isRagdoll");
    int _animatorImpactHash = Animator.StringToHash("impact");
    int _animatorIsMovingHash = Animator.StringToHash("isMoving");

    Vector3 moveDirection;

    void Start()
    {
        _animator = GetComponent<Animator>();
        _characterController = GetComponent<CharacterController>();

        ragdollColliders = GetComponentsInChildren<Collider>();
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();

        foreach (var collider in ragdollColliders)
        {
            collider.enabled = false;
        }
        foreach (var rb in ragdollRigidbodies)
        {
            rb.isKinematic = true;
        }

        _characterController.enabled = true;
    }

    void Update()
    {
        if (!_characterController.enabled) return;

        float verticalMovement = 0f;
        float horizontalMovement = 0f;

        if (!_characterController.isGrounded)
        {
            // 중력
            verticalMovement += Physics.gravity.y;
        }

        // 이동 처리
        moveDirection = new Vector3(moveDirection.x * horizontalMovement, verticalMovement, moveDirection.z * horizontalMovement);
        _characterController.Move(moveDirection * Time.deltaTime);
    }


    void OnFootstep(AnimationEvent animationEvent)
    {

    }

    void SetRagdollActive(bool isActive)
    {
        _animator.enabled = !isActive;
        _characterController.enabled = !isActive;

        foreach (var collider in ragdollColliders)
        {
            collider.enabled = isActive;
        }
        foreach (var rb in ragdollRigidbodies)
        {
            rb.isKinematic = !isActive;
        }
    }

    public void Impact(Vector3 force)
    {
        SetRagdollActive(true);

        ragdollSpineRigidbody.AddForce(force, ForceMode.Impulse);

        disableRagdollCoroutine = StartCoroutine(DisableRagdoll(disableRagdollDelay));
    }

    [ContextMenu("Debug Impact")]
    public void ImpactDebug()
    {
        Vector3 force = new Vector3(0, 10, 10);
        Impact(force);
    }

    IEnumerator DisableRagdoll(float delay)
    {
        yield return new WaitForSeconds(delay);

        transform.position = ragdollSpineRigidbody.position;
        ragdollSpineRigidbody.transform.localPosition = Vector3.zero;

        _animator.SetTrigger(_animatorImpactHash);

        SetRagdollActive(false);
    }
}
