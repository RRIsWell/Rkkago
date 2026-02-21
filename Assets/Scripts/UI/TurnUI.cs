using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections;
using System.Runtime.Serialization;
using System.Data;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;
using UnityEngine.UI;

public class TurnUI : MonoBehaviour
{
    [SerializeField] private GameObject turnPanel;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text timerText; // 중앙 타이머 (이제 안 씀)

    [Header("Popup Image")]
    [SerializeField] private Image turnPopupImage;

    [Header("Turn Popup Sprites")]
    [SerializeField] private Sprite p1_MyTurnSprite;     // P1 테마 MY TURN
    [SerializeField] private Sprite p1_EnemyTurnSprite;  // P1 테마 ENEMY TURN
    [SerializeField] private Sprite p2_MyTurnSprite;     // P2 테마 MY TURN
    [SerializeField] private Sprite p2_EnemyTurnSprite;  // P2 테마 ENEMY TURN

    [Header("Result Popup Sprites")]
    [SerializeField] private Sprite p1_WinSprite;
    [SerializeField] private Sprite p1_LoseSprite;
    [SerializeField] private Sprite p2_WinSprite;
    [SerializeField] private Sprite p2_LoseSprite;

    [SerializeField] private bool disableTextOnStart = true;

    /// <summary>
    /// 타이머 UI
    /// </summary>
    [SerializeField] private TMP_Text leftTimerText;
    [SerializeField] private TMP_Text rightTimerText;

    [SerializeField] private TMP_Text leftTurnCountText;
    [SerializeField] private TMP_Text rightTurnCountText;

    // 동전 애니메이션 UI 연결용
    // [SerializeField] private CoinFlipUI coinFlipUI;


    private bool hasDeferredTurn = false;
    private ulong deferredTurnId;

    void Start()
    {
        // 시작할 때는 팝업 패널 숨김
        if(turnPanel != null) turnPanel.SetActive(false);
    }

    void Update()
    {
        if(TurnManager.Instance == null) return;

        timerText.text =
            Mathf.Ceil(TurnManager.Instance.GetRemainingTime()).ToString();

        // 턴 쌍 표시
        int turnN = 1;
        if(TurnManager.Instance.TurnNumber != null)
            turnN = TurnManager.Instance.TurnNumber.Value;
        
        if(leftTurnCountText !=  null) 
            leftTurnCountText.text = $"Turn {turnN}";
        if(rightTurnCountText !=  null) 
            rightTurnCountText.text = $"Turn {turnN}";
        
        // 현재 턴 주인 쪽만 타이머 표시 (P1 왼쪽, P2 오른쪽)
        string timeStr = Mathf.Ceil(TurnManager.Instance.GetRemainingTime()).ToString();
        ulong ownerId = TurnManager.Instance.CurrentTurnClientId.Value;

        ulong leftId = TurnManager.Instance.Player1ClientId;
        ulong rightId = TurnManager.Instance.Player2ClientId;

        if(leftTimerText != null)
            leftTimerText.text = (ownerId == leftId && leftId != ulong.MaxValue) ? timeStr : "--";
        if(rightTimerText != null)
            rightTimerText.text = (ownerId == rightId && rightId != ulong.MaxValue) ? timeStr : "--";

    }

    public void OnEnable()
    {
        StartCoroutine(WaitAndBindGameState());

    }
    
    private System.Collections.IEnumerator WaitAndBindGameState(){
        while (GameManager.Instance == null)
            yield return null;

        GameManager.Instance.OnGameStateChanged -= StartTurnUI;
        GameManager.Instance.OnGameStateChanged += StartTurnUI;
    }

    private void StartTurnUI(GameState oldState, GameState newState)
    {
        if (newState == GameState.Playing)
            StartCoroutine(WaitAndRegister());
    }

    // TurnManager -> TurnUI로 신호 전달
    private IEnumerator WaitAndRegister()
    {
        // TurnManager 인스턴스 생길 때까지 대기
        while(TurnManager.Instance == null)
        {
            yield return null;
        }

        TurnManager.Instance.CurrentTurnClientId.OnValueChanged
            += HandleTurnClientIdChanged;

        // 동전 던지기 이벤트 구독
        TurnManager.Instance.OnSeatsDecided += HandleSeatsDecided;

        // 첫 턴 팝업 처리
        if(NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening)
        {
            HandleTurnClientIdChanged(
                ulong.MaxValue,
                TurnManager.Instance.CurrentTurnClientId.Value
            );
        }
    }

    // 동전 결과 받기 (애니메이션 시작)
    private void HandleSeatsDecided(bool isHeads, ulong p1LeftId, ulong p2RightId)
    {
        // p1LeftId = host(왼쪽/파란), p2RightId = guest(오른쪽/핑크)
        ulong starter = isHeads ? p1LeftId : p2RightId;
        
        // coinFlipUI 있으면 Play 호출
        // coinFlipUI.Play(isHeads);
    }

    // ID 비교해서 턴 판정
    private void HandleTurnClientIdChanged(ulong oldId, ulong newId)
    {        
        // 인트로 중이면 팝업 띄우지 말고 보류
        if (GameManager.IsMatchIntroPlaying)
        {
            hasDeferredTurn = true;
            deferredTurnId = newId;
            return;
        }

        ShowTurnPopupNow(newId);
    }

    private void ShowTurnPopupNow(ulong turnOwnerId)
    {
        bool IsMyTurn =
            NetworkManager.Singleton.LocalClientId == turnOwnerId;

        ulong localId = NetworkManager.Singleton.LocalClientId;
        bool isLocalP1 = (TurnManager.Instance != null && localId == TurnManager.Instance.Player1ClientId);

        StopAllCoroutines();
        StartCoroutine(ShowTurnPopup(IsMyTurn, isLocalP1));
    }

    private void OnDisable() // 비활성화
    {
        if(TurnManager.Instance != null)
        {
            // 중복 호출 방지
        TurnManager.Instance.CurrentTurnClientId.OnValueChanged
            -= HandleTurnClientIdChanged;

            // 이벤트 해제
            TurnManager.Instance.OnSeatsDecided -= HandleSeatsDecided;
        }        
    }

    public void ShowGameResult(ulong loserId)
    {
        // 내가 패자인지 확인
        bool iLost = NetworkManager.Singleton.LocalClientId
            == loserId;
        
        ulong localId = NetworkManager.Singleton.LocalClientId;
        bool isLocalP1 = (TurnManager.Instance != null && localId == TurnManager.Instance.Player1ClientId);

        StopAllCoroutines();
        StartCoroutine(ShowResultPopup(!iLost, isLocalP1)); // 안 졌으면 승리
    }

    IEnumerator ShowTurnPopup(bool IsMyTurn, bool isLocalP1)
    {
        // 텍스트 대신 스프라이트 교체
        if (turnPopupImage != null)
        {
            if (isLocalP1)
                turnPopupImage.sprite = IsMyTurn ? p1_MyTurnSprite : p1_EnemyTurnSprite;
            else
                turnPopupImage.sprite = IsMyTurn ? p2_MyTurnSprite : p2_EnemyTurnSprite;
        }

        turnPanel.SetActive(true);

        // 2초 대기
        yield return new WaitForSeconds(2f);

        turnPanel.SetActive(false);

        if(NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening &&
            TurnManager.Instance != null &&
            TurnManager.Instance.IsSpawned)
        {        
            TurnManager.Instance.NotifyTurnPopupFinishedServerRpc();
        }
    }

    IEnumerator ShowResultPopup(bool didIWin, bool isLocalP1)
    {
        // 텍스트 대신 스프라이트 교체
        if (turnPopupImage != null)
        {
            if (isLocalP1)
                turnPopupImage.sprite = didIWin ? p1_WinSprite : p1_LoseSprite;
            else
                turnPopupImage.sprite = didIWin ? p2_WinSprite : p2_LoseSprite;
        }

        turnPanel.SetActive(true);

        yield return new WaitForSeconds(4f);

        // 로비로 돌아가는 로직
        UnityEngine.SceneManagement.SceneManager.LoadScene("StartScene");
    }

    public void PlayDeferredTurnPopup()
    {
        if (!hasDeferredTurn) return;

        hasDeferredTurn = false;
        ShowTurnPopupNow(deferredTurnId);
    }

}