using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    // TODO: 나중에 더 구현
    public void Init(MapConfig config)
    {
        if(!config.useObstacle)
            return;

        SpawnInternal(config);
    }

    void SpawnInternal(MapConfig config)
    {
        // 실제 장애물 생성 로직
        // prefab instantiate, 위치 랜덤 등
    }
}
