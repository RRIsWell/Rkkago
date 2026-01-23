using System.Dynamic;
using UnityEngine;

public class MatchIntroController : MonoBehaviour
{
    [SerializeField] private MatchIntroUI matchIntroPrefab;

    private MatchIntroUI instance;
    private GameState lastState;

    void Start()
    {
        // 초기 상태 저장
        lastState = GameManager.Instance.CurrentGameState;
    }

    void Update()
    {
        var current = GameManager.Instance.CurrentGameState;

        if(current != lastState)
        {
            OnGameStateChanged(lastState, current);
            lastState = current;
        }
    }

    void OnGameStateChanged(GameState oldState, GameState newState)
    {
        if(newState == GameState.MatchIntro)
        {
            ShowMatchIntro();
        }
        else
        {
            HideMatchIntro();
        }
    }

    void ShowMatchIntro()
    {
        if(instance == null)
        {
            instance = Instantiate(
                matchIntroPrefab,
                transform // OverlayRoot
            );

            instance.Show("Player 1", "Player 2");
        }

        // 테스트용 이름
        instance.Show("Player 1", "Player 2");
    }

    void HideMatchIntro()
        {
            if(instance != null) 
                instance.Hide();
        }
}