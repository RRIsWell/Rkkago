using UnityEngine;

public class MatchIntroController : MonoBehaviour
{
    [SerializeField] private MatchIntroUI matchIntroPrefab;

    private MatchIntroUI instance;

    void Awake()
    {
        Debug.Log($"[MatchIntro] Awake - {name} (active={gameObject.activeInHierarchy})");
    }

    void OnEnable()
    {
        Debug.Log($"[MatchIntro] OnEnable - {name}");

        // 초기 상태 저장
        if(GameManager.Instance == null) return;

        GameManager.Instance.OnGameStateChanged
            += HandleStateChanged;

        // 시작하자마자 현재 상태를 UI에 반영
        ApplyState(GameManager.Instance.CurrentGameState);
    }

    void OnDisable()
    {
        Debug.Log($"[MatchIntro] OnDisable - {name}");

        if (GameManager.Instance == null) return;

        GameManager.Instance.OnGameStateChanged -= HandleStateChanged;
    }


    void HandleStateChanged(GameState oldState, GameState newState)
    {
        Debug.Log($"[MatchIntro] State Changed: {oldState} -> {newState}");
        ApplyState(newState);
    }

    void ApplyState(GameState state)
    {
        if(state == GameState.MatchIntro) ShowMatchIntro();
        else DestroyMatchIntro(); // 나머지 경우에는 꺼두기
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
    }

    void DestroyMatchIntro()
    {
        if(instance != null)
        {
            Debug.Log("[MatchIntro] Destroyed");
            Destroy(instance.gameObject);
            instance = null;
        }
    }
}