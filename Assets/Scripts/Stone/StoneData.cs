using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StoneData", menuName = "Scriptable Objects/StoneData")]
public class StoneData : ScriptableObject
{
    // 모든 알이 공통으로 가지고 있고 변하지 않는 값 (Base)
    [Header("Physics Base")] 
    [SerializeField] private float baseSpeed;
    [SerializeField] private float baseDeceleration;

    [Header("Physics Stone")]
    [SerializeField] private float weight;
    [SerializeField] private float scale;
    [SerializeField] private float power;
    [SerializeField] private bool canCollide;
    
    [Header("Skills")]
    [SerializeField] private float damage;
    [SerializeField] private float health;

    public float BaseSpeed => baseSpeed;
    public float BaseDeceleration => baseDeceleration;
    
    public float Weight => weight;
    public float Scale => scale;
    public float Power => power;
    public bool CanCollide => canCollide;

    public float Damage => damage;
    public float Health => health;
}
