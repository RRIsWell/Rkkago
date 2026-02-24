using UnityEngine;

public class TheGiant : SkillBase
{
    private readonly float _scale;
    private readonly float _weight;
    private readonly float _power;

    public TheGiant(Stone stone, SkillSO data) : base(stone, data)
    {
        var so = data as TheGiantSO;
        _scale = so.scale;
        _weight = so.weight;
        _power = so.power;
    }

    public override void Activate()
    {
        Stone.ChangeStoneScaleServerRpc(_scale);
        Stone.ChangeStoneWeightServerRpc(_weight);
        Stone.ChangeStonePowerServerRpc(_power);
    }
}
