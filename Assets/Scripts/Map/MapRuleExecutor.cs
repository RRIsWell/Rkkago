using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Collections;

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

        // 돌 하나라도 죽으면 즉시 새 스킬 분배
        TurnManager.Instance?.GiveRandomSkillsPublic();

        netObj.Despawn(); // 서버에서 삭제

        if(remain[owner] <= 0)
            OnPlayerLose(owner);
    }

    public void OnPlayerLose(ulong loser)
    {
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
    }
}