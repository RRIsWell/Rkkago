using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class MapManager : NetworkBehaviour
{
    // 맵
    [SerializeField] MapConfig currentMapConfig;
    private MapRuleExecutor ruleExecutor;
    
    // 알 스폰
    [SerializeField] private GameObject stone1Prefab;
    [SerializeField] private GameObject stone2Prefab;
    private bool stoneSpawned = false; // 알 중복 스폰 방지

    private void Start()
    {
        SoundManager.Instance.PlayBGM(currentMapConfig.bgmName);
    }

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

        // 알 생성
        SpawnAllStones();
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

    void SpawnAllStones()
    {
        if(!IsServer) return;

        // 접속된 클라이언트 목록 확인
        var clients = NetworkManager.Singleton.ConnectedClientsList;
        if(clients.Count < 2) return;

        // P1은 호스트(0번), P2는 클라이언트(1번)에게 소유권 부여
        ulong p1Id = NetworkManager.ServerClientId;
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
        SpawnByTeam(p1Id, 0, stone1Prefab);
        SpawnByTeam(p2Id, 1, stone2Prefab);
        
        stoneSpawned = true; // 중복 방지
    }

    void SpawnByTeam(ulong ownerId, int playerIndex, GameObject prefab)
    {
        foreach (Transform spawnPoint in currentMapConfig.stoneSpawnPoints[playerIndex].spawnPoints)
        {
            var go = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        
            // 소유권을 지정하여 스폰
            go.GetComponent<NetworkObject>().SpawnWithOwnership(ownerId);
            Stone stone = go.GetComponent<Stone>();
            
            //ruleExecutor.RegisterStone(stone);
        }
    }
}