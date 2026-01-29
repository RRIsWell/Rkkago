using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Debug = UnityEngine.Debug;

public class MapManager : NetworkBehaviour
{
    // 맵
    [SerializeField] MapConfig currentMapConfig;
    [SerializeField] private MapRuleExecutor ruleExecutor;
    
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

        // 반드시 2명 모였을 때만
        if(NetworkManager.Singleton.ConnectedClientsList.Count < 2)
            return;

        Debug.Log("2 players connected → Spawn Stones");

        if (ruleExecutor == null)
        {
            Debug.LogError("[MapManager] ruleExecutor가 인스펙터에 할당되지 않았습니다.");
            return;
        }

        ruleExecutor.Init(currentMapConfig);

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
        
            var netObj = go.GetComponent<NetworkObject>();
            netObj.SpawnWithOwnership(ownerId);

            // ruleExecutor null 체크
            if(ruleExecutor == null)
            {
                Debug.LogError("[MapManager] ruleExecutor is null. MapRuleExecutor 못 찾음");
                return;
            }

            // StoneController에 ruleExecutor 주입
            var controller = go.GetComponent<StoneController>();
            if(controller == null)
            {
                Debug.LogError("[MapManager] Spawned stone has no StoneController");
            }
            else
            {
                controller.SetRuleExecutor(ruleExecutor);
            }

            // remain 초기화(RegisterStone)
            var stone = go.GetComponent<Stone>();
            if(stone == null)
            {
                Debug.LogError("[MapManager] Spawned stone has no Stone");
            }
            else
            {
                ruleExecutor.RegisterStone(stone);
            }
        }
    }
}