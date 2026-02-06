using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections;
using System.Runtime.Serialization;
using System.Data;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;

public class TurnUI : MonoBehaviour
{
    [SerializeField] private GameObject turnPanel;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text timerText; // 중앙 타이머 (이제 안 씀)

    /// <summary>
    /// 타이머 UI
    /// </summary>
    [SerializeField] private TMP_Text leftTimerText;
    [SerializeField] private TMP_Text rightTimerText;

    [SerializeField] private TMP_Text leftTurnCountText;
    [SerializeField] private TMP_Text rightTurnCountText;


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
        
        // 현재 턴 주인 쪽만 타이머 표시 (호스트 왼쪽, 클라이언트 오른쪽)
        string timeStr = Mathf.Ceil(TurnManager.Instance.GetRemainingTime()).ToString();
        ulong ownerId = TurnManager.Instance.CurrentTurnClientId.Value;

        if(leftTimerText != null)
            leftTimerText.text = (ownerId == 0) ? timeStr : "--";
        if(rightTimerText != null)
            rightTimerText.text = (ownerId == 1) ? timeStr : "--";

    }

    public void OnEnable()
    {
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

        StopAllCoroutines();
        StartCoroutine(ShowTurnPopup(IsMyTurn));
    }

    private void OnDisable() // 비활성화
    {
        if(TurnManager.Instance != null)
        {
            // 중복 호출 방지
        TurnManager.Instance.CurrentTurnClientId.OnValueChanged
            -= HandleTurnClientIdChanged;
        }        
    }

    public void ShowGameResult(ulong loserId)
    {
        // 내가 패자인지 확인
        bool iLost = NetworkManager.Singleton.LocalClientId
            == loserId;

            StopAllCoroutines();
            StartCoroutine(ShowResultPopup(!iLost)); // 안 졌으면 승리
    }

    IEnumerator ShowTurnPopup(bool IsMyTurn)
    {
        turnText.text = IsMyTurn ? "your turn" : "enemy's turn";
        turnText.color = IsMyTurn ? UnityEngine.Color.green : UnityEngine.Color.red;

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

    IEnumerator ShowResultPopup(bool didIWin)
    {
        turnText.text = didIWin? "You Win!" : "You Lose...";
        turnText.color = didIWin? UnityEngine.Color.green : UnityEngine.Color.red;

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