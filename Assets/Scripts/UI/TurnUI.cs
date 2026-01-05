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

        TurnManager.Instance.CurrentTurnClientId.OnValueChanged
            += HandleTurnClientIdChanged;
        TurnManager.Instance.OnRemainingTimeChanged
            += HandleRemainingTimeChanged;
    }

    private void HandleRemainingTimeChanged(float newTime)
    {
        timerText.text = Mathf.Ceil(newTime).ToString();
    }

    private void HandleTurnClientIdChanged(ulong oldId, ulong newId)
    {
        if(NetworkManager.Singleton.IsHost)
            return;
        
        bool IsMyTurn =
            NetworkManager.Singleton.LocalClientId == newId;

        StopAllCoroutines();
        StartCoroutine(ShowTurnPopup(IsMyTurn));
    }

    private void OnDisable() // 비활성화
    {
        if(TurnManager.Instance == null) return;

        // 중복 호출 방지
        TurnManager.Instance.CurrentTurnClientId.OnValueChanged
            -= HandleTurnClientIdChanged;
        
        TurnManager.Instance.OnRemainingTimeChanged
            -= HandleRemainingTimeChanged;
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