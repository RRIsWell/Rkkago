using UnityEngine;

public class MapManager : MonoBehaviour
{
    [SerializeField] MapConfig currentMapConfig;
    [SerializeField] GameObject mapLayoutPrefab;

    void Start()
    {
        var layout = Instantiate(mapLayoutPrefab);
        InitializeSystems(layout);
    }

    void InitializeSystems(GameObject layout)
    {
        layout.GetComponentInChildren<MapBoundary>()
            ?.Init(currentMapConfig);

        layout.GetComponentInChildren<SurfaceController>()
            ?.Init(currentMapConfig);
        
        if(currentMapConfig.useObstacle)
        {
            layout.GetComponentInChildren<ObstacleSpawner>()
                ?.Init(currentMapConfig);
        }
    }
}