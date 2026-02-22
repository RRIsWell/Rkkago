using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections;
using System.Runtime.Serialization;
using System.Data;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

    [Header("Timers / Turn Count")]
    [SerializeField] private TMP_Text leftTimerText;
    [SerializeField] private TMP_Text rightTimerText;

    [SerializeField] private TMP_Text leftTurnCountText;
    [SerializeField] private TMP_Text rightTurnCountText;

    /// <summary>
    /// 결과 팝업
    /// </summary>
    [Header("Result Popup")]
    [SerializeField] private GameObject resultPanel;  // ResultPanel
    [SerializeField] private Image resultBannerImage;  // BannerImage (ROUND WINNER)
    [SerializeField] private Image leftProfileImage;  // LeftProfileImage
    [SerializeField] private Image rightProfileImage;  // RightProfileImage
    [SerializeField] private TMP_Text scoreText;  // ScoreText (1:0 / 0:1)


    [Header("Result Sprites")]
    [SerializeField] private Sprite roundWinnerBannerSprite;  // UI_RoundWinner.png
    [SerializeField] private Sprite p1_ProfileWin;     // P1(승자) 프로필
    [SerializeField] private Sprite p1_ProfileLoseGray;     // P1(패자) 프로필
    [SerializeField] private Sprite p2_ProfileWin;            // P1(승자) 프로필
    [SerializeField] private Sprite p2_ProfileLoseGray;       // P2(패자 회색) 프로필

    [SerializeField] private float winnerScale = 1.2f;
    [SerializeField] private float loserScale = 0.9f;

    // 동전 애니메이션 UI 연결용
    // [SerializeField] private CoinFlipUI coinFlipUI;

    private bool hasDeferredTurn = false;
    private ulong deferredTurnId;

    // 점수용
    private Vector3 _leftBaseScale;
    private Vector3 _rightBaseScale;

    private int p1Score = 0;
    private int p2Score = 0;
    

    void Start()
    {
        // 시작할 때는 팝업 패널 숨김
        if(turnPanel != null) turnPanel.SetActive(false);

        // 결과 패널도 기본은 숨김
        if (resultPanel != null) resultPanel.SetActive(false);

        // 프로필 기본 스케일 저장
        _leftBaseScale  = (leftProfileImage  != null) ? leftProfileImage.transform.localScale  : Vector3.one;
        _rightBaseScale = (rightProfileImage != null) ? rightProfileImage.transform.localScale : Vector3.one;

        // 점수도 숨김이 기본
        if (scoreText != null) scoreText.text = "";
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
        if (resultPanel != null && resultPanel.activeSelf)
            return; // 결과 떠 있으면 턴 팝업 막기

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

    IEnumerator ShowTurnPopup(bool IsMyTurn, bool isLocalP1)
    {
        // 사운드
        if(IsMyTurn)
            SoundManager.Instance.PlaySFX(SFXName.myTurn);
        
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

    // 공통 API: RuleExecutor가 이걸 호출
    public void ShowGameResult(ulong winnerId, ulong loserId, GameEndReason reason)
    {
        ulong p1 = TurnManager.Instance != null ? TurnManager.Instance.Player1ClientId : ulong.MaxValue;
        ulong p2 = TurnManager.Instance != null ? TurnManager.Instance.Player2ClientId : ulong.MaxValue;

        // 점수 갱신
        if (winnerId == p1) { p1Score = 1; p2Score = 0; }
        else if (winnerId == p2) { p1Score = 0; p2Score = 1; }

        StopAllCoroutines();
        StartCoroutine(ShowResultPopup(winnerId, loserId, reason));
    }

    // =========================================
    // 결과 팝업
    // =========================================
    IEnumerator ShowResultPopup(ulong winnerId, ulong loserId, GameEndReason reason)
    {
        // 사운드
        if(winnerId == NetworkManager.Singleton.LocalClientId)
            SoundManager.Instance.PlaySFX(SFXName.승리);
        
        // 턴 팝업 끄기
        if (turnPanel != null) turnPanel.SetActive(false);

        // 배너
        if (resultBannerImage != null && roundWinnerBannerSprite != null)
            resultBannerImage.sprite = roundWinnerBannerSprite;

        ulong p1 = TurnManager.Instance != null ? TurnManager.Instance.Player1ClientId : ulong.MaxValue;
        ulong p2 = TurnManager.Instance != null ? TurnManager.Instance.Player2ClientId : ulong.MaxValue;

        bool p1Won = (winnerId == p1);

        // 프로필 스프라이트
        if (leftProfileImage != null)
            leftProfileImage.sprite = p1Won ? p1_ProfileWin : p1_ProfileLoseGray;

        if (rightProfileImage != null)
            rightProfileImage.sprite = p1Won ? p2_ProfileLoseGray : p2_ProfileWin;

        // 승자 크게 / 패자 작게
        if (leftProfileImage != null)
            leftProfileImage.transform.localScale = _leftBaseScale * (p1Won ? winnerScale : loserScale);

        if (rightProfileImage != null)
            rightProfileImage.transform.localScale = _rightBaseScale * (p1Won ? loserScale : winnerScale);

        // 점수
        if (scoreText != null)
            scoreText.text = $"{p1Score} : {p2Score}";

        if (resultPanel != null) resultPanel.SetActive(true);

        yield return new WaitForSeconds(4f);

        // 씬 전환은 서버(MapRuleExecutor)에서 Netcode로 처리함
    }

    public void PlayDeferredTurnPopup()
    {
        if (!hasDeferredTurn) return;

        hasDeferredTurn = false;
        ShowTurnPopupNow(deferredTurnId);
    }

}