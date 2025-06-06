using UnityEngine;

namespace Player.Component
{
    public class PlayerBody : MonoBehaviour
    {
        PlayerController player;

        void Start()
        {
            player = PlayerController.Instance;
        }

        void OnTriggerEnter(Collider other)
        {
            // Debug.Log("Collision detected with: " + other.gameObject.name);
            if (other.CompareTag("Enemy") && other.TryGetComponent<EnemyController>(out var enemy))
            {
                if (enemy.isRagdoll)
                {
                    // 적이 쓰러져 있다면 무시
                    return;
                }
                // 적과 충돌 시 처리 로직
                Debug.Log("Hit by enemy attack!");
                player.StatsManagerComponent.TakeDamage(enemy.attackDamage);

                // 적의 반대방향으로 내가 튕겨나가기
                Vector3 knockbackDirection = (transform.position - other.transform.position).normalized;
                knockbackDirection.y = 0; // 수직 방향은 제외
                player.CharacterControllerComponent.Move(knockbackDirection * 0.3f);
            }
        }
    }

}
