using UnityEngine;

public class NanoShift : SkillBase
{
    private readonly float _scale = 0.5f;

    public NanoShift(Stone stone, SkillSO data) : base(stone, data)
    {
        
    }

    public override void Activate()
    {
        Stone.ChangeStoneScale(_scale);
    }
}
