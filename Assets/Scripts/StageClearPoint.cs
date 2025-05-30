using Player;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageClearPoint : MonoBehaviour
{
    public Transform clearFlag;
    PlayerController playerController;

    void Start()
    {
        playerController = PlayerController.Instance;
    }

    void Update()
    {
        // flag가 항상 플레이어를 바라보도록 설정
        clearFlag.LookAt(playerController.transform);
        // 아래로 90도 더 돌려야함
        clearFlag.Rotate(90, 0, 0);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 플레이어가 스테이지 클리어 지점에 도달했을 때
            Debug.Log("Stage Clear!");

            FindFirstObjectByType<PlayerUIManager>().EndGame(true);
            EnemyManager.Instance.ClearEnemies();
        }
    }
}
