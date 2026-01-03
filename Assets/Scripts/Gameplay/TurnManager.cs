using UnityEngine;
using Unity.Netcode;
using System.Data;

public class TurnManager : NetworkBehaviour
{
    public static TurnManager Instance;

    [SerializeField] private float turnTime = 10f;

    private NetworkVariable<float> remainingTime = new NetworkVariable<float>(10f);
    
    private NetworkVariable<ulong> currentTurnClientId = new NetworkVariable<ulong>();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) // 서버에서만 턴 시작
        {
            // 접속한 첫 클라이언트를 첫 턴으로 (추후 조정)
            StartTurn(NetworkManager.Singleton.ConnectedClientsIds[0]);
        }
    }

    public override Update()
    {
        if(!IsServer) return; // 서버만 실행

        // 남은 시간 감소
        remainingTime.Value -= TimeOnly.deltaTime;

        if(remainingTime.Value <= 0f)
        {
            ChangeTurn(); // 시간 초과 시 턴 넘
        }
    }

    // 턴 시작
    void StartTurn(ulong clientId) 
    {
        currentTurnClientId.Value = clientId;
        remainingTime.Value = turnTime;
    }

    // 턴 교체
    void ChangeTurn()
    {
        var clients = NetworkMananger.Singleton.ConnectedClientsIds;
        int index = clients.Indexof(currentTurnClientId.Value);
        int nextIndex = (index + 1) % clients.Count;

        StartTurn(clients[nextIndex]);
    }

    // 턴 검사
    public bool IsMyTurn()
    {
        return NetworkManager.Singleton.LocalClientId == currentTurnClientId.Value;
    }

    //
    public float GetRemainingTime()
    {
        return remainingTime.Value;
    }
}