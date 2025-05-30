using System.Collections.Generic;
using Player;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    public EnemyController enemyPrefab; // 적 프리팹

    public float spawnDistance = 40f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // 이미 인스턴스가 존재하면 중복 생성 방지
        }
    }

    HashSet<EnemyPoint> enemyPoints = new HashSet<EnemyPoint>();
    HashSet<EnemyController> enemies = new HashSet<EnemyController>();
    List<EnemyPoint> willRemovePoint = new List<EnemyPoint>();

    void Update()
    {
        Vector3 playerPosition = PlayerController.Instance.transform.position;
        willRemovePoint.Clear();

        foreach (var enemyPoint in enemyPoints)
        {
            if (Vector3.Distance(playerPosition, enemyPoint.transform.position) <= spawnDistance)
            {
                willRemovePoint.Add(enemyPoint);
            }
        }

        foreach (var enemyPoint in willRemovePoint)
        {
            SpawnEnemy(enemyPoint);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void RegisterEnemyPoint(EnemyPoint enemyPoint)
    {
        if (!enemyPoints.Contains(enemyPoint))
        {
            enemyPoints.Add(enemyPoint);
        }
    }

    public void UnregisterEnemyPoint(EnemyPoint enemyPoint)
    {
        if (enemyPoints.Contains(enemyPoint))
        {
            enemyPoints.Remove(enemyPoint);
            Destroy(enemyPoint.gameObject); // 적 포인트 제거
        }
    }

    public void EnemyDied(EnemyController enemy)
    {
        enemy.Disable();
        enemy.gameObject.SetActive(false);
    }

    public void SpawnEnemy(EnemyPoint enemyPoint)
    {
        var enemy = GetEnemy();
        if (enemy != null)
        {
            enemy.transform.SetPositionAndRotation(enemyPoint.transform.position, enemyPoint.transform.rotation);
            enemy.gameObject.SetActive(true);
            enemy.Active();

            UnregisterEnemyPoint(enemyPoint); // 적이 생성되면 해당 포인트에서 제거
        }
        else
        {
            Debug.LogWarning("활성화된 적이 없습니다. 적을 생성할 수 없습니다.");
        }

    }

    EnemyController GetEnemy()
    {
        foreach (var enemy in enemies)
        {
            if (!enemy.gameObject.activeSelf)
            {
                return enemy;
            }
        }
        var newEnemy = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
        newEnemy.Init();
        newEnemy.gameObject.SetActive(false);
        enemies.Add(newEnemy);
        return newEnemy;
    }

    public void ClearEnemies()
    {
        foreach (var enemy in enemies)
        {
            enemy.Disable();
            enemy.gameObject.SetActive(false);
        }
    }

}