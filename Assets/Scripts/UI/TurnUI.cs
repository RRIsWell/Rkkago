using UnityEngine;
using TMPro;

public class TurnUI : MonoBehaviour
{
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text timerText;

    void Update()
    {
        // NullReferenceException 방지용
        if(TurnManager.Instance == null) return;

        // 현재 턴이 나인지
        bool myTurn = TurnManager.Instance.IsMyTurn();

        turnText.text = myTurn ? "your turn" : "enemy's turn";

        float time = TurnManager.Instance.GetRemainingTime();
        timerText.text = Mathf.Ceil(time).ToString(); // 올림해서 정수 처리
    }
}