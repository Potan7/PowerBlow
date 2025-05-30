using UnityEngine;

namespace Player
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
            if (other.CompareTag("Enemy"))
            {
                // 적과 충돌 시 처리 로직
                Debug.Log("Hit by enemy attack!");
                player.Health -= player.enemyDamage;

                // 적의 반대방향으로 내가 튕겨나가기
                Vector3 knockbackDirection = (transform.position - other.transform.position).normalized;
                knockbackDirection.y = 0; // 수직 방향은 제외
                player.CharacterControllerComponent.Move(knockbackDirection * 0.2f);
            }
        }
    }

}
