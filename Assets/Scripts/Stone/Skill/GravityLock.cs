using UnityEngine;

public class GravityLock : SkillBase
{
    public override string SkillName => "GravityLock";
    private readonly float _weight = 2.0f;

    public GravityLock(Stone stone) : base(stone)
    {
    }

    public override void Activate()
    {
        Stone.ChangeStoneWeight(_weight);
    }
}
