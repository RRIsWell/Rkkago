using UnityEngine;
using Unity.Netcode;

public class MapManager : MonoBehaviour
{
    [SerializeField] MapConfig currentMapConfig;
    [SerializeField] GameObject mapLayoutPrefab;

    private MapRuleExecutor ruleExecutor;

    void Start()
    {
        var layout = Instantiate(mapLayoutPrefab);

        ruleExecutor = layout.GetComponentInChildren<MapRuleExecutor>();
    
        InitializeSystems(layout);

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