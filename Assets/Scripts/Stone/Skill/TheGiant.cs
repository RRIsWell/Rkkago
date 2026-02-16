using UnityEngine;

public class TheGiant : SkillBase
{
    private readonly float _scale;
    private readonly float _weight;

    public TheGiant(Stone stone, SkillSO data) : base(stone, data)
    {
        var so = data as TheGiantSO;
        _scale = so.scale;
        _weight = so.weight;
    }

    public override void Activate()
    {
        Stone.ChangeStoneScaleServerRpc(_scale);
        Stone.ChangeStoneWeightServerRpc(_weight);
    }
}
