using UnityEngine;

public class EnemyController : MonoBehaviour
{
    Animator _animator;
    CharacterController _characterController;

    public Rigidbody ragdollSpineRigidbody;

    private Collider[] ragdollColliders;
    private Rigidbody[] ragdollRigidbodies;

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


    void OnFootstep(AnimationEvent animationEvent)
    {

    }

    public void SetRagdollActive(bool isActive)
    {

    }

    public void Impact(Vector3 force)
    {
        SetRagdollActive(true);

        ragdollSpineRigidbody.AddForce(force, ForceMode.Impulse);
    }
}
