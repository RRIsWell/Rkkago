using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.WSA;

public interface ISkill
{
    public SkillSO Data { get; }
    public void Activate();
}

public abstract class SkillBase : ISkill
{
    public SkillName SkillName { get; }
    protected Stone Stone { get; private set; }
    public SkillSO Data { get; private set; }

    /// <summary>
    /// 무조건 자식 클래스에서 부모 생성자 호출해야함
    /// </summary>
    /// <param name="stone"></param>
    /// <param name="data"></param>
    protected SkillBase(Stone stone, SkillSO data)
    {
        this.Stone = stone;
        this.Data = data;
        
        SkillName = data.skillName;
    }
    
    public virtual bool CanActivate()
    {
        return true;
    }
    
    public abstract void Activate();
}

/// <summary>
/// 예시 스킬
/// </summary>
public class ChangeScaleSkill : SkillBase
{
    private readonly float _scale;
    
    public ChangeScaleSkill(Stone stone, SkillSO data) : base(stone, data)
    {
        var so = data as ChangeScaleSO;
        if (so != null) _scale = so.scale;
    }
    
    public override void Activate()
    {
        Stone.ChangeStoneScale(_scale);
        Debug.Log(Data.skillName);
    }
    
}
