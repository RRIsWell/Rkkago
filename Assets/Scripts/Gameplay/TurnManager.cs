using UnityEngine;
using Unity.Netcode;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class TurnManager : NetworkBehaviour 
{
    public static TurnManager Instance;

    [SerializeField] private float turnTime = 10f;
    private bool isChangingTurn = false; // 턴 교체 중복 방지용
    private bool isTurnActive = false; // 팝업 뜰 땐 타이머X

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

    private List<ulong> playerClientIds = new List<ulong>();

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

        NetworkManager.Singleton.OnClientConnectedCallback 
            += OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if(clientId == NetworkManager.ServerClientId)
            return;

        playerClientIds.Add(clientId);

        // 정확히 2명 모였을 때만 게임 시작
        if(playerClientIds.Count == 2)
        {
            StartTurn(playerClientIds[0]);
        }
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

    void StartTurn(ulong clientId) 
    {
        currentTurnClientId.Value = clientId;
        remainingTime.Value = turnTime; // 턴 시작 시 시간 리셋
        isChangingTurn = false;
        isTurnActive = false;
        Debug.Log($"Turn Started for: {clientId}");

        OnTurnChanged?.Invoke(clientId);

    }

    // 턴 교체 (다음 플레이어로 턴 이동)
    void ChangeTurn()
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

        StartTurn(playerClientIds[nextIndex]);
    }

    // 클라이언트가 직접 턴 종료 (필요할지 결정해야 됨)
    /*
    [ServerRpc(RequireOwnership = false)]
    public void EndTurnServerRpc(ServerRpcParams rpcParams = default)
    {
        if(rpcParams.Receive.SenderClientId != currrentTurnClientId.Value) 
            return;

        ChangeTurn();
    }
    */

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
        isTurnActive = true;
    }

}