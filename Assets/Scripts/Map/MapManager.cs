using UnityEngine;
using Unity.Netcode;

public class MapManager : NetworkBehaviour
{
    [SerializeField] MapConfig currentMapConfig;

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
            RegisterAllStones();
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

    void RegisterAllStones()
    {
        if(!NetworkManager.Singleton.IsServer) return;

        var stones = FindObjectsOfType<Stone>();

        foreach(var stone in stones)
        {
            ruleExecutor.RegisterStone(stone);
        }
    }
}