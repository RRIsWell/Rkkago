using UnityEngine;

public class KingShield : SkillBase
{
    public KingShield(Stone stone, SkillSO data) : base(stone, data)
    {
    }
    
    public override void Activate()
    {
        Stone.ChangeStoneCollisionServerRpc(false);
    }
}
