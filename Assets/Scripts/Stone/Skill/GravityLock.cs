using UnityEngine;

public class GravityLock : SkillBase
{
    private readonly float _weight = 2.0f;

    public GravityLock(Stone stone, SkillSO data) : base(stone, data)
    {
    }

    public override void Activate()
    {
        Stone.ChangeStoneWeight(_weight);
        Debug.Log(Data.skillName);
    }
}
