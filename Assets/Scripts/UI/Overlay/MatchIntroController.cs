using UnityEngine;
using Unity.Netcode;

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

        StartCoroutine(WaitAndBind());
    }

    private System.Collections.IEnumerator WaitAndBind()
    {
        while(GameManager.Instance == null)
            yield return null;
        // 초기 상태 저장

        GameManager.Instance.OnGameStateChanged
            += HandleStateChanged;

        Debug.Log("[MatchIntro] Bound to GameManager");
        Debug.Log($"[MatchIntro] CurrentState at bind = {GameManager.Instance.CurrentGameState}");
        // 시작하자마자 현재 상태를 UI에 반영
        ApplyState(GameManager.Instance.CurrentGameState);

        // 2명 모이면 인트로 띄우기
        StartCoroutine(WaitForTwoPlayersAndShow());
    }

    private System.Collections.IEnumerator WaitForTwoPlayersAndShow()
    {
        // Netcode 시작 기다림
        while(NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            yield return null;
        
        // 2명 연결될 때까지 대기
        while (NetworkManager.Singleton.ConnectedClientsIds == null ||
               NetworkManager.Singleton.ConnectedClientsIds.Count < 2)
            yield return null;

        Debug.Log($"[MatchIntro] 2 players connected: {string.Join(",", NetworkManager.Singleton.ConnectedClientsIds)}");

        // 인트로 시작해서 턴 팝업 막기
        GameManager.IsMatchIntroPlaying = true;
        
        ShowMatchIntro();

        yield return new WaitForSeconds(2f);
        DestroyMatchIntro();

        // 인트로 종료돼서 턴 팝업 허용
        GameManager.IsMatchIntroPlaying = false;

        // 보류해둔 턴 팝업 띄우라고 TurnUI에게 지시
        var ui = FindObjectOfType<TurnUI>();
        if (ui != null)
            ui.PlayDeferredTurnPopup();
    }

    void OnDisable()
    {
        Debug.Log($"[MatchIntro] OnDisable - {name}");

        StopAllCoroutines();

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
        Debug.Log($"[MatchIntro] ApplyState = {state}");
        if(state == GameState.MatchIntro) ShowMatchIntro();
        else DestroyMatchIntro(); // 나머지 경우에는 꺼두기
    }

    void ShowMatchIntro()
    {
        if (matchIntroPrefab == null)
        {
            Debug.LogError("[MatchIntro] matchIntroPrefab 레퍼런스 깨짐");
            return;
        }   
        
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