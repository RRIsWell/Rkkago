using UnityEngine;
using Unity.Netcode;

public class MapManager : NetworkBehaviour
{
    [SerializeField] MapConfig currentMapConfig;
    [SerializeField] private Stone stonePrefab;

    private MapRuleExecutor ruleExecutor;

    public override void OnNetworkSpawn()
    {
        Debug.Log("MapManager OnNetworkSpawn, IsServer = " + IsServer);
        if (!IsServer) return;
        
        var layout = GameObject.Find("Map1Layout");
        ruleExecutor = layout.GetComponentInChildren<MapRuleExecutor>();
    
        InitializeSystems(layout.gameObject);

        // 서버만 돌 등록
        if(NetworkManager.Singleton.IsServer)
        {
            SpawnAllStones(layout);
        }
    }

    void InitializeSystems(GameObject layout)
    {
        if(ruleExecutor != null)
            ruleExecutor.Init(currentMapConfig);
        
        layout.GetComponentInChildren<MapBoundary>()
            ?.Init(currentMapConfig, ruleExecutor);

        layout.GetComponentInChildren<SurfaceController>()
            ?.Init(currentMapConfig);
        
        if(currentMapConfig.useObstacle)
        {
            layout.GetComponentInChildren<ObstacleSpawner>()
                ?.Init(currentMapConfig);
        }
    }

    void SpawnAllStones(GameObject layout)
    {
        if(!IsServer) return;

        var spawnRoot = layout.transform.Find("Player1SpawnPoints");
        if(spawnRoot == null)
        {
            Debug.LogError("Player1SpawnPoints 발견되지 않음");
            return;
        }

        foreach (Transform spawnPoint in spawnRoot)
        {
            // Player1SpawnPoints 자기 자신은 스킵
            if(spawnPoint == spawnRoot) continue;

            var stone = Instantiate(
                stonePrefab,
                spawnPoint.position,
                Quaternion.identity
            );

            stone.GetComponent<NetworkObject>().Spawn();

            ruleExecutor.RegisterStone(stone);
        }
    }
}