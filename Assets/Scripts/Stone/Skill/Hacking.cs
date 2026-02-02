using UnityEngine;

public class Hacking : SkillBase
{
    public Hacking(Stone stone, SkillSO data) : base(stone, data)
    {
    }

    public override void Activate()
    {
        if (Stone.VisualController != null)
        {
            Stone.VisualController.SetVisualStateServerRpc(VisualState.Deception);
            
        }
    }
}
