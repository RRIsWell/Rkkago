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

    public void ChangeStonePower(float power)
    {
        stoneData.power += power;
        Debug.Log($"파워 변화 {stoneData.power}");
    }
}
