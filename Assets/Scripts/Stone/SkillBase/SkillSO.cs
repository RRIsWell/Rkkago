using UnityEngine;

[CreateAssetMenu(fileName = "SkillSO", menuName = "Scriptable Objects/SkillSO")]
public class SkillSO : ScriptableObject
{
    [Header("스킬 이름")]
    public SkillName skillName;
    
    [Header("실행 타입")]
    public SkillActivationType activationType;  // 언제 스킬을 실행할 것인지
    
    [Header("발동 판정 카운트 타입")]
    public SkillCountType countType;    // 언제 스킬이 발동되었다고 판단할 것인지
    
    [Header("스킬 설명")]
    public string skillDescription;

    [Header("스킬 적용 알 수")]
    public int applyStoneCount;     // 스킬 적용 알 수
    
    [Header("지속 턴 수")]
    public int durationTurns;       // 지속 턴 수 (Base에서 제어x)
    
    [Header("실행 제한 횟수")]
    public int activateCounts;      // 스킬 발동 제한 횟수
    
}
