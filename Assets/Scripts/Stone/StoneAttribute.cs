using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 알 데이터 저장용
/// </summary>
public class StoneAttribute : NetworkBehaviour
{
    [SerializeField] 
    private StoneData stoneData;
    public StoneData BaseData => stoneData;
    
    public float baseSpeed;
    public float baseDeceleration;
    
    public float weight;    // 무게
    public float scale;     // 크기
    public float power;     // 힘
    
    public float damage;
    public float health;
    
    void Awake()
    {
        baseSpeed = stoneData.BaseSpeed;
        baseDeceleration = stoneData.BaseDeceleration;
        
        weight = stoneData.Weight;
        scale = stoneData.Scale;
        power = stoneData.Power;
        
        damage = stoneData.Damage;
        health = stoneData.Health;
    }
}
