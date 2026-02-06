using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

public class IceAge : SkillBase
{
    private int _durationTurns;
    private IceAgeSO _so;
    private GameObject _icePrefab;
    private float _tileSpacing;
    private Dictionary<Vector2Int, ValueTuple<GameObject, int>> _activeTiles = new Dictionary<Vector2Int, ValueTuple<GameObject, int>>(); // Vector2Int: 생성 좌표, GameObject: 오브젝트, int: 남은 턴 수

    private StoneController _controller;
    private StoneMovement _movement;
    private NetworkObject _networkObject;
    
    public IceAge(Stone stone, SkillSO data) : base(stone, data)
    {
        _so = data as  IceAgeSO;
        if (_so != null)
        {
            _durationTurns = _so.durationTurns;
            _icePrefab = _so.icePrefab;
            _tileSpacing = _so.tileSpacing;
        }
        _controller = stone.GetComponent<StoneController>();
        _movement = _controller.StoneMovement;
        _networkObject = _controller.NetworkObject;
    }
    
    public override void Activate()
    {
        // 움직일 때 빙판길 생성
        _movement.OnMovement += OnMovementHandler;

        // 턴 바뀔 때
        TurnManager.Instance.OnTurnChanged += DestroySingleIceTile;
    }

    public void Deactivate()
    {
        // 모든 이벤트 구독 해제
        _movement.OnMovement -= OnMovementHandler;
        TurnManager.Instance.OnTurnChanged -= DestroySingleIceTile;

        // 생성한 빙판길 모두 삭제
        DestroyAllIceTiles();
    }
    
    /// <summary>
    /// 네트워크 호출을 통한 빙판길 생성 함수 실행
    /// </summary>
    /// <param name="position"></param>
    private void OnMovementHandler(Vector2 position)
    {
        // MapEffectManager를 통해 네트워크 호출(빙판길 생성)
        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.CreateIceTile(
                position, 
                new NetworkObjectReference(_networkObject)
            );
        }
    }
    
    /// <summary>
    /// 빙판길 생성 함수
    /// </summary>
    /// <param name="position"></param>
    public void CreateSingleIceTile(Vector2 position)
    {
        Vector2Int gridPos = WorldToGrid(position);
        
        // 이미 존재하는 타일이면 return
        if (_activeTiles.ContainsKey(gridPos))
        {
            return;
        }
        
        // 새 타일 생성
        GameObject iceTile = Object.Instantiate(_icePrefab, position,  Quaternion.identity, GameObject.FindWithTag("MainUI").transform);
        _activeTiles.Add(gridPos, (iceTile, Data.durationTurns));
    }

    /// <summary>
    /// 일정 턴 수 지나면 빙판길 삭제
    /// </summary>
    /// <param name="clientId"></param>
    private void DestroySingleIceTile(ulong clientId)
    {
        Debug.Log("제발 사라져");
        foreach (var key in _activeTiles.Keys.ToList())
        {
            if (_activeTiles[key].Item2 <= 0)
            {
                Object.Destroy(_activeTiles[key].Item1);
                _activeTiles.Remove(key);
            }
            else
            {
                var data = _activeTiles[key];
                data.Item2--;
                _activeTiles[key] = data;
            }
        }
    }

    /// <summary>
    /// 모든 빙판길 삭제 후 자료구조 초기화
    /// </summary>
    private void DestroyAllIceTiles()
    {
        foreach (var key in _activeTiles.Keys)
        {
            Object.Destroy(_activeTiles[key].Item1);
        }
        
        _activeTiles.Clear();
    }
    
    private Vector2Int WorldToGrid(Vector2 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / _tileSpacing),
            Mathf.RoundToInt(worldPos.y / _tileSpacing)
        );
    }
}
