using System;
using UnityEngine;
using Unity.Netcode;


/// <summary>
/// Stone의 기본 데이터를 관리하는 곳
/// </summary>
public class Stone : NetworkBehaviour
{
    [SerializeField] 
    private StoneData stoneData;
    
    private Animator _animator;
    private SpriteRenderer _renderer;
    private StoneVisualController _visualController;
    
    private float _defaultScale;
    private float _defaultWeight;
    
    // Animation Parameters
    public static readonly int HashDead = Animator.StringToHash("Dead");
    
    private void Awake()
    {
        _animator =  GetComponent<Animator>();
        _renderer = GetComponent<SpriteRenderer>();
        _visualController = GetComponent<StoneVisualController>();
        
        _defaultScale = stoneData.scale;
        _defaultWeight = stoneData.weight;
    }

    public void SetTeam(int teamId)
    {
        // 모든 클라이언트 색상 바꿈
        SetTeamClientRpc(teamId);
    }

    [ClientRpc]
    private void SetTeamClientRpc(int teamId)
    {
        if (_visualController != null)
        {
            _visualController.InitializeVisuals(teamId);
        }
        
    }
    
    /// <summary>
    /// 모든 상태를 태어날 때로 되돌리는 함수
    /// </summary>
    public void ResetStoneState()
    {
        // 물리적 수치 원상복구
        ChangeStoneScale(_defaultScale);
        ChangeStoneWeight(_defaultWeight);

        // 시각적 효과(낙서, 색상 등) 모두 제거 요청
        if (_visualController != null)
        {
            _visualController.ResetVisuals();
        }
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

    public void ChangeStoneWeight(float weight)
    {
        stoneData.weight = weight;
        Debug.Log($"무게 변화 {stoneData.weight}");
    }

    public void SetAnimatorTrigger(int param)
    {
        _animator.SetTrigger(param);
    }
    
    /// <summary>
    /// 스킬에서 visualController에 접근하기 위한 getter
    /// </summary>
    public StoneVisualController VisualController => _visualController;
    
    /// <summary>
    /// Dead 애니메이션 이벤트 실행 함수
    /// </summary>
    public void OnDestroy()
    {
        Destroy(gameObject);
    }
}
