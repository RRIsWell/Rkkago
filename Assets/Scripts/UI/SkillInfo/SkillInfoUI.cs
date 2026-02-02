using TMPro;
using UnityEngine;

/// <summary>
/// 현재 보유 스킬 정보를 표시하는 UI
/// </summary>
public class SkillInfoUI : MonoBehaviour
{
    [Header("스킬 정보")]
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private TMP_Text skillDescText;

    public void Show(SkillName skillName, string skillDescription)
    {
        if (skillNameText != null)
            skillNameText.text = skillName.ToString();
        if (skillDescText != null)
            skillDescText.text = skillDescription;

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
