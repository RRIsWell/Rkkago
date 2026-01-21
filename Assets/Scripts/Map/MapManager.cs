using UnityEngine;
using Unity.Netcode;

public class MapManager : NetworkBehaviour
{
    [SerializeField] MapConfig currentMapConfig;
    [SerializeField] private Stone stonePrefab;

    private MapRuleExecutor ruleExecutor;
    private bool stoneSpawned = false; // 돌 중복 스폰 방지

    public override void OnNetworkSpawn()
    {
        Debug.Log("MapManager OnNetworkSpawn, IsServer = " + IsServer);
        if (!IsServer) return;
        
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        if (stoneSpawned) return;

        Debug.Log("Client Connected → Spawn Stones");

        var layout = GameObject.Find("Map1Layout");
        if (layout == null)
        {
            Debug.LogError("Map1Layout 못 찾음");
            return;
        }

        ruleExecutor = layout.GetComponentInChildren<MapRuleExecutor>();
        InitializeSystems(layout.gameObject);

        SpawnAllStones(layout);

        stoneSpawned = true; // 중복 방지
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

        // 접속된 클라이언트 목록 확인
        var clients = NetworkManager.Singleton.ConnectedClientsList;
        if(clients.Count == 0) return;

        // P1은 호스트(0번), P2는 클라이언트(1번)에게 소유권 부여
        ulong p1Id = NetworkManager.ServerClientId; // 🔥 이게 더 안전
        ulong p2Id = p1Id;

        foreach (var c in clients)
        {
            if (c.ClientId != p1Id)
            {
                p2Id = c.ClientId;
                break;
            }
        }

        // 각각의 스폰 포인트 그룹에서 소환
        SpawnByTeam(layout, "Player1SpawnPoints", p1Id, 1);
        SpawnByTeam(layout, "Player2SpawnPoints", p2Id, 2);
    }

    void SpawnByTeam(GameObject layout, string pointName, ulong ownerId, int teamId)
    {
        var spawnRoot = layout.transform.Find(pointName);
        if (spawnRoot == null) return;

        foreach (Transform spawnPoint in spawnRoot)
        {
            if(spawnPoint == spawnRoot) continue;

            var stone = Instantiate(stonePrefab, spawnPoint.position, Quaternion.identity);
            
            // 소유권을 지정하여 스폰
            stone.GetComponent<NetworkObject>().SpawnWithOwnership(ownerId);

            // 팀 설정
            stone.SetTeam(teamId); 
            ruleExecutor.RegisterStone(stone);
        }
    }
}