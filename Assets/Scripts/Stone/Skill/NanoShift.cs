using UnityEngine;

public class NanoShift : SkillBase
{
    private readonly float _scale;
    private readonly float _weight;

    public NanoShift(Stone stone, SkillSO data) : base(stone, data)
    {
        var so = data as NanoShiftSO;
        _scale = so.scale;
        _weight = so.weight;
    }

    public override void Activate()
    {
        Stone.ChangeStoneScaleServerRpc(_scale);
        Stone.ChangeStoneWeightServerRpc(_weight);
    }
}
