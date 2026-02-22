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

    private Vector2Int _currentTilePos;
    
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

    public override void Init()
    {
        base.Init();
        
        // 움직임 끝나면 빙판길 업데이트 해제
        _movement.OnMovementEnded -= EndUpdateIceScale;
        _movement.OnMovementEnded += EndUpdateIceScale;
        
        // 턴 바뀔 때
        TurnManager.Instance.OnTurnChanged -= RequestToDestroyIceTile;
        TurnManager.Instance.OnTurnChanged += RequestToDestroyIceTile;
    }

    public override void Activate()
    {        
        _currentTilePos = Vector2Int.zero;
        
        // 빙판길 생성
        RequestToCreateIceTile(Stone.gameObject.transform.position);
        
        // 움직일 때 빙판길 업데이트
        _movement.OnMovement -= RequestToUpdateIceTile;
        _movement.OnMovement += RequestToUpdateIceTile;
    }

    public override void Deactivate()
    {
        base.Deactivate();
        
        // 모든 이벤트 구독 해제
        _movement.OnMovement -= RequestToUpdateIceTile;
        _movement.OnMovementEnded -= EndUpdateIceScale;
        TurnManager.Instance.OnTurnChanged -= RequestToDestroyIceTile;

        // 생성한 빙판길 모두 삭제
        RequestToDestroyAllIceTiles();
    }
    
    /// <summary>
    /// 네트워크 호출을 통한 빙판길 생성 함수 실행
    /// </summary>
    /// <param name="position"></param>
    private void RequestToCreateIceTile(Vector2 position)
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
    /// 네트워크 호출을 통한 빙판길 업데이트 함수 실행
    /// </summary>
    /// <param name="stonePos"></param>
    private void RequestToUpdateIceTile(Vector2 stonePos)
    {
        // MapEffectManager를 통해 네트워크 호출(빙판길 생성)
        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.UpdateIceTile(
                stonePos, 
                new NetworkObjectReference(_networkObject)
            );
        }
    }
    
    /// <summary>
    /// 네트워크 호출을 통한 빙판길 삭제 함수 실행
    /// </summary>
    /// <param name="clientId"></param>
    private void RequestToDestroyIceTile(ulong clientId)
    {
        // MapEffectManager를 통해 네트워크 호출(빙판길 삭제)
        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.DestroyIceTile(
                new NetworkObjectReference(_networkObject)
            );
        }
    }
    
    /// <summary>
    /// 네트워크 호출을 통한 빙판길 모두 삭제 함수 실행
    /// </summary>
    private void RequestToDestroyAllIceTiles()
    {
        // MapEffectManager를 통해 네트워크 호출(빙판길 모두 삭제)
        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.DestroyAllIceTiles(
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
        iceTile.transform.localScale = new Vector3(1, 0, 1);;
        
        _activeTiles.Add(gridPos, (iceTile, Data.durationTurns));
        _currentTilePos = gridPos;
        
        // 사운드
        SoundManager.Instance.PlaySFXClientRpc(SFXName.빙판길생성);
    }

    /// <summary>
    /// 빙판길 위치, 길이 업데이트
    /// </summary>
    /// <param name="stonePos"></param>
    public void UpdateIceScale(Vector2 stonePos)
    {
        if (!_activeTiles.TryGetValue(_currentTilePos, out var tile) || tile.Item1 == null) 
            return;
        
        Transform curTile = tile.Item1.transform;
        
        Vector2 direction = stonePos - (Vector2)curTile.position;
        float distance = Vector2.Distance(stonePos, curTile.position);
        
        // 회전값
        float angle = Vector2.SignedAngle(Vector2.up, direction);
        curTile.rotation = Quaternion.Euler(0, 0, angle);
        
        // 길이
        Vector3 scale = curTile.localScale;
        scale.y = distance;
        curTile.localScale = scale;
    }

    private void EndUpdateIceScale()
    {
        _movement.OnMovement -= RequestToUpdateIceTile;
    }

    /// <summary>
    /// 일정 턴 수 지나면 빙판길 삭제
    /// </summary>
    public void DestroySingleIceTile()
    {
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
    public void DestroyAllIceTiles()
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
