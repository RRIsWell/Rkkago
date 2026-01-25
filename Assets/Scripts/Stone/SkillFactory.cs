using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// 스킬을 저장하고 실행하는 곳
/// </summary>
public class SkillFactory
{
    private List<SkillBase> _skills;
    public int SkillCount => _skills.Count;
    
    public SkillFactory(Stone stone)
    {
        _skills = new List<SkillBase>();
        InitSkills(stone);
    }

    public SkillBase GetSkillByIndex(int index)
    {
        index = Mathf.Clamp(index, 0, _skills.Count - 1);
        return _skills[index];
    }
    
    /// <summary>
    /// 스킬 리스트 초기화
    /// </summary>
    /// <param name="stone"></param>
    private void InitSkills(Stone stone)
    {
        // 추가될 때마다 직접 추가해줘야함
        _skills.Add(new ChangeScaleSkill(stone));
        
        // 이어서 추가
        _skills.Add(new GravityLock(stone));
    }
    
    /// <summary>
    /// 랜덤 스킬 반환
    /// </summary>
    public SkillBase ChoiceRandomSkill()
    {
        return _skills[UnityEngine.Random.Range(0, _skills.Count)];
    }

    /// <summary>
    /// 스킬 실행 부분
    /// </summary>
    /// <param name="skill"></param>
    public void ActivateSkill(SkillBase skill)
    {
        if(skill.CanActivate())
            skill.Activate();
    }
}
