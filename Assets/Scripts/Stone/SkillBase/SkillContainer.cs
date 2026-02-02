using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 스킬을 저장하고 실행하는 곳
/// </summary>
public class SkillContainer
{
    private readonly List<SkillBase> _skills;
    private readonly SkillFactory _skillFactory;

    public SkillBase CurrSkill { get; private set; }
    
    
    public List<SkillBase> Skills => _skills;

    public SkillContainer(Stone stone)
    {
        _skills = new List<SkillBase>();
        _skillFactory = new SkillFactory();

        _skills = _skillFactory.CreateSkillList(stone);
    }
    
    /// <summary>
    /// 랜덤 스킬 반환
    /// </summary>
    public Tuple<int, SkillBase> GetRandomSkill()
    {
        int index = Random.Range(0, _skills.Count);
        CurrSkill = _skills[index];
        
        return Tuple.Create(index, _skills[index]);
    }

    /// <summary>
    /// Index로 스킬 반환
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private SkillBase GetSkillByIndex(int index)
    {
        index = Mathf.Clamp(index, 0, _skills.Count - 1);
        return _skills[index];
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

    public void ActivateSkill(int skillIndex)
    {
        SkillBase skill = GetSkillByIndex(skillIndex);
        ActivateSkill(skill);
    }
}
