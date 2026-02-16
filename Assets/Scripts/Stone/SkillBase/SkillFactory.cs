using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public enum SkillName{
    ChangeScale,
    GravityLock,
    NanoShift,
    Hacking,
    IceAge,
    Teleportation,
}

/// <summary>
/// 스킬 객체를 생성하는 곳
/// </summary>
public class SkillFactory
{
    /// <summary>
    /// 스킬 객체 딕셔너리 생성 후 반환하는 함수 
    /// </summary>
    /// <param name="stone"></param>
    public Dictionary<SkillName, SkillBase> CreateSkillDictionary(Stone stone)
    {
        var datas = Resources.Load<SkillData>("Skills/SkillData");
        Dictionary<SkillName, SkillBase> skills = new Dictionary<SkillName, SkillBase>();
        
        foreach (var so in datas.SkillSO)
        {
            switch (so.skillName)
            {
                case SkillName.ChangeScale:
                    skills.Add(SkillName.ChangeScale, new ChangeScaleSkill(stone, so));
                    break;
                case SkillName.GravityLock:
                    skills.Add(SkillName.GravityLock, new GravityLock(stone, so));
                    break;
                case SkillName.NanoShift:
                    skills.Add(SkillName.NanoShift, new NanoShift(stone, so));
                    break;
                case SkillName.Hacking:
                    skills.Add(SkillName.Hacking, new Hacking(stone, so));
                    break;
                case SkillName.IceAge:
                    skills.Add(SkillName.IceAge, new IceAge(stone, so));
                    break;
                case SkillName.Teleportation:
                    skills.Add(SkillName.Teleportation, new Teleportation(stone, so));
                    break;
            
                default:
                    throw new Exception($"알 수 없는 스킬 타입: {so.skillName}");
            }
        }

        return skills;
    }
}
