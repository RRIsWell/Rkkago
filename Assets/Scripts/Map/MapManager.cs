using UnityEngine;

public class MapManager : MonoBehaviour
{
    [Header("Map References")]
    public MapConfig mapConfig;
    public GameObject mapLayoutPrefab;

    void Start()
    {
        ApplyMapConfig();
    }

    void ApplyMapConfig()
    {
        FindObjectOfType<MapBoundary>()?.Init(mapConfig);
        FindObjectOfType<SurfaceController>()?.Init(mapConfig);
        FindObjectOfType<ObstacleSpawner>()?.Init(mapConfig);
    }
}
