using System.Collections;
using Player;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Moving,
        Attacking,
        Ragdoll
    }

    Animator _animator;
    CharacterController _characterController;
    NavMeshAgent _navMeshAgent;

    public Rigidbody ragdollSpineRigidbody;

    EnemyState currentState = EnemyState.Idle;

    private Collider[] ragdollColliders;
    private Rigidbody[] ragdollRigidbodies;

    Coroutine disableRagdollCoroutine;
    public float disableRagdollDelay = 4f;

    public float moveSpeed = 5f;
    public float playerFindDistance = 10f;
    public float playerAttackDistance = 2f;
    public float removeDistance = 100f;

    public Collider[] attackColliders;

    public Vector3 debugForce;

    // int _animatorIsRagdollHash = Animator.StringToHash("isRagdoll");
    int animatorImpactHash = Animator.StringToHash("impact");
    int animatorIsMovingHash = Animator.StringToHash("isMoving");
    int animatorGetupHash = Animator.StringToHash("getUp");
    int animatorIsAttackingHash = Animator.StringToHash("isAttacking");


    void Awake()
    {
        if (_characterController == null)
            Init();
    }

    public void Init()
    {
        _animator = GetComponent<Animator>();
        _characterController = GetComponent<CharacterController>();
        _navMeshAgent = GetComponent<NavMeshAgent>();

        ragdollColliders = GetComponentsInChildren<Collider>();
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        Disable();
    }

    public void Disable()
    {
        _animator.enabled = false;
        _navMeshAgent.enabled = false;
        _characterController.enabled = false;

        foreach (var collider in ragdollColliders)
        {
            collider.enabled = false;
        }
        foreach (var rb in ragdollRigidbodies)
        {
            rb.isKinematic = true;
        }

        currentState = EnemyState.Idle;
    }

    public void Active()
    {

        foreach (var collider in ragdollColliders)
        {
            collider.enabled = false;
        }
        foreach (var rb in ragdollRigidbodies)
        {
            rb.isKinematic = true;
        }

        _animator.enabled = true;
        _navMeshAgent.enabled = true;
        _characterController.enabled = true;
        currentState = EnemyState.Idle;
    }

    void Update()
    {
        if (transform.position.y < -5f)
        {
            PlayerController.Instance.EnemyIsDead();
            EnemyManager.Instance.EnemyDied(this);
            // Destroy(gameObject);
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
            if (distance < playerAttackDistance)
            {
                // 플레이어와 가까워지면 공격 상태로 전환
                _animator.SetBool(animatorIsMovingHash, false);
                _animator.SetBool(animatorIsAttackingHash, true);
                currentState = EnemyState.Attacking;
                _navMeshAgent.ResetPath();
            }
            else if (distance < playerFindDistance)
            {
                // 플레이어를 찾음
                _animator.SetBool(animatorIsMovingHash, true);
                _animator.SetBool(animatorIsAttackingHash, false);
                currentState = EnemyState.Moving;
                _navMeshAgent.SetDestination(playerPos);
            }
            else if (distance > removeDistance)
            {
                // 플레이어가 너무 멀리 있으면 적 제거
                EnemyManager.Instance.EnemyDied(this);
                // Destroy(gameObject);
            }
            else
            {
                currentState = EnemyState.Idle;
                _navMeshAgent.ResetPath();
                _animator.SetBool(animatorIsMovingHash, false);
                _animator.SetBool(animatorIsAttackingHash, false);
            }

            for (int i = 0; i < attackColliders.Length; i++)
            {
                attackColliders[i].enabled = currentState == EnemyState.Attacking;
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
        Debug.Log("Impact with force: " + force);
        if (disableRagdollCoroutine != null)
        {
            StopCoroutine(disableRagdollCoroutine);
            disableRagdollCoroutine = null;
        }
        SetRagdollActive(true);
        currentState = EnemyState.Ragdoll;

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
        yield return new WaitForSeconds(0.5f); // 래그돌 상태에서 잠시 대기
        bool isGround = false;
        while (!isGround)
        {
            // 캐릭터가 바닥에 닿았는지 확인
            isGround = Physics.Raycast(ragdollSpineRigidbody.position, Vector3.down, 0.5f);
            yield return null;
        }
        yield return new WaitForSeconds(delay); // 지정된 시간만큼 기다림

        // 래그돌 상태에서 캐릭터의 방향 판정
        // ragdollSpineRigidbody를 기준으로 판정합니다.
        // ragdollSpineRigidbody.transform.up이 월드 위쪽을 향하면 누워있는 것(하늘 보기)
        // ragdollSpineRigidbody.transform.up이 월드 아래쪽을 향하면 엎드려있는 것(바닥 보기)
        float dotUp = Vector3.Dot(ragdollSpineRigidbody.transform.up, Vector3.up);

        float getUpAnimationDuration = 2.0f;
        // 방향을 먼저 결정하고 애니메이션 파라미터를 설정합니다.
        if (dotUp > 0) // 척추의 위쪽이 월드 위쪽을 향하면 (등으로 누워있음)
        {
            _animator.SetFloat(animatorGetupHash, 0.5f); // 하늘 보기 (누워서 일어남)
            // Debug.Log("Lying on back, getUp = 1");
        }
        else // 척추의 위쪽이 월드 아래쪽을 향하면 (엎드려 있음)
        {
            _animator.SetFloat(animatorGetupHash, 0f); // 바닥 보기 (엎드려서 일어남)
            getUpAnimationDuration = 7f;
        }

        // 래그돌의 위치와 회전을 캐릭터 컨트롤러에 적용
        // 위치는 척추의 위치를 사용
        transform.position = ragdollSpineRigidbody.position;

        // 회전은 캐릭터가 앞을 보도록 설정 (선택 사항, 애니메이션에서 처리할 수도 있음)
        // 래그돌의 척추가 향하던 방향을 유지하거나, 특정 방향으로 정렬할 수 있습니다.
        // 여기서는 척추의 전방 방향을 캐릭터의 전방으로 설정하려고 시도합니다.
        // 단, y축 회전만 적용하여 캐릭터가 기울어지지 않도록 합니다.
        Vector3 spineForward = ragdollSpineRigidbody.transform.forward;
        spineForward.y = 0; // 수평 방향만 사용
        if (spineForward.sqrMagnitude > 0.01f) // 방향이 유효한 경우에만
        {
            transform.rotation = Quaternion.LookRotation(spineForward.normalized, Vector3.up);
        }
        // 또는, 애니메이션이 시작될 때 자연스럽게 정렬되도록 회전은 그대로 둘 수도 있습니다.
        // transform.rotation = ragdollSpineRigidbody.rotation; // 이렇게 하면 래그돌의 기울어짐까지 반영될 수 있음


        // 척추 리지드바디의 로컬 위치를 초기화 (필수는 아닐 수 있음, 모델 구조에 따라)
        // ragdollSpineRigidbody.transform.localPosition = Vector3.zero; // 이 라인은 문제를 일으킬 수 있으므로 주의

        // SetRagdollActive(false)를 호출하기 전에 애니메이션 트리거를 설정하여
        // 애니메이터가 활성화될 때 바로 애니메이션을 재생하도록 합니다.
        _animator.SetTrigger(animatorImpactHash);

        SetRagdollActive(false); // 여기서 CharacterController와 Animator가 활성화됩니다.

        // NavMeshAgent 활성화는 CharacterController가 안정된 후, 또는 애니메이션 완료 후가 더 적절할 수 있습니다.
        _navMeshAgent.enabled = false; // 일단 비활성화
        // 캐릭터가 완전히 일어선 후에 NavMeshAgent를 활성화하는 것이 좋습니다.
        // 예를 들어, 일어서는 애니메이션의 길이를 알고 있다면 그만큼 더 기다립니다.
        // float getUpAnimationDuration = _animator.GetCurrentAnimatorStateInfo(0).length; // 현재 상태 길이를 가져오는 것은 복잡할 수 있음
        yield return new WaitForSeconds(getUpAnimationDuration);

        _navMeshAgent.enabled = true; // NavMeshAgent 활성화
        currentState = EnemyState.Idle; // 상태를 Idle로 변경
    }
}
