using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public enum GameEndReason
{
    AllStonesOut,
    TieBreaker
}

public class MapRuleExecutor : NetworkBehaviour
{
    private MapConfig config;
    public MapConfig Config => config;
    private bool gameEnded = false; // 승패 판정 중복 방지
    public bool GameEnded => gameEnded;

    // clientId -> 남은 돌
    private Dictionary<ulong, int> remain = new Dictionary<ulong, int>();

    // 현재 맵에 스폰된 모든 알 리스트
    public readonly List<StoneController> stones = new List<StoneController>();
    
    // 살아있는 돌 목록 (기준으로 컬링 맵 판정)
    public readonly List<Stone> aliveStones = new();
    

    public void Init(MapConfig mapConfig)
    {
        config = mapConfig;

        // 씬 재시작/재매치 대비 초기화
        gameEnded = false;
        remain.Clear();
        stones.Clear();
        aliveStones.Clear();
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

        if (!aliveStones.Contains(stone))
        {
            aliveStones.Add(stone);
            stones.Add(stone.GetComponent<StoneController>());
        }
            
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
        
        // 돌 하나라도 죽으면 즉시 새 스킬 분배
        TurnManager.Instance?.GiveRandomSkillsPublic();

        // 서버에서 삭제
        StartCoroutine(DelayedDespawn(netObj)); 
        //netObj.Despawn();

        // 공통 패배 조건: 한쪽 돌이 0개 되면 종료
        if (remain.ContainsKey(owner) && remain[owner] <= 0)
        {
            ulong loserId = owner;
            ulong winnerId = GetOpponentId(loserId);
            EndGame(winnerId, loserId, GameEndReason.AllStonesOut);
        }
    }
    private IEnumerator DelayedDespawn(NetworkObject netObj)
    {
        // RPC 처리 시간 확보
        yield return new WaitForSeconds(0.1f);
        
        netObj.Despawn();
    }

    /// <summary>
    /// 공통 게임 종료 함수 
    /// Map3 타이브레이크도 이걸 호출하게 만들 거임
    /// </summary>
    public void EndGame(ulong winnerId, ulong loserId, GameEndReason reason)
    {
        if (!IsServer) return;
        if (gameEnded) return;

        gameEnded = true;

        Debug.Log($"[Rule] EndGame reason={reason}, winner={winnerId}, loser={loserId}");

        NotifyGameResultClientRpc(winnerId, loserId, (int)reason);
    }

    private ulong GetOpponentId(ulong oneSide)
    {
        if (TurnManager.Instance == null) return oneSide;

        ulong p1 = TurnManager.Instance.Player1ClientId;
        ulong p2 = TurnManager.Instance.Player2ClientId;

        if (oneSide == p1) return p2;
        if (oneSide == p2) return p1;

        // 혹시 좌석이 아직 안 잡혔을 때를 대비한 fallback
        foreach (var c in NetworkManager.Singleton.ConnectedClientsList)
            if (c.ClientId != oneSide) return c.ClientId;

        return oneSide;
    }

    [ClientRpc]
    private void NotifyGameResultClientRpc(ulong winnerId, ulong loserId, int reasonInt)
    {
        var ui = FindObjectOfType<TurnUI>();
        if (ui != null)
            ui.ShowGameResult(winnerId, loserId, (GameEndReason)reasonInt);
        else
            Debug.LogError("[Rule] TurnUI를 못 찾음 (씬에 TurnUI 존재/활성 확인)");
    }

    public void CheckCullingTieBreaker(int currentTurnPairs)
{
    if (gameEnded) return;
    if (!IsServer) return;

    if (config == null) return;
    if (config.ruleType != MapRuleType.Culling) return;
    if (currentTurnPairs < config.maxTurnPairs) return;

    // 좌석 기반으로 비교해야 함
    ulong p1 = TurnManager.Instance != null ? TurnManager.Instance.Player1ClientId : ulong.MaxValue;
    ulong p2 = TurnManager.Instance != null ? TurnManager.Instance.Player2ClientId : ulong.MaxValue;
    if (p1 == ulong.MaxValue || p2 == ulong.MaxValue) return;

    float bestP1 = float.MaxValue;
    float bestP2 = float.MaxValue;

    foreach (var s in aliveStones)
    {
        if (s == null) continue;

        var no = s.GetComponent<NetworkObject>();
        if (no == null || !no.IsSpawned) continue;

        float dist = Vector2.Distance(s.transform.position, config.center);

        if (no.OwnerClientId == p1) bestP1 = Mathf.Min(bestP1, dist);
        else if (no.OwnerClientId == p2) bestP2 = Mathf.Min(bestP2, dist);
    }

    // 둘 중 한쪽이라도 돌이 없으면 여기서 판정하면 안 됨 (이미 AllStonesOut이 처리해야 함)
    if (bestP1 == float.MaxValue || bestP2 == float.MaxValue) return;

    if (Mathf.Approximately(bestP1, bestP2))
    {
        return;
    }

    // 더 가까운 쪽이 승자
    ulong winnerId = (bestP1 < bestP2) ? p1 : p2;
    ulong loserId  = (winnerId == p1) ? p2 : p1;

    EndGame(winnerId, loserId, GameEndReason.TieBreaker);
}
}