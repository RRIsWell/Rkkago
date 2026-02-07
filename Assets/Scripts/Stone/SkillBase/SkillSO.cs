using UnityEngine;

[CreateAssetMenu(fileName = "SkillSO", menuName = "Scriptable Objects/SkillSO")]
public class SkillSO : ScriptableObject
{
    public SkillName skillName;
    public SkillActivationType activationType;
    public string skillDescription;

    public int applyStoneCount;     // 스킬 적용 알 수
    public int durationTurns;       // 지속 턴 수

}
