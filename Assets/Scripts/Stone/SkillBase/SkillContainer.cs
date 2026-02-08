using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 스킬을 저장하고 실행하는 곳
/// </summary>
public class SkillContainer
{
    private readonly Dictionary<SkillName, SkillBase> _skills;
    private readonly List<SkillName> _skillKeys;
    private readonly SkillFactory _skillFactory;
    
    public Dictionary<SkillName, SkillBase> Skills => _skills;

    public SkillContainer(Stone stone)
    {
        _skills = new Dictionary<SkillName, SkillBase>();
        _skillFactory = new SkillFactory();

        _skills = _skillFactory.CreateSkillDictionary(stone);
        _skillKeys = new List<SkillName>(_skills.Keys);
    }
    
    /// <summary>
    /// 랜덤 스킬 반환
    /// </summary>
    public Tuple<int, SkillBase> GetRandomSkill()
    {
        int index = Random.Range(0, _skills.Count);
        return Tuple.Create(index, _skills[_skillKeys[index]]);
    }

    /// <summary>
    /// Index로 스킬 반환
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public SkillBase GetSkillByIndex(int index)
    {
        index = Mathf.Clamp(index, 0, _skills.Count - 1);
        return _skills[_skillKeys[index]];
    }

    /// <summary>
    /// 이름으로 스킬 반환
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public SkillBase GetSkillByName(SkillName name)
    {
        return _skills[name];
    }

    /// <summary>
    /// 스킬 기본 세팅
    /// </summary>
    /// <param name="skill"></param>
    public void InitSkill(SkillBase skill)
    {
        skill.Init();
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
