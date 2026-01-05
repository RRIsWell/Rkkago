using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StoneData", menuName = "Scriptable Objects/StoneData")]
public class StoneData : ScriptableObject
{
    // 예시 입니다
    // 기획 구체화 되고 개발하면서 추가할 예정
    [Header("Physics")] 
    public float baseSpeed;
    public float weight;
    public float scale;
    public float bounceForce;
    
    [Header("Skills")]
    public float damage;
    public float health;

    [Header("Design")] 
    public Sprite sprite;
}
