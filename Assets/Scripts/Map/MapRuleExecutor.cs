using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class MapRuleExecutor : NetworkBehaviour
{
    private MapConfig config;
    public MapConfig Config => config;
    private bool gameEnded = false; // 승패 판정 중복 방지

    // clientId -> 남은 돌
    private Dictionary<ulong, int> remain = new Dictionary<ulong, int>();

    // 살아있는 돌 목록 (기준으로 컬링 맵 판정)
    public readonly List<Stone> aliveStones = new();

    public void Init(MapConfig mapConfig)
    {
        config = mapConfig;
    }

    // 서버에서만 실행
    public void RegisterStone(Stone stone)
    {
        if(!IsServer) return;
        
        ulong owner = stone.GetComponent<NetworkObject>().OwnerClientId;

        if(!remain.ContainsKey(owner))
        {
            remain[owner] = config.stonesPerPlayer;
        }

        if(!aliveStones.Contains(stone))
            aliveStones.Add(stone);
    }

    // 경계 밖으로 나갔을 때 호출
    public void OnStoneOut(Stone stone)
    {
        if(!IsServer) return;

        // RegisterStones에서 추가하고 OnStoneOut에서 뺌
        aliveStones.Remove(stone);
        
        var netObj = stone.GetComponent<NetworkObject>();
        ulong owner = netObj.OwnerClientId;

        remain[owner]--;

        netObj.Despawn(); // 서버에서 삭제

        if(remain[owner] <= 0)
            OnPlayerLose(owner);
    }

    
    /// <summary>
    /// Map3 전용 : 턴 쌍 기준 타이브레이크하고 승패 판정
    /// </summary>
    /// <param name="loserId"></param>
    public void CheckCullingTieBreaker(int currentTurnPairs)
    {
        if(gameEnded) return;
        if(!IsServer) return;

        if(config == null) return; // null 오류 방지
        if(config.ruleType != MapRuleType.Culling) return;
        if(currentTurnPairs < config.maxTurnPairs) return;

        ulong hostId = NetworkManager.ServerClientId;
        ulong otherId = GetOtherClientId(hostId);

        float bestHost = float.MaxValue;
        float bestOther = float.MaxValue;

        foreach(var s in aliveStones)
        {
            if(s == null) continue;

            var no = s.GetComponent<NetworkObject>();
            if(no == null || !no.IsSpawned) continue;

            float dist = Vector2.Distance(
                s.transform.position,
                config.center
            );

            if(no.OwnerClientId == hostId)
                bestHost = Mathf.Min(bestHost, dist);
            else if(no.OwnerClientId == otherId)
                bestOther = Mathf.Min(bestOther, dist);
        }

        if(bestHost == float.MaxValue || bestOther == float.MaxValue)
            return;
        
        if(Mathf.Approximately(bestHost, bestOther))
        {
            Debug.Log("[Map3] TieBreaker Draw");
            return;
        }

        // 더 먼 쪽이 패배
        ulong loser = (bestHost < bestOther) ? otherId : hostId;
        OnPlayerLose(loser);
    }

    private ulong GetOtherClientId(ulong hostId)
    {
        foreach(var c in NetworkManager.Singleton.ConnectedClientsList)
            if(c.ClientId != hostId) return c.ClientId;
        
        return hostId;
    }


    // 패배
    public void OnPlayerLose(ulong loser)
    {
        if(gameEnded) return;
        gameEnded = true;
        
        Debug.Log($"{loser} LOSE");

        //TODO: TurnManager에 전달해서 게임 종료 처리
        NotifyGameResultClientRpc(loser);
    }

    [ClientRpc]
    private void NotifyGameResultClientRpc(ulong loserId)
    {
        TurnUI ui = FindObjectOfType<TurnUI>();
        if(ui != null)
        {
            ui.ShowGameResult(loserId);
        }
        else
        {
            Debug.LogError("[Rule] TurnUI를 못 찾음 (씬에 TurnUI 존재/활성 확인)");
        }
    }
}