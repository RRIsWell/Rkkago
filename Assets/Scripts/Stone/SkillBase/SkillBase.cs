using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.WSA;

public enum SkillActivationType
{
    OnEquip,        // 스킬이 부여될 때
    OnReleaseMouse, // 마우스 드래그 후 뗄 때
}

public interface ISkill
{
    public SkillSO Data { get; }
    public void Activate();
    string SkillDescription { get; }
}

public abstract class SkillBase : ISkill
{
    public virtual string SkillDescription => SkillDescription;
    protected Stone Stone { get; private set; }
    public SkillSO Data { get; private set; }
    
    public SkillName SkillName { get; }
    public SkillActivationType ActivationType { get; }

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
        ActivationType = data.activationType;
    }

    public virtual void Init()
    {
        // 스킬 아이콘 Sprite 변경
        // 꼭 Stone Sprite Library에 스킬 아이콘 이미지 등록해야함 (스킬 이름으로 등록)
        Stone.Animator.enabled = false;
        Stone.Resolver.SetCategoryAndLabel("Idle", SkillName.ToString());
    }
    
    public virtual bool CanActivate()
    {
        return true;
    }
    
    // 스킬 활성화
    public abstract void Activate();    

    // 스킬 비활성화 (스킬 바뀔 때 실행됨)
    public virtual void Deactivate()
    {
        Stone.Resolver.SetCategoryAndLabel("Idle", "Basic");
    }  
}

/// <summary>
/// 예시 스킬
/// </summary>
public class ChangeScaleSkill : SkillBase
{
    private readonly float _scale = 2.0f;
    
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

    public override void Deactivate()
    {
        
    }
    
}
