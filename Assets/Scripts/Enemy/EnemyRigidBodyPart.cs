using UnityEngine;

public class EnemyRigidBodyPart : MonoBehaviour
{
    public EnemyController enemyController;

    void Start()
    {
        if (enemyController == null)
        {
            enemyController = GetComponentInParent<EnemyController>();
        }

        if (enemyController == null)
        {
            Debug.LogError("EnemyController not found in parent or self.");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (enemyController.isRagdoll)
        {
            // Debug.Log("Enemy collided with wall while in ragdoll state.");
            enemyController.PlayGroundHitSound();
        }
    }
}