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

    // 장애물 스폰
    [SerializeField] private ObstacleSpawner obstacleSpawner;
    
    // 알 스폰
    [SerializeField] private GameObject stone1Prefab; 
    [SerializeField] private GameObject stone2Prefab; 
    private bool stoneSpawned = false; // 알 중복 스폰 방지

    private void Start()
    {
        SoundManager.Instance?.PlayBGM(currentMapConfig.bgmName);
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

        TurnManager.Instance.AddClientId(clientId);
        
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
        TurnManager.Instance.SetRuleExecutor(ruleExecutor);

        // 장애물 스폰
        if(currentMapConfig != null && currentMapConfig.useObstacle)
        {
            if(obstacleSpawner == null)
                obstacleSpawner = FindObjectOfType<ObstacleSpawner>();

            obstacleSpawner?.Init(currentMapConfig);
        }

        // 좌석 먼저 결정
        TurnManager.Instance.DecideSeatsIfNeeded();

        // 좌석 기준으로 알 스폰하고 팀 세팅
        SpawnAllStonesBySeats();

        // 매치 시작 (첫 턴 시작)
        TurnManager.Instance.StartMatch();

    }

    // 지금은 호출 안 되고 있긴 한데 일단 남겨둠
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

    // 좌석 기준 스폰
    void SpawnAllStonesBySeats()
    {
        if(!IsServer) return;

        // 접속된 클라이언트 목록 확인
        var clients = NetworkManager.Singleton.ConnectedClientsList;
        if(clients.Count < 2) return;

        // P1은 호스트(0번), P2는 클라이언트(1번)에게 소유권 부여
        ulong leftId = TurnManager.Instance.Player1ClientId;   // P1 = 왼쪽
        ulong rightId = TurnManager.Instance.Player2ClientId;  // P2 = 오른쪽

        if (leftId == ulong.MaxValue || rightId == ulong.MaxValue)
        {
            Debug.LogError("[MapManager] Seats not decided yet.");
            return;
        }

        // 각각의 스폰 포인트 그룹에서 소환 : 1=왼쪽(하늘) , 2=오른쪽(분)
        SpawnByTeam(leftId, 0, 1, stone1Prefab);
        SpawnByTeam(rightId, 1, 2, stone2Prefab);
        
        stoneSpawned = true; // 중복 방지
    }

    // teamId 파라미터 추가
    void SpawnByTeam(ulong ownerId, int playerIndex, int teamId, GameObject prefab)
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
            if(stone != null)
            {
                stone.SetRuleExecutor(ruleExecutor); 
                
                // 팀 세팅 (모든 클라에 색 동기화)
                stone.SetTeam(teamId);
                
                ruleExecutor.RegisterStone(stone);
            }
        }
    }
}