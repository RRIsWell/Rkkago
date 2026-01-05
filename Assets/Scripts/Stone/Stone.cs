using System;
using UnityEngine;


/// <summary>
/// Stone의 기본 데이터를 관리하는 곳
/// </summary>
public class Stone : MonoBehaviour
{
    [SerializeField] 
    private StoneData stoneData;
    
    private void Awake()
    {
        
    }

    /// <summary>
    /// stone 데이터를 기반으로 speed를 계산하는 함수
    /// speed에 영향을 주는 요소: 알의 무게, 크기
    /// </summary>
    /// <returns></returns>
    public float CalculateSpeed()
    {
        return stoneData.baseSpeed / (stoneData.weight * stoneData.scale);
        
        // 영향도를 다르게 하고 싶다면
        // baseSpeed / (weight * weightFactor + size * sizeFactor)
    }
    
    public void ChangeStoneScale(float scale)
    {
        stoneData.scale = scale;
        Debug.Log($"크기 변화 {stoneData.scale}");
    }
}
