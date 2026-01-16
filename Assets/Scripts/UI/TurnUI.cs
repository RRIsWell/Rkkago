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

    void Update()
    {
        if(TurnManager.Instance == null) return;

        timerText.text =
            Mathf.Ceil(TurnManager.Instance.GetRemainingTime()).ToString();
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
        if (NetworkManager.Singleton.IsListening && TurnManager.Instance.CurrentTurnClientId.Value != 0)
        {
            HandleTurnClientIdChanged(0, TurnManager.Instance.CurrentTurnClientId.Value);
        }
    }

    // ID 비교해서 턴 판정
    private void HandleTurnClientIdChanged(ulong oldId, ulong newId)
    {        
        bool IsMyTurn =
            NetworkManager.Singleton.LocalClientId == newId;

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
}