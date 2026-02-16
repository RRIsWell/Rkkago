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

    // 무게
    private readonly NetworkVariable<float> _weight = new NetworkVariable<float>(
        0f, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );
    // 크기 
    private readonly NetworkVariable<float> _scale = new NetworkVariable<float>(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    // 힘
    private readonly NetworkVariable<float> _power = new NetworkVariable<float>(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    public float Weight
    {
        get => _weight.Value;
        set
        {
            if(IsServer)
                _weight.Value = value;
        }
    }

    public float Scale     
    {
        get => _scale.Value;
        set
        {
            if (IsServer)
            {
                _scale.Value = value;
                transform.localScale = Vector3.one * _scale.Value;
            }
        }
    }

    public float Power
    {
        get => _power.Value;
        set
        {
            if (IsServer)
            {
                _power.Value = value;
            }
        }
    }
    
    public float damage;
    public float health;
    
    void Awake()
    {
        ResetData();
    }

    /// <summary>
    /// 데이터를 초기화하는 함수 (Deactivate시 실행)
    /// </summary>
    public void ResetData()
    {
        baseSpeed = stoneData.BaseSpeed;
        baseDeceleration = stoneData.BaseDeceleration;

        if (IsOwner)
        {
            SetWeightServerRpc(stoneData.Weight);
            SetScaleServerRpc(stoneData.Scale);
            SetPowerServerRpc(stoneData.Power);
        }
        
        damage = stoneData.Damage;
        health = stoneData.Health;
    }

    [ServerRpc]
    private void SetWeightServerRpc(float weight)
    {
        Weight = weight;
    }
    [ServerRpc]
    private void SetScaleServerRpc(float scale)
    {
        Scale = scale;
    }
    [ServerRpc]
    private void SetPowerServerRpc(float power)
    {
        Power = power;
    }
    
}
