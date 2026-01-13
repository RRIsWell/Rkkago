using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject[] obstaclePrefabs;
    public BoxCollider2D spawnArea;

    public void Init(MapConfig config)
    {
        if (!config.useObstacle) return;

        int count = Random.Range(3, 7); // 나중에 밸런스패치로 조정
        for (int i = 0; i < count; i++)
        {
            Vector2 pos = GetRandomInside();
            Instantiate(obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)], pos, Quaternion.identity, transform);
        }
    }

    Vector2 GetRandomInside()
    {
        Bounds b = spawnArea.bounds;
        return new Vector2(
            Random.Range(b.min.x, b.max.x),
            Random.Range(b.min.y, b.max.y)
        );
    }
}

