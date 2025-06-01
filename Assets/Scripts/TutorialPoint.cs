using UnityEngine;

public class TutorialPoint : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 플레이어가 튜토리얼 포인트에 도달했을 때
            Debug.Log("Tutorial Point Reached!");

            // 튜토리얼 매니저를 찾아서 현재 단계 표시
            FindFirstObjectByType<TutorialManager>()?.ShowTutorialStep();
            Destroy(gameObject); // 튜토리얼 포인트를 제거
            
        }
    }
}
