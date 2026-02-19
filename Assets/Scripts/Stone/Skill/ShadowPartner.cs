using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

public class ShadowPartner : SkillBase
{
    private StoneController _controller;
    private NetworkObject _networkObject;

    private List<ValueTuple<GameObject, int>> _goList = new List<ValueTuple<GameObject, int>>(); // 생성된 분신 리스트
    private GameObject _go1;
    private GameObject _go2;
    
    public ShadowPartner(Stone stone, SkillSO data) : base(stone, data)
    {
        _controller = stone.GetComponent<StoneController>();
        _networkObject = _controller.NetworkObject;
    }

    public override void Init()
    {
        base.Init();
        
        // 오브젝트 생성 이벤트
        _controller.OnShootStone -= RequestToCreateAndShootShadowObjects;
        _controller.OnShootStone += RequestToCreateAndShootShadowObjects;
        
        // 오브젝트 삭제 이벤트
        TurnManager.Instance.OnTurnChanged -= RequestToDestroyShadowObjects;
        TurnManager.Instance.OnTurnChanged += RequestToDestroyShadowObjects;
    }

    public override void Activate()
    {
    }

    public override void Deactivate()
    {
        base.Deactivate();
        
        // 이벤트 구독 해제
        _controller.OnShootStone -= RequestToCreateAndShootShadowObjects;
        TurnManager.Instance.OnTurnChanged -= RequestToDestroyShadowObjects;

        // 생성한 모든 오브젝트 삭제
        RequestToDestroyAllShadowObjects();
    }

    /// <summary>
    /// 네트워크 호출을 통한 분신 생성 함수 실행
    /// </summary>
    /// <param name="velocity"></param>
    private void RequestToCreateAndShootShadowObjects(Vector2 velocity)
    {
        // EffectManager를 통해 네트워크 호출(분신 생성 후 날림)
        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.CreateAndShootShadowObjects(
                velocity,
                new NetworkObjectReference(_networkObject)
            );
        }
    }
    
    /// <summary>
    /// 네트워크 호출을 통한 분신 반투명화 함수 실행
    /// </summary>
    /// <param name="netObj1"></param>
    /// <param name="netObj2"></param>
    private void RequestToSetObjects(NetworkObjectReference netObj1, NetworkObjectReference netObj2)
    {
        // EffectManager를 통해 네트워크 호출(자신의 분신 반투명 처리)
        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.SetShadowObjects(
                new NetworkObjectReference(_networkObject),
                netObj1,
                netObj2
            );
        }
    }
    
    /// <summary>
    /// 네트워크 호출을 통한 분신 삭제 함수 실행
    /// </summary>
    /// <param name="clientId"></param>
    private void RequestToDestroyShadowObjects(ulong clientId)
    {
        // EffectManager를 통해 네트워크 호출(분신 삭제)
        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.DestroyShadowObjects(
                new NetworkObjectReference(_networkObject)
            );
        }
    }
    
    /// <summary>
    /// 네트워크 호출을 통한 분신 모두 삭제 함수 실행
    /// </summary>
    private void RequestToDestroyAllShadowObjects()
    {
        // EffectManager를 통해 네트워크 호출(분신 모두 삭제)
        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.DestroyAllShadowObjects(
                new NetworkObjectReference(_networkObject)
            );
        }
    }
    
    /// <summary>
    /// 분신 오브젝트 생성 함수
    /// </summary>
    /// <param name="velocity"></param>
    public void CreateTwoShadowObjectsAndShoot(Vector2 velocity)
    {
        // 스폰 위치
        Vector2 dir = velocity.normalized;  // 방향(단위벡터)
        float speed = velocity.magnitude;   // 속도(스칼라)
        
        // 좌우 30도
        Vector2 left30 = Quaternion.Euler(0, 0, 30) * dir;
        Vector2 right30 = Quaternion.Euler(0, 0, -30) * dir;
        
        // 스폰
        _go1 = Object.Instantiate(Stone.gameObject, (Vector2)Stone.transform.position + left30,  Quaternion.identity);
        _go1.GetComponent<NetworkObject>().Spawn();
        
        _go2 = Object.Instantiate(Stone.gameObject, (Vector2)Stone.transform.position + right30,  Quaternion.identity);
        _go2.GetComponent<NetworkObject>().Spawn();
        
        _go1.GetComponent<CircleCollider2D>().enabled = false;
        _go2.GetComponent<CircleCollider2D>().enabled = false;
        
        // 리스트에 저장
        _goList.Add((_go1, Data.durationTurns));
        _goList.Add((_go2, Data.durationTurns));
        
        // 발사
        ShootShadowObjects(left30, right30, speed);
            
        // 클라이언트에 생성된 오브젝트 주입
        RequestToSetObjects(_go1, _go2);
    }
    
    private void ShootShadowObjects(Vector2 dir1, Vector2 dir2, float speed)
    {
        _go1.GetComponent<StoneController>().TriggerShootFromCollision(dir1, speed);
        _go2.GetComponent<StoneController>().TriggerShootFromCollision(dir2, speed);
    }
    
    public void SetColorAlpha(NetworkObjectReference  netObj1, NetworkObjectReference netObj2)
    {
        // 반투명 (스킬 시전자만)
        if (_go1 == null || _go2 == null)
        {
            _go1 = netObj1;
            _go2 = netObj2;
        }
        
        SpriteRenderer sr1 = _go1.GetComponent<SpriteRenderer>();

        Color c1 = sr1.color;
        c1.a = 0.1f;   
        sr1.color = c1;
        
        SpriteRenderer sr2 = _go2.GetComponent<SpriteRenderer>();

        Color c2 = sr2.color;
        c2.a = 0.1f;   
        sr2.color = c2;
    }
    
    /// <summary>
    /// 일정 턴 수 지나면 분신 삭제
    /// </summary>
    public void DestroyShadowObjects()
    {
        for (int i = 0; i < _goList.Count; i++)
        {
            if (_goList[i].Item1 != null && _goList[i].Item2 <= 0) 
            {
                // 정해진 턴 종료 후 오브젝트 삭제
                Object.Destroy(_goList[i].Item1);
                var data = _goList[i];
                data.Item1 = null;
                _goList[i] = data;
            }
            else
            {
                var data = _goList[i];
                data.Item2--;
                _goList[i] = data;
            }
        }
    }

    /// <summary>
    /// 모든 분신 삭제 후 자료구조 초기화
    /// </summary>
    public void DestroyAllShadowObjects()
    {
        foreach (var pair in _goList)
        {
            if(pair.Item1 != null)
                Object.Destroy(pair.Item1);
        }
        
        _goList.Clear();
    }
}
