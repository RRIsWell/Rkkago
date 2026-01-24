using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameState CurrentGameState { get; private set; } 
        = GameState.Waiting;

    public event Action<GameState, GameState> OnGameStateChanged;

    public void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetGameState(GameState newState)
    {
        var old = CurrentGameState;
        CurrentGameState = newState;

        Debug.Log($"[GameState] -> {newState}");
        OnGameStateChanged?.Invoke(old, newState);
    }
}