using Unity.Netcode;
using UnityEngine;

public class GameStateNetworkDriver : NetworkBehaviour
{
    private NetworkVariable<GameState> netState =  
        new(GameState.Waiting);

    public override void OnNetworkSpawn()
    {
        netState.OnValueChanged += OnStateChanged;

        if(IsServer)
        {
            TryStartMatch();
        }
    }

    void TryStartMatch()
    {
        if(NetworkManager.Singleton.ConnectedClients.Count == 2)
        {
            netState.Value = GameState.MatchIntro;
        }
    }

    void OnStateChanged(GameState oldState, GameState newState)
    {
        GameManager.Instance.SetGameState(newState);
    }
}