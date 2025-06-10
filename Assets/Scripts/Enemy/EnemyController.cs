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

    public int attackDamage = 20;

    public bool isRagdoll = false;

    public Collider[] attackColliders;

    public Vector3 debugForce;

    // int _animatorIsRagdollHash = Animator.StringToHash("isRagdoll");
    int animatorImpactHash = Animator.StringToHash("impact");
    int animatorIsMovingHash = Animator.StringToHash("isMoving");
    int animatorGetupHash = Animator.StringToHash("getUp");
    int animatorIsAttackingHash = Animator.StringToHash("isAttacking");

    public AudioSource footAudioSource;
    public AudioSource bodyAudioSource;
    public AudioClip[] footstepSound;
    public AudioClip[] groundHitSound;

    public float overchargeDelay = 1f; // 과충전 딜레이
    public float overchargeTime = 0;

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
        isRagdoll = false;
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

        if (!_characterController.enabled || currentState == EnemyState.Ragdoll || !_navMeshAgent) return;

        if (isRagdoll)
        {
            return;
        }

        if (overchargeTime > 0)
        {
            overchargeTime -= Time.deltaTime;
            if (overchargeTime <= 0)
            {
                overchargeTime = 0;
            }
        }

        if (!_characterController.isGrounded)
        {
            // 중력
            _characterController.Move(Physics.gravity * Time.deltaTime);
        }
        else if (_navMeshAgent.enabled) // NavMeshAgent가 활성화되어 있을 때만 이동 로직 실행
        {
            Vector3 playerPos = PlayerController.Instance.transform.position;
            float distance = Vector3.Distance(transform.position, playerPos);

            if (distance < playerAttackDistance)
            {
                // 플레이어와 가까워지면 공격 상태로 전환
                transform.LookAt(new Vector3(playerPos.x, transform.position.y, playerPos.z)); // 적이 플레이어를 지속적으로 바라보도록 함
                _animator.SetBool(animatorIsMovingHash, false);
                _animator.SetBool(animatorIsAttackingHash, true);
                currentState = EnemyState.Attacking;
                if (_navMeshAgent.isOnNavMesh) // NavMeshAgent가 NavMesh 위에 있을 때만 경로를 초기화
                {
                    _navMeshAgent.ResetPath();
                }
            }
            else if (distance < playerFindDistance)
            {
                // 플레이어를 찾음
                _navMeshAgent.SetDestination(playerPos);
                _animator.SetBool(animatorIsMovingHash, true);
                _animator.SetBool(animatorIsAttackingHash, false);
                currentState = EnemyState.Moving;

            }
            else if (distance > removeDistance)
            {
                // 플레이어가 너무 멀리 있으면 적 제거
                EnemyManager.Instance.EnemyDied(this);
                // Destroy(gameObject);
            }
            else
            {
                // 적이 플레이어를 찾지 못했을 때
                currentState = EnemyState.Idle;
                if (_navMeshAgent.isOnNavMesh)
                {
                    _navMeshAgent.ResetPath();
                }
                _animator.SetBool(animatorIsMovingHash, false);
                _animator.SetBool(animatorIsAttackingHash, false);
            }

            // 공격 판정
            for (int i = 0; i < attackColliders.Length; i++)
            {
                attackColliders[i].enabled = currentState == EnemyState.Attacking;
            }
        }
    }

    void OnFootstep(AnimationEvent animationEvent)
    {
        if (footAudioSource != null && footstepSound.Length > 0)
        {
            int randomIndex = Random.Range(0, footstepSound.Length);
            footAudioSource.clip = footstepSound[randomIndex];
            footAudioSource.Play();
        }
    }

    void SetRagdollActive(bool isActive)
    {
        foreach (var col in ragdollColliders)
        {
            col.enabled = isActive;
        }
        foreach (var rb in ragdollRigidbodies)
        {
            rb.isKinematic = !isActive;
        }

        _animator.enabled = !isActive;
        _characterController.enabled = !isActive;
        isRagdoll = isActive;

        // NavMeshAgent는 비활성화만 하고 활성화는 외부에서 제어하도록
        if (isActive)
        {
            if (_navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
            {
                // 끄기 전에 경로 초기화
                _navMeshAgent.ResetPath();
            }
            _navMeshAgent.enabled = false;
        }
    }

    public virtual void Impact(Vector3 force)
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
        overchargeTime = overchargeDelay; // 과충전 딜레이 설정
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
        float groundCheckStartTime = Time.time;
        while (!isGround)
        {
            // 캐릭터가 바닥에 닿았는지 확인
            isGround = Physics.Raycast(ragdollSpineRigidbody.position, Vector3.down, 0.7f);
            // 타임아웃
            if (Time.time - groundCheckStartTime > 7f)
            {
                Debug.LogWarning($"{gameObject.name}이 너무 오랫동안 바닥에 닿지 않았습니다. 래그돌 상태를 비활성화합니다.");
                break;
            }
            yield return null;
        }
        yield return new WaitForSeconds(delay); // 지정된 시간만큼 기다림

        // 적이 navMesh 위에 있는지 확인
        float navMeshCheckRadius = 1.5f;
        Vector3 potentialPosition = ragdollSpineRigidbody.position;

        if (!NavMesh.SamplePosition(potentialPosition, out NavMeshHit navHit, navMeshCheckRadius, NavMesh.AllAreas))
        {
            // 유효한 NavMesh 위치에 있지 않으면 적이 죽은 것으로 간주
            Debug.LogWarning($"{gameObject.name}이 {potentialPosition}에 NavMesh가 없습니다. 제거합니다.");
            PlayerController.Instance.EnemyIsDead(); // 플레이어에게 알림
            EnemyManager.Instance.EnemyDied(this);  // 비활성화/파괴 처리
            yield break; // 코루틴 중지
        }

        // NavMeshHit에서 유효한 위치로 이동
        transform.position = navHit.position;


        // 래그돌 상태에서 캐릭터의 방향 판정
        float dotUp = Vector3.Dot(ragdollSpineRigidbody.transform.up, Vector3.up);
        float getUpAnimationDuration = 2.0f;

        if (dotUp > 0)
        {
            _animator.SetFloat(animatorGetupHash, 0.5f);
        }
        else
        {
            _animator.SetFloat(animatorGetupHash, 0f);
            getUpAnimationDuration = 7f;
        }

        // 래그돌 비활성화
        SetRagdollActive(false);
        _navMeshAgent.enabled = false; // 애니메이션 전에 NavMeshAgent를 한번더 비활성화

        // 캐릭터의 방향을 설정
        Vector3 spineForward = ragdollSpineRigidbody.transform.forward;
        spineForward.y = 0;
        if (spineForward.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(spineForward.normalized, Vector3.up);
        }
        else // 만약 spineForward가 거의 0 벡터라면 플레이어 방향으로 회전
        {
            Vector3 playerDir = PlayerController.Instance.transform.position - transform.position;
            playerDir.y = 0;
            if (playerDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(playerDir.normalized, Vector3.up);
            }
        }

        // NavMeshAgent의 위치 최신화 시도
        if (!_navMeshAgent.Warp(transform.position))
        {
            Debug.LogWarning($"{gameObject.name}이 NavMeshAgent를 Warp할 수 없습니다. NavMesh 위치를 다시 샘플링합니다.");
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit finalWarpHit, 0.5f, NavMesh.AllAreas))
            {
                transform.position = finalWarpHit.position;
                _navMeshAgent.Warp(finalWarpHit.position);
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}이 {transform.position}에 NavMesh가 없습니다. 제거합니다.");
                PlayerController.Instance.EnemyIsDead();
                EnemyManager.Instance.EnemyDied(this);
                yield break;
            }
        }

        _animator.SetTrigger(animatorImpactHash);

        yield return new WaitForSeconds(getUpAnimationDuration);

        // 혹시 애니메이션 중 위치가 바뀔 수 있으니 다시 측정
        if (NavMesh.SamplePosition(transform.position, out navHit, 0.5f, NavMesh.AllAreas))
        {
            transform.position = navHit.position; // Ensure it's on a valid spot
            _navMeshAgent.Warp(navHit.position); // Sync agent to this spot
            _navMeshAgent.enabled = true;
            currentState = EnemyState.Idle;
        }
        else
        {
            Debug.LogError($"{gameObject.name}이 최종 NavAgent 활성화 전에 NavMesh에 배치할 수 없습니다. 제거합니다.");
            PlayerController.Instance.EnemyIsDead();
            EnemyManager.Instance.EnemyDied(this);
            yield break;
        }
    }

    public void PlayGroundHitSound()
    {
        if (bodyAudioSource != null && groundHitSound.Length > 0)
        {
            if (bodyAudioSource.isPlaying) return; // 이미 재생 중이면 중복 재생 방지

            int randomIndex = Random.Range(0, groundHitSound.Length);
            bodyAudioSource.clip = groundHitSound[randomIndex];
            bodyAudioSource.Play();
        }
    }
}
