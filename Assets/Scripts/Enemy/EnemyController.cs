using System.Collections;
using Player;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    Animator _animator;
    CharacterController _characterController;
    NavMeshAgent _navMeshAgent;

    public Rigidbody ragdollSpineRigidbody;

    private Collider[] ragdollColliders;
    private Rigidbody[] ragdollRigidbodies;

    Coroutine disableRagdollCoroutine;
    public float disableRagdollDelay = 4f;

    public float moveSpeed = 5f;
    public float playerFindDistance = 10f;
    public float playerAttackDistance = 2f;

    public Vector3 debugForce;

    // int _animatorIsRagdollHash = Animator.StringToHash("isRagdoll");
    int _animatorImpactHash = Animator.StringToHash("impact");
    int _animatorIsMovingHash = Animator.StringToHash("isMoving");


    void Start()
    {
        _animator = GetComponent<Animator>();
        _characterController = GetComponent<CharacterController>();
        _navMeshAgent = GetComponent<NavMeshAgent>();

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
        if (transform.position.y < -10f)
        {
            Destroy(gameObject);
            return;
        }

        if (!_characterController.enabled) return;

        if (!_characterController.isGrounded)
        {
            // 중력
            _characterController.Move(Physics.gravity * Time.deltaTime);
        }
        else if (_navMeshAgent.enabled)
        {
            Vector3 playerPos = PlayerController.Instance.transform.position;
            float distance = Vector3.Distance(transform.position, playerPos);
            if (distance < playerFindDistance)
            {
                // 플레이어를 찾음
                _animator.SetBool(_animatorIsMovingHash, true);
                _navMeshAgent.SetDestination(playerPos);
            }
            else
            {
                _navMeshAgent.ResetPath();
                _animator.SetBool(_animatorIsMovingHash, false);
            }
        }
        
    }


    void OnFootstep(AnimationEvent animationEvent)
    {

    }

    void SetRagdollActive(bool isActive)
    {
        _animator.enabled = !isActive;
        _navMeshAgent.enabled = !isActive;

        foreach (var collider in ragdollColliders)
        {
            collider.enabled = isActive;
        }
        foreach (var rb in ragdollRigidbodies)
        {
            rb.isKinematic = !isActive;
        }

        _characterController.enabled = !isActive;
    }

    public void Impact(Vector3 force)
    {
        if (disableRagdollCoroutine != null)
        {
            StopCoroutine(disableRagdollCoroutine);
            disableRagdollCoroutine = null;
        }
        SetRagdollActive(true);

        ragdollSpineRigidbody.AddForce(force, ForceMode.Impulse);
        disableRagdollCoroutine = StartCoroutine(DisableRagdoll(disableRagdollDelay));
    }

    [ContextMenu("Debug Impact")]
    public void ImpactDebug()
    {
        Impact(debugForce);
    }

    IEnumerator DisableRagdoll(float delay)
    {
        yield return new WaitForSeconds(delay);

        transform.position = ragdollSpineRigidbody.position;
        ragdollSpineRigidbody.transform.localPosition = Vector3.zero;

        _animator.SetTrigger(_animatorImpactHash);

        SetRagdollActive(false);
        _navMeshAgent.enabled = false;
        yield return new WaitForSeconds(7f);
        _navMeshAgent.enabled = true;
    }
}
