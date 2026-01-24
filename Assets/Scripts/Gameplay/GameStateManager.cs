using System.Dynamic;
using Unity.Netcode;
using UnityEngine;

public class GameStateManager : NetworkBehaviour
{
    private NetworkVariable<GameState> netState =  
        new(GameState.Waiting);

    public override void OnNetworkSpawn()
    {
        netState.OnValueChanged += OnStateChanged;

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
        if(newState == GameState.MatchIntro)
        {
            var introUI = FindFirstObjectByType<MatchIntroUI>();
            if(introUI != null)
            {
                introUI.Show("Player 1" , "Player 2");

                // 3초 뒤 서버가 상태를 Playing으로 바꿈
                if(IsServer)
                {
                    Invoke(nameof(TransitionToPlaying), 3f);
                }
            }
        }

        else if(newState == GameState.Playing)
            {
                var introUI = FindFirstObjectByType<MatchIntroUI>();
                if(introUI != null) introUI.Hide();
            }
    }

    // 서버에서 3초 뒤 상태 변경
    void TransitionToPlaying()
    {
        netState.Value = GameState.Playing;
    }
}