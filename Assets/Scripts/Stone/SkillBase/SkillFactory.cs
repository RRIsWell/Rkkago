using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

/// <summary>
/// 스킬 객체를 생성하는 곳
/// </summary>
public class SkillFactory
{
    /// <summary>
    /// 스킬 객체 리스트 생성 후 반환하는 함수 
    /// </summary>
    /// <param name="stone"></param>
    public List<SkillBase> CreateSkills(Stone stone)
    {
        List<SkillBase> skills = new List<SkillBase>
        {
            // 추가될 때마다 직접 추가해줘야함
            new ChangeScaleSkill(stone),
            new GravityLock(stone) 
        };

        return skills;
    }
    
}
