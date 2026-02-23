using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class TurnManager : NetworkBehaviour 
{
    public static TurnManager Instance;

    [SerializeField] private float turnTime = 10f;
    [SerializeField] private float waitTillSKill = 1f;
    [SerializeField] private float waitTillTurnEnd = 1f;
    
    
    private bool isChangingTurn = false; // 턴 교체 중복 방지용
    private bool isTurnActive = false; // 팝업 뜰 땐 타이머X
    private bool initialSkillGiven = false; // 처음에 스킬 부여
    private bool gameStarted = false; // 게임 시작 1회만 선공 결정
    private bool _isStarted = false; // 게임 시작 1회만 state가 바뀔 때 스킬창 실행

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

    // 접속한 플레이어 정보 (왼쪽이 P1, 오른쪽이 P2 고정)
    private List<ulong> playerClientIds = new List<ulong>();
    public List<ulong> PlayerClientIds => playerClientIds;

    // P1(왼쪽), P2(오른쪽) 배정 네트워크 변수
    private NetworkVariable<ulong> player1ClientId = 
        new NetworkVariable<ulong>(
            ulong.MaxValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private NetworkVariable<ulong> player2ClientId = 
        new NetworkVariable<ulong>(
            ulong.MaxValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public ulong Player1ClientId => player1ClientId.Value; // 왼쪽 파랑
    public ulong Player2ClientId => player2ClientId.Value; // 오른쪽 분홍
    
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
    public int GetTurnPairs() => turnStep / 2;

    /// <summary>
    /// 좌석 고정 + 동전 던지기 결과 알림 (TurnUI에서 애니메이션 트리거)
    /// (isHeads, p1LeftId(host), p2RightId(guest))
    /// </summary>
    public event Action<bool, ulong, ulong> OnSeatsDecided;

    // =========================
    // 턴이 1번 진행되었다 (서버 전용 이벤트)
    // =========================
    public static event System.Action OnServerTurnAdvanced;

    /// <summary>
    /// 턴 끝난 후 스킬 발동될 때 부르는 이벤트
    /// </summary>
    public event System.Action OnTurnEndedSkill;
    
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
        /*NetworkManager.Singleton.OnClientConnectedCallback 
            += OnClientConnected; */

        // 이미 2명인 상태로 스폰되는 경우 시작 시도
        /*TryStartGame();*/
    }

    /*private void OnClientConnected(ulong clientId)
    {
        if(!playerClientIds.Contains(clientId)) // 중복 방지
            playerClientIds.Add(clientId);

        // 2명 모이면 시작 시도
        //TryStartGame();
    }*/

    public void AddClientId(ulong clientId)
    {
        if(!playerClientIds.Contains(clientId)) // 중복 방지
            playerClientIds.Add(clientId);
    }

    /// <summary>
    /// 좌석과 색 고정
    /// 동전 던지기는 여기서 X
    /// </summary>
    public void DecideSeatsIfNeeded()
    {
        if(!IsServer) return;
        if(gameStarted) return;

        var connected = NetworkManager.Singleton.ConnectedClientsIds;
        if (connected.Count < 2) return;

        gameStarted = true;

        ulong hostId = NetworkManager.ServerClientId;

        // 게스트 찾기
        ulong guestId = ulong.MaxValue;
        foreach (var id in connected)
        {
            if (id != hostId) { guestId = id; break; }
        }
        if (guestId == ulong.MaxValue) return;

        // host=P1(왼쪽), client=P1(오른쪽) 네트워크로 공유
        player1ClientId.Value = hostId;
        player2ClientId.Value = guestId;

        // 앞으로 모든 로직이 [P1, P2] 순서를 쓰게 리스트 정렬
        playerClientIds.Clear();
        playerClientIds.Add(hostId);
        playerClientIds.Add(guestId);

        gameStarted = true;
    }

    /// <summary>
    /// 단판제 시작
    /// - 좌석은 이미 고정됨
    /// - 동전으로 선공만 결정
    /// </summary>
    public void StartMatch()
    {
        if(!IsServer) return;
        if(Player1ClientId == ulong.MaxValue || Player2ClientId == ulong.MaxValue) return;
        
        _isStarted = true;
        
        // 단판제
        ResetTurnCounter();

        // 선공만 동전으로 결정 (좌석은 고정)
        bool isHeads = Random.Range(0, 2) == 0;
        ulong starter = isHeads ? Player1ClientId : Player2ClientId;

        // UI에 좌석 고정 + 동전 결과를 한 번만 알림
        SeatsDecidedClientRpc(isHeads, Player1ClientId, Player2ClientId);


        // 첫 턴 시작
        StartTurn(starter);
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
    public event System.Action OnTurnChangedNoArgs;

    [ClientRpc]
    private void InvokeTurnChangedClientRpc(ulong clientId)
    {
        OnTurnChanged?.Invoke(clientId);
        OnTurnChangedNoArgs?.Invoke();
    }
    
    [ClientRpc]
    private void InvokeTurnEndedSkillClientRpc()
    {
        OnTurnEndedSkill?.Invoke();
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
        if(Player2ClientId == ulong.MaxValue) return;
        
        var stones = ruleExecutor?.stones; 
        var aliveStones = ruleExecutor?.aliveStones;

        List<StoneController> p1Stones = new();
        List<StoneController> p2Stones = new();

        ulong p1Id = Player1ClientId;
        ulong p2Id = Player2ClientId;

        // 기존 스킬 비활성화
        foreach (var s in stones)
        {
            if(s != null)
                s.DeactivateSkillClientRpc();
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

        if (!_isStarted)
        {
            // 스킬 팝업창 생성
            SkillInfoController.Instance.ShowSkillInfoClientRpc();
        }
        _isStarted = false;
        
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
        bool turnOwnerStillConnected = false;
        foreach (var id in clients)
        {
            if (id == currentTurnClientId.Value)
            {
                turnOwnerStillConnected = true;
                break;
            }
        }

        if (!turnOwnerStillConnected)
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

        // 턴 1회 진행 이벤트 발행 (서버만)
        OnServerTurnAdvanced?.Invoke();
        
        ruleExecutor?.CheckCullingTieBreaker(TurnPairs);

        // 더 이상 StartTurn 호출 금지
        if (ruleExecutor != null && ruleExecutor.GameEnded)
        {
            isTurnActive = false;
            isChangingTurn = false;
            return;
        }

        // 턴 시작
        StartTurn(playerClientIds[nextIndex]);
    }

    // 한 번 날리고 나면 10초 안 끝나도 상대 턴
    [ServerRpc(RequireOwnership = false)]
    public void EndTurnServerRpc(ServerRpcParams rpcParams = default)
    {
        if(rpcParams.Receive.SenderClientId != currentTurnClientId.Value) 
            return;
        
        // 2. 이미 턴 교체 중이면 즉시 차단 (매우 중요)
        if(isChangingTurn) return; 

        isChangingTurn = true; // 코루틴 시작 전 즉시 true 설정
        
        StartCoroutine(WaitTillTurnEnd());
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

    private IEnumerator WaitTillTurnEnd()
    {
        yield return new WaitForSeconds(waitTillSKill);
        InvokeTurnEndedSkillClientRpc();
        yield return new WaitForSeconds(waitTillTurnEnd);
        isChangingTurn = true;
        ChangeTurn();
    }
}