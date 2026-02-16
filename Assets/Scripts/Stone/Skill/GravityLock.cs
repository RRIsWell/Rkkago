using UnityEngine;

public class GravityLock : SkillBase
{
    private float _weight;

    public GravityLock(Stone stone, SkillSO data) : base(stone, data)
    {
        var so = data as GravityLockSO;
        _weight = so.weight;
    }

    public override void Activate()
    {
        Stone.ChangeStoneWeightServerRpc(_weight);
        Debug.Log(Data.skillName);
    }
}
