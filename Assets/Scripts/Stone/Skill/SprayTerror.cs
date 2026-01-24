using UnityEngine;

public class SprayTerror : SkillBase
{
    public override string SkillName => "SprayTerror";

    public SprayTerror(Stone stone) : base(stone)
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
