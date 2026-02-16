using System;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class TurnManager : NetworkBehaviour 
{
    public static TurnManager Instance;

    [SerializeField] private float turnTime = 10f;
    private bool isChangingTurn = false; // 턴 교체 중복 방지용
    private bool isTurnActive = false; // 팝업 뜰 땐 타이머X
    private bool initialSkillGiven = false; // 처음에 스킬 부여
    private bool gameStarted = false; // 게임 시작 1회만 선공 결정

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

    // 접속한 플레이어 정보 (왼쪽이 P1, 오른쪽이 P2)
    private List<ulong> playerClientIds = new List<ulong>();
    public List<ulong> PlayerClientIds => playerClientIds;

    // P1(왼쪽), P2(오른쪽) 배정 네트워크 변수
    private NetworkVariable<ulong> player1ClientId = 
        new NetworkVariable<ulong>(
            ulong.MaxValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public ulong Player1ClientId => player1ClientId.Value;
    public ulong Player2ClientId
    {
        get
        {
            if(playerClientIds.Count < 2) return ulong.MaxValue;
            if(player1ClientId.Value == ulong.MaxValue) return ulong.MaxValue;

            // 1번이 P2
            return playerClientIds[1];
        }
    }
    
    // =========================
    // Map3(컬링)용 턴쌍 카운터
    // =========================
    private int turnStep = 0;
    private int TurnPairs => turnStep / 2;

    // =========================
    // UI용 턴 쌍 네트워크 동기화
    // =========================
    private NetworkVariable<int> turnNumber = 
        new NetworkVariable<int>(
            1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public NetworkVariable<int> TurnNumber => turnNumber;

    private int CalcTurnNumber() => (turnStep / 2) + 1;

    /// <summary>
    /// 동전 던지기 결과 알림 (TurnUI에서 애니메이션 트리거)
    /// </summary>
    public event Action<bool, ulong, ulong> OnSeatsDecided;
    // (isHeads, p1LeftId, p2RightId)
    
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
        if(!IsServer) return; //버에서만 실행

        // 이미 연결된 클라이언트를 먼저 채워넣음
        playerClientIds.Clear();
        foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
        {
            playerClientIds.Add(id);
        }

        // 새로 들어오는 클라이언트 받음
        NetworkManager.Singleton.OnClientConnectedCallback 
            += OnClientConnected;

        // 이미 2명인 상태로 스폰되는 경우 시작 시도
        TryStartGame();

        Debug.Log($"[TM] OnNetworkSpawn, players={string.Join(",", playerClientIds)}");
    }

    private void OnClientConnected(ulong clientId)
    {
        if(!playerClientIds.Contains(clientId)) // 중복 방지
            playerClientIds.Add(clientId);

        // 2명 모이면 시작 시도
        //TryStartGame();
    }

    /// <summary>
    /// 2명 모이면 동전 던지기로 P1/P2 배정 + 첫 턴 시작
    /// </summary>
    public void TryStartGame()
    {
        if(!IsServer) return;
        if(gameStarted) return;
        if(playerClientIds.Count < 2) return;

        gameStarted = true;

        // 서버만 동전 던지기 (true=앞면, false=뒷면)
        bool isHeads = Random.Range(0, 2) == 0;

        // 앞면이면 지금 리스트[0]이 P1, 뒷면이면 리스트[1]이 P1
        ulong p1 = isHeads ? playerClientIds[0] : playerClientIds[1];
        ulong p2 = isHeads ? playerClientIds[1] : playerClientIds[0];

        // P1(왼쪽) 네트워크로 공유
        player1ClientId.Value = p1;

        // 앞으로 모든 로직이 [P1, P2] 순서를 쓰게 리스트 정렬
        playerClientIds.Clear();
        playerClientIds.Add(p1);
        playerClientIds.Add(p2);

        // UI들에게 동전 결과 알림 (애니메이션 트리거)
        SeatsDecidedClientRpc(isHeads, p1, p2);

        // 턴 카운터 초기화 후 P1이 선공
        ResetTurnCounter();
        StartTurn(p1);

        Debug.Log($"[TM] Seats decided. Heads={isHeads}, P1(left)={p1}, P2(right)={p2}");
    }

    [ClientRpc]
    private void SeatsDecidedClientRpc(bool isHeads, ulong p1Id, ulong p2Id)
    {
        OnSeatsDecided?.Invoke(isHeads, p1Id, p2Id);
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

    [ClientRpc]
    private void InvokeTurnChangedClientRpc(ulong clientId)
    {
        OnTurnChanged?.Invoke(clientId);
    }

    public void StartTurn(ulong clientId) 
    {
        currentTurnClientId.Value = ulong.MaxValue;
        currentTurnClientId.Value = clientId;
        
        remainingTime.Value = turnTime; // 턴 시작 시 시간 리셋
        isChangingTurn = false;
        
        isTurnActive = false;
        Debug.Log($"Turn Started for: {clientId}");

        // UI용 턴 쌍 갱신
        if(IsServer)
            turnNumber.Value = CalcTurnNumber();

        // 턴 교체 이벤트 발생
        InvokeTurnChangedClientRpc(clientId);

        // 최초 게임 시작 시 1회 랜덤 스킬 부여
        if (IsServer && !initialSkillGiven && playerClientIds.Count == 2)
        {
            initialSkillGiven = true;
            GiveRandomSkillsToBothPlayers();
        }
    }

    // 랜덤 스킬 부여용
    private void GiveRandomSkillsToBothPlayers()
    {
        if(playerClientIds.Count < 2) return;
        if(Player1ClientId == ulong.MaxValue) return;
        
        var stones = ruleExecutor?.stones; 
        var aliveStones = ruleExecutor?.aliveStones;

        List<StoneController> p1Stones = new();
        List<StoneController> p2Stones = new();

        ulong p1Id = Player1ClientId;
        ulong p2Id = Player2ClientId;

        // 기존 스킬 비활성화
        foreach (var s in stones)
        {
            s.DeActivateSkillClientRpc();
        }

        // 살아남은 알 리스트 업데이트
        foreach (var s in aliveStones)
        {
            var no = s.GetComponent<NetworkObject>();
            var sc = s.GetComponent<StoneController>();
            
            if (no == null) continue;

            if (no.OwnerClientId == p1Id)
                p1Stones.Add(sc);
            else if (no.OwnerClientId == p2Id)
                p2Stones.Add(sc);
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
        // 몇 개의 스킬에 적용할 건지
        List<int> p1Index = PickRandomIndices(p1Stones.Count, p1Skill.Item2.Data.applyStoneCount);
        List<int> p2Index = PickRandomIndices(p2Stones.Count, p2Skill.Item2.Data.applyStoneCount);
        
        foreach (var i in p1Index)
            p1Stones[i].ApplySkillClientRpc(p1Skill.Item1);
        foreach (var i in p2Index)
            p2Stones[i].ApplySkillClientRpc(p2Skill.Item1);
        
        // 스킬 팝업창 생성
        SkillInfoController.Instance.ShowSkillInfoClientRpc();
        
        Debug.Log($"[Skill] 플레이어1: {p1Skill.Item2.SkillName} 플레이어2: {p2Skill.Item2.SkillName}");
    }
    
    List<int> PickRandomIndices(int totalCount, int pickCount)
    {
        // pickCount가 totalCount보다 크면 오류 방지
        pickCount = Mathf.Clamp(pickCount, 0, totalCount);
        
        List<int> indices = new List<int>();
        for (int i = 0; i < totalCount; i++)
            indices.Add(i);

        // Fisher–Yates 셔플
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            (indices[i], indices[r]) = (indices[r], indices[i]);
        }

        // 앞에서 n개 반환
        return indices.GetRange(0, pickCount);
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
        turnNumber.Value = 1; // 턴 수도 1로 맞춤 
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