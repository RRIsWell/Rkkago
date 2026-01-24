using System.Collections;
using System.Dynamic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : NetworkBehaviour
{
    private NetworkVariable<GameState> netState =  
        new(GameState.Waiting);

    public override void OnNetworkSpawn()
    {
        netState.OnValueChanged += OnStateChanged;

        // 클라이언트가 늦게 접속해도 현재 상태를 GameManager에 동기화
        GameManager.Instance.SetGameState(netState.Value);

        if(IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback 
                += OnClientConnected;
        }
    }

    void OnClientConnected(ulong clientId)
    {
        TryStartMatch();
    }

    public override void OnNetworkDespawn()
    {
        netState.OnValueChanged -= OnStateChanged;
        
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback 
                -= OnClientConnected;
        }
    }

    void TryStartMatch()
    {
        // 중복 호출 방지
        if(netState.Value != GameState.Waiting)
            return;
        
        if(NetworkManager.Singleton.ConnectedClients.Count == 2)
        {
            netState.Value = GameState.MatchIntro;
        }
    }

    void OnStateChanged(GameState oldState, GameState newState)
    {
        
        // GameManager 업데이트
        GameManager.Instance.SetGameState(newState);

        // UI 처리
        if(newState == GameState.MatchIntro && IsServer)
        {
            CancelInvoke(nameof(TransitionToPlaying));
            Invoke(nameof(TransitionToPlaying), 3f);
        }
    }

    // 서버에서 3초 뒤 상태 변경
    void TransitionToPlaying()
    {
        netState.Value = GameState.Playing;
    }
}