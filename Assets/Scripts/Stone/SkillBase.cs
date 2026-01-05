using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.WSA;

public interface ISkill
{
    string SkillName { get; }
    public void Activate();
}

public abstract class SkillBase : ISkill
{
    public abstract string SkillName { get; }
    protected Stone Stone { get; private set; }

    /// <summary>
    /// 무조건 자식 클래스에서 부모 생성자 호출해야함
    /// </summary>
    /// <param name="stone"></param>
    protected SkillBase(Stone stone)
    {
        this.Stone = stone;
    }
    
    public virtual bool CanActivate()
    {
        return true;
    }
    
    public abstract void Activate();
}

public class ChangeScaleSkill : SkillBase
{
    public override string SkillName => "ChangeScale";
    private readonly float _scale = 2.0f;
    
    public ChangeScaleSkill(Stone stone) : base(stone)
    {
    }
    
    public override void Activate()
    {
        Stone.ChangeStoneScale(_scale);
    }
}
