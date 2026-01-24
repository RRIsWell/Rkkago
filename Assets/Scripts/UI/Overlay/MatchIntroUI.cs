using TMPro;
using UnityEngine;

public class MatchIntroUI : MonoBehaviour
{
    [Header("플레이어 1")]
    [SerializeField] private TMP_Text p1NameText;
    [SerializeField] private UnityEngine.UI.Image p1ProfileImage;

    [Header("플레이어 2")]
    [SerializeField] private TMP_Text p2NameText;
    [SerializeField] private UnityEngine.UI.Image p2ProfileImage;

    // 매칭 화면
    public void Show(string p1Name, string p2Name)
    {
        p1NameText.text = p1Name;
        p2NameText.text = p2Name;
        
        Debug.Log("MatchIntroUI Show");
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}