using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections;

public class TurnUI : MonoBehaviour
{
    [SerializeField] private GameObject turnPanel;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text timerText;


    void Start()
    {
        // 시작할 때는 팝업 패널 숨김
        if(turnPanel != null) turnPanel.SetActive(false);
    }

    // TurnManager -> TurnUI로 신호 전달
    private void OnEnable()
    {
        if(TurnManager.Instance == null) return; // NullReferenceException 방지

        TurnManager.Instance.OnTurnChanged += HandleTurnChanged;
    }

    private void OnDisable() // 비활성화
    {
        if(TurnManager.Instance == null) return;

        TurnManager.Instance.OnTurnChanged -= HandleTurnChanged; // 중복 호출 방지
    }

    void Update()
    {
        if(TurnManager.Instance == null) return;

        // 타이머 갱신 (올림해서 정수 처리)
        timerText.text = 
            Mathf.Ceil(TurnManager.Instance.GetRemainingTime()).ToString();
    }

    // ID 비교해서 턴 판정
    void HandleTurnChanged(ulong newTurnClientId)
    {
        bool IsMyTurn = 
            NetworkManager.Singleton.LocalClientId == newTurnClientId;
        
        StopAllCoroutines();
        StartCoroutine(ShowTurnPopup(IsMyTurn));
    }

    IEnumerator ShowTurnPopup(bool IsMyTurn)
    {
        turnText.text = IsMyTurn ? "your turn" : "enemy's turn";
        turnText.color = IsMyTurn ? UnityEngine.Color.green : UnityEngine.Color.red;

        turnPanel.SetActive(true);

        // 2초 대기
        yield return new WaitForSeconds(2f);

        turnPanel.SetActive(false);
    }
}