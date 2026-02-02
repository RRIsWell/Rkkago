using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public enum SkillName{
    ChangeScale,
    GravityLock,
    
}

/// <summary>
/// 스킬 객체를 생성하는 곳
/// </summary>
public class SkillFactory
{
    /// <summary>
    /// 스킬 객체 리스트 생성 후 반환하는 함수 
    /// </summary>
    /// <param name="stone"></param>
    public List<SkillBase> CreateSkillList(Stone stone)
    {
        var datas = Resources.Load<SkillData>("Skills/SkillData");
        List<SkillBase> skills = new List<SkillBase>();
        
        foreach (var data in datas.SkillSO)
        {
            skills.Add(CreateSKill(stone, data));
        }

        return skills;
    }

    public SkillBase CreateSKill(Stone stone, SkillSO so)
    {
        switch (so.skillName)
        {
            case SkillName.ChangeScale:
                return new ChangeScaleSkill(stone, so);
            case SkillName.GravityLock:
                return new GravityLock(stone, so);
            
            default:
                throw new Exception($"알 수 없는 스킬 타입: {so.skillName}");
        }
    }


}
