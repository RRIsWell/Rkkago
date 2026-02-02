using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

public class TurnManager : NetworkBehaviour 
{
    public static TurnManager Instance;

    [SerializeField] private float turnTime = 10f;
    private bool isChangingTurn = false; // 턴 교체 중복 방지용
    private bool isTurnActive = false; // 팝업 뜰 땐 타이머X
    private bool initialSkillGiven = false;

    // FindObjectOfType 제거하고 주입 받기
    private MapRuleExecutor ruleExecutor;
    public void SetRuleExecutor(MapRuleExecutor exec) => ruleExecutor = exec;


    private NetworkVariable<float> remainingTime = 
        new NetworkVariable<float>(
            10f, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server // NetworkVariable의 권한 명시
        );
    
    private NetworkVariable<ulong> currentTurnClientId = 
        new NetworkVariable<ulong>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public NetworkVariable<ulong> CurrentTurnClientId => currentTurnClientId;

    public NetworkVariable<ulong> GetCurrentTurnClientId()
    {
        return currentTurnClientId;
    }

    // 접속한 플레이어 정보
    private List<ulong> playerClientIds = new List<ulong>();
    public List<ulong> PlayerClientIds => playerClientIds;

    
    // =========================
    // [ADD] Map3(컬링)용 턴쌍 카운터
    // =========================
    private int turnStep = 0;
    private int TurnPairs => turnStep / 2;
    
    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // 서버는 플레이어로 간주하지 않도록 로직 구현
    public override void OnNetworkSpawn()
    {
        if(!IsServer) return; // 서버에서만 실행

        // 이미 연결된 클라이언트를 먼저 채워넣음
        playerClientIds.Clear();
        foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
        {
            playerClientIds.Add(id);
        }

        // 새로 들어오는 클라이언트 받음
        NetworkManager.Singleton.OnClientConnectedCallback 
            += OnClientConnected;

        // 이미 2명 모여있으면 바로 시작
        /*if (playerClientIds.Count == 2)
        {
            StartTurn(playerClientIds[0]);
        }*/

        Debug.Log($"[TM] OnNetworkSpawn, players={string.Join(",", playerClientIds)}");
    }

    private void OnClientConnected(ulong clientId)
    {
        if(!playerClientIds.Contains(clientId)) // 중복 방지
            playerClientIds.Add(clientId);

        // 정확히 2명 모였을 때만 게임 시작
        /*if(playerClientIds.Count == 2)
        {
            StartTurn(playerClientIds[0]);
        }*/
    }

    public event System.Action<float> OnRemainingTimeChanged;

    public void Update()
    {
        if(!IsSpawned) return;
        if(!IsServer)
        {
            // Debug.Log("서버 아님");
            return;
        }

        // Debug.Log("정상");

        if (isChangingTurn) return;
        if(!isTurnActive) return;

        // 남은 시간 감소
        remainingTime.Value -= Time.deltaTime;
        OnRemainingTimeChanged?.Invoke(remainingTime.Value);

        if(remainingTime.Value <= 0f)
        {
            isChangingTurn = true;
            ChangeTurn(); // 시간 초과 시 턴 넘김
        }
    }
    
    // 턴 시작
    public event System.Action<ulong> OnTurnChanged;

    public void StartTurn(ulong clientId) 
    {
        currentTurnClientId.Value = ulong.MaxValue;
        currentTurnClientId.Value = clientId;
        
        remainingTime.Value = turnTime; // 턴 시작 시 시간 리셋
        isChangingTurn = false;
        
        isTurnActive = false;
        Debug.Log($"Turn Started for: {clientId}");

        OnTurnChanged?.Invoke(clientId);

        // 최초 게임 시작 시 1회 랜덤 스킬 부여
        if (IsServer && !initialSkillGiven && playerClientIds.Count == 2)
        {
            Debug.Log("[TM] Starting Skill");
            initialSkillGiven = true;
            GiveRandomSkillsToBothPlayers();
        }
    }

    // 랜덤 스킬 부여용
    private void GiveRandomSkillsToBothPlayers()
    {
        var stones = FindObjectsOfType<StoneController>();

        List<StoneController> p1Stones = new();
        List<StoneController> p2Stones = new();

        foreach (var s in stones)
        {
            var no = s.GetComponent<NetworkObject>();
            if (no == null) continue;

            if (no.OwnerClientId == 0)
                p1Stones.Add(s);
            else if (no.OwnerClientId == 1)
                p2Stones.Add(s);
        }

        if (p1Stones.Count == 0 || p2Stones.Count == 0)
        {
            Debug.LogWarning("[TM] Stones not ready yet");
            return;
        }
        
        // 각 플레이어 랜덤 스킬 선택
        var p1Skill = p1Stones[0].SkillContainer.GetRandomSkill();
        var p2Skill = p2Stones[0].SkillContainer.GetRandomSkill();

        // 스킬 적용
        foreach (var stone in p1Stones)
            stone.ApplySkillClientRpc(p1Skill.Item1);
        foreach (var stone in p2Stones)
            stone.ApplySkillClientRpc(p2Skill.Item1);
        
        SkillInfoController.Instance.ShowSkillInfoClientRpc();
        
        Debug.Log($"[Skill] 플레이어1: {p1Skill.Item2.SkillName} 플레이어2: {p2Skill.Item2.SkillName}");
    }

    public void GiveRandomSkillsPublic()
    {
        if (!IsServer) return;
        GiveRandomSkillsToBothPlayers();
    }

    
    // =========================
    // 게임 시작 시 턴 카운터 리셋 (Map3 = 15쌍)
    // =========================
    public void ResetTurnCounter()
    {
        if(!IsServer) return;
        turnStep = 0;
    }


    // 턴 교체 (다음 플레이어로 턴 이동)
    private void ChangeTurn()
    {
        var clients = NetworkManager.Singleton.ConnectedClientsIds;
        if(clients.Count < 2) return;

        // 클라이언트의 탈주 처리
        if(!clients.Contains(currentTurnClientId.Value))
        {
            StartTurn(clients[0]);
            return;
        }

        int index = playerClientIds
            .IndexOf(currentTurnClientId.Value);
        int nextIndex = (index + 1) % playerClientIds.Count;

        
        // =========================
        // 턴 쌍 카운트 증가 + Map3의 타이브레이크 체크
        // =========================
        turnStep++;
        
        ruleExecutor?.CheckCullingTieBreaker(TurnPairs);

        // 턴 시작
        StartTurn(playerClientIds[nextIndex]);
    }

    // 한 번 날리고 나면 10초 안 끝나도 상대 턴
    [ServerRpc(RequireOwnership = false)]
    public void EndTurnServerRpc(ServerRpcParams rpcParams = default)
    {
        if(rpcParams.Receive.SenderClientId != currentTurnClientId.Value) 
            return;

        // 즉시 턴 넘기기
        isChangingTurn = true;
        ChangeTurn();
    }

    // 턴 검사
    public bool IsMyTurn()
    {
        // 내 ClientId와 서버가 정한 턴 ClientID 비교
        return NetworkManager.Singleton.LocalClientId == 
                currentTurnClientId.Value;
    }

    // UI 타이머 표시용
    public float GetRemainingTime()
    {
        return remainingTime.Value;
    }

    // 팝업 리셋용
    [ServerRpc(RequireOwnership = false)]
    public void NotifyTurnPopupFinishedServerRpc()
    {
        Debug.Log("[TM] Popup finished -> Turn Active");
        isTurnActive = true;
    }
}