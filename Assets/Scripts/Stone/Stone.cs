using System;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering.VirtualTexturing;
using UnityEngine.U2D.Animation;


/// <summary>
/// Stone의 기본 데이터 관련 계산, 애니메이션
/// </summary>
public class Stone : NetworkBehaviour
{
    private StoneAttribute _stoneData;
    
    private Animator _animator;
    private SpriteResolver _resolver;
    private SpriteRenderer _renderer;
    private StoneVisualController _visualController;
    private MapRuleExecutor _ruleExecutor;
    
    public Animator Animator => _animator;
    public SpriteResolver Resolver => _resolver;
    
    private bool _deadEventCalled = false;
    
    // Animation Parameters
    public static readonly int HashDead = Animator.StringToHash("Dead");
    
    private void Awake()
    {
        _stoneData = GetComponent<StoneAttribute>();
        _animator =  GetComponent<Animator>();
        _renderer = GetComponent<SpriteRenderer>();
        _visualController = GetComponent<StoneVisualController>();
        _resolver = GetComponent<SpriteResolver>();
        
        _animator.enabled = false;
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
        ChangeStoneScale(_stoneData.BaseData.Scale);
        ChangeStoneWeight(_stoneData.BaseData.Weight);

        // 시각적 효과(낙서, 색상 등) 모두 제거 요청
        if (_visualController != null)
        {
            _visualController.ResetVisuals();
        }
    }

    /// <summary>
    /// stone 데이터를 기반으로 speed를 계산하는 함수
    /// speed에 영향을 주는 요소: 무게, 파워, 플레이어가 당긴 힘
    /// </summary>
    /// <returns></returns>
    public float CalculateBaseSpeed()
    {
        float weightFactor = (_stoneData.weight - 1.0f) * 0.2f + 1.0f;   // 무게 가중치
        return _stoneData.baseSpeed * _stoneData.power * weightFactor;
    }

    /// <summary>
    /// 물체와 충돌 이후 speed를 계산하는 함수 (충격량)
    /// 충격량에 영향을 주는 요소: 무게
    /// </summary>
    /// <param name="otherSpeed">부딪힌 알의 speed</param>
    /// <returns></returns>
    public float CalculateCollisionSpeed(float otherSpeed)
    {
        return otherSpeed / _stoneData.weight;
    }

    /// <summary>
    /// stone 데이터를 기반으로 감속도를 계산하는 함수
    /// 감속도에 영향을 주는 요소: 무게 (기본 50)
    /// </summary>
    /// <returns></returns>
    public float CalculateDeceleration()
    {
        return Mathf.Clamp(_stoneData.baseDeceleration + 300.0f * (_stoneData.weight - 1.0f), 0, 200f);
    }
    
    public void ChangeStoneScale(float scale)
    {
        _stoneData.scale = scale;
        //Debug.Log($"크기 변화 {stoneData.scale}");
    }
    
    public void ChangeStoneWeight(float weight)
    {
        _stoneData.weight = weight;
        //Debug.Log($"무게 변화 {stoneData.weight}");
    }

    [ClientRpc]
    public void SetAnimatorTriggerClientRpc(int param)
    {
        _animator.enabled = true;
        _animator.SetTrigger(param);
    }
    
    /// <summary>
    /// 스킬에서 visualController에 접근하기 위한 getter
    /// </summary>
    public StoneVisualController VisualController => _visualController;
    
    /// <summary>
    /// Dead 애니메이션 이벤트 실행 함수
    /// </summary>
    public void OnDestroyStone()
    {
        _animator.enabled = false;
        
        if(_deadEventCalled) return;
        _deadEventCalled = true;

        // 서버에서 승패/디스폰/스킬 분배까지 처리
        if(IsServer && _ruleExecutor != null)
        {
            _ruleExecutor?.OnStoneOut(this);
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// MapRuleExecutor를 Set으로 받게 함
    /// </summary>
    public void SetRuleExecutor(MapRuleExecutor executor)
    {
        _ruleExecutor = executor;
    }
}
