using UnityEngine;

public class NanoShift : SkillBase
{
    private readonly float _scale;

    public NanoShift(Stone stone, SkillSO data) : base(stone, data)
    {
        var so = data as ChangeScaleSO;
        _scale = so.scale;
    }

    public override void Activate()
    {
        Stone.ChangeStoneScale(_scale);
    }
}
