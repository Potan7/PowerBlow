using UnityEngine;

public class BigEnemy : EnemyController
{
    void Start()
    {
        Active();
    }

    public override void Impact(Vector3 force)
    {
        // BigEnemy는 날아가지 않음
        overchargeTime = overchargeDelay; // 과충전 딜레이 설정
    }
}
