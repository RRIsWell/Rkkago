using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.WSA;

/// <summary>
/// 스킬 실행 타입
/// </summary>
public enum SkillActivationType
{
    OnEquip,        // 스킬이 부여될 때
    OnReleaseMouse, // 마우스 드래그 후 뗄 때
    OnTurnStarted, // 턴 시작할 때
}

/// <summary>
/// 스킬이 발동되었다고 인식되는 타입
/// </summary>
public enum SkillCountType
{
    OnShoot,    // 알 발사할 때
    OnCollide,  // 다른 알에 의해 충돌되었을 때
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
    public SkillCountType CountType { get; }
    
    private StoneController _stoneController;
    private int _activateCount;
    
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
        CountType = data.countType;
        _activateCount = 0;
    }

    /// <summary>
    /// 스킬 부여될 때마다 한 번 호출
    /// </summary>
    public virtual void Init()
    {
        // 스킬 아이콘 Sprite 변경
        // 꼭 Stone Sprite Library에 스킬 아이콘 이미지 등록해야함 (스킬 이름으로 등록)
        Stone.Animator.enabled = false;
        Stone.Resolver.SetCategoryAndLabel("Idle", SkillName.ToString());
        
        _stoneController = Stone.GetComponent<StoneController>();
    }
    
    public virtual bool CanActivate()
    {
        return true;
    }
    
    /// <summary>
    /// 스킬 활성화
    /// </summary>
    public abstract void Activate();

    /// <summary>
    /// 장착은 되었지만 실행은 안 되는 상태
    /// </summary>
    public virtual void Inactivate()
    {
        
    }
    /// <summary>
    /// 스킬 비활성화 (스킬 바뀔 때 실행됨)
    /// </summary>
    public virtual void Deactivate()
    {
        // 스킬 아이콘 리셋
        Stone.Resolver.SetCategoryAndLabel("Idle", "Basic");
        
        // 스킬 카운트 리셋
        _activateCount = 0;
        
        _stoneController.StoneMovement.OnMovementEnded -= ResetSkill;
    }

    /// <summary>
    /// 스킬 리셋 (스킬 발동 제한 횟수 다 썼을 때 실행됨)
    /// </summary>
    private void ResetSkill()
    {
        // 스킬 아이콘 리셋
        Stone.Resolver.SetCategoryAndLabel("Idle", "Basic");
        
        // 스킬 카운트 리셋
        _activateCount = 0;

        // 데이터 리셋
        _stoneController.ResetSkillServerRpc();

        Inactivate();
    }

    /// <summary>
    /// 스킬 발동 횟수 카운트
    /// </summary>
    public void ActivateCount()
    {
        _activateCount++;

        if (_activateCount >= Data.activateCounts)
        {
            _stoneController.StoneMovement.OnMovementEnded -= ResetSkill;
            _stoneController.StoneMovement.OnMovementEnded += ResetSkill;
        }
            
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
        Stone.ChangeStoneScaleServerRpc(_scale);
        Debug.Log(Data.skillName);
    }

    public override void Deactivate()
    {
        
    }
    
}
