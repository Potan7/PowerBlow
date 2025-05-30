using UnityEngine;

public class EnemyPoint : MonoBehaviour
{
    void Start()
    {
        // EnemyManager 인스턴스에 이 EnemyPoint를 등록
        EnemyManager.Instance.RegisterEnemyPoint(this);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 0.5f);
    }
}   
