using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject[] obstaclePrefabs;

    [Header("Spawn Area")]
    [SerializeField] private BoxCollider2D spawnArea;

    public void Init(MapConfig config)
    {
        if (!config.useObstacle)
            return;

        SpawnInternal(config);
    }

    void SpawnInternal(MapConfig config)
    {
        int count = config.obstacleCount;

        for (int i = 0; i < count; i++)
        {
            Vector2 pos = GetValidRandomPosition(config.obstacleMargin);

            GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
            Instantiate(prefab, pos, Quaternion.identity, transform);
        }
    }

    Vector2 GetValidRandomPosition(float margin)
    {
        Bounds b = spawnArea.bounds;

        return new Vector2(
            Random.Range(b.min.x + margin, b.max.x - margin),
            Random.Range(b.min.y + margin, b.max.y - margin)
        );
    }
}