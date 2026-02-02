using UnityEngine;

public class SprayTerror : SkillBase
{
    public SprayTerror(Stone stone, SkillSO data) : base(stone, data)
    {
    }

    public override void Activate()
    {
        // Stone에 연결된 VisualController를 가져와서 실행
        if (Stone.VisualController != null)
        {
            Stone.VisualController.CastGraffitiSkill();
            Debug.Log("[Skill] Spray Terror");
        }
        else
        {
            Debug.LogError("StoneVisualController가 없습니다!");
        }
    }
}
