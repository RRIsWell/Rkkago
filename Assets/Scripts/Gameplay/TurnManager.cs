using UnityEngine;
using Unity.Netcode;
using System.Data;
using System.Linq;

public class TurnManager : NetworkBehaviour 
{
    public static TurnManager Instance;

    [SerializeField] private float turnTime = 10f;
    private bool isChangingTurn = false; // 턴 교체 중복 방지용

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

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if(!IsServer) return; // 서버에서만 실행

        // 클라이언트 입장 시 첫번째 플레이어부터 턴 시작
        var clients = NetworkManager.Singleton.ConnectedClientsIds;
        
        if(clients.Count >= 1) 
            StartTurn(clients[0]);
    }

    public void Update()
    {
        if(!IsSpawned) return;
        if(!IsServer)
        {
            Debug.Log("서버 아님");
            return;
        }

        Debug.Log("정상");

        if (isChangingTurn) return;

        // 남은 시간 감소
        remainingTime.Value -= Time.deltaTime;

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
        Debug.Log($"Turn Started for: {clientId}");

        OnTurnChanged?.Invoke(clientId);

    }

    // 턴 교체 (다음 플레이어로 턴 이동)
    void ChangeTurn()
    {
        var clients = NetworkManager.Singleton.ConnectedClientsIds;
        if(clients.Count == 0) return;

        // 클라이언트의 탈주 처리
        if(!clients.Contains(currentTurnClientId.Value))
        {
            StartTurn(clients[0]);
            return;
        }

        int index = clients.ToList().IndexOf(currentTurnClientId.Value);
        int nextIndex = (index + 1) % clients.Count;

        StartTurn(clients[nextIndex]);
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
}