using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Diagnostics;

public class MapRuleExecutor : NetworkBehaviour
{
    private MapConfig config;

    // clientId -> 남은 돌
    private Dictionary<ulong, int> remain = new Dictionary<ulong, int>();
    public void Init(MapConfig mapConfig)
    {
        config = mapConfig;
    }

    // 서버에서만 실행
    public void RegisterStone(Stone stone)
    {
        ulong owner = stone.GetComponent<NetworkObject>().OwnerClientId;

        if(!remain.ContainsKey(owner))
        {
            remain[owner] = config.stonesPerPlayer;
        }
    }

    // 경계 밖으로 나갔을 때 호출
    public void OnStoneOut(Stone stone)
    {
        if(!IsServer) return;
        
        var netObj = stone.GetComponent<NetworkObject>();
        ulong owner = netObj.OwnerClientId;

        remain[owner]--;
        Destroy(stone.gameObject); // 서버에서 삭제

        if(remain[owner] <= 0)
            OnPlayerLose(owner);
    }

    public void OnPlayerLose(ulong loser)
    {
        Debug.Log($"{loser} LOSE");

        //TODO: TurnManager에 전달해서 게임 종료 처리
    }
}