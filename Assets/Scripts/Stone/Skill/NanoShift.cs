using UnityEngine;

public class NanoShift : SkillBase
{
    public override string SkillName => "NanoShift";
    private readonly float _scale = 0.5f;

    public NanoShift(Stone stone) : base(stone)
    {
        
    }

    public override void Activate()
    {
        Stone.ChangeStoneScale(_scale);
    }
}
