using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class IceAge : SkillBase
{
    private int _durationTurns;
    private IceAgeSO _so;
    private GameObject _icePrefab;
    private float _tileSpacing;
    private Dictionary<Vector2Int, GameObject> _activeTiles = new Dictionary<Vector2Int, GameObject>();

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
        _movement.EnableRecordPath();
        _movement.Collision.SetIceAgeSkil(this);
    }

    public override void Deactivate()
    {
        // 생성한 빙판길 삭제
        _activeTiles.Clear();
        _movement.OnMovement -= OnMovementHandler;
        _movement.DisableRecordPath();
    }
    
    private void OnMovementHandler(Vector2 position)
    {
        // MapEffectManager를 통해 네트워크 호출(빙판길 생성)
        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.CreateIceTileServerRpc(
                position, 
                new NetworkObjectReference(_networkObject)
            );
        }
    }
    
    public void CreateSingleIceTile(Vector2 position)
    {
        Vector2Int gridPos = WorldToGrid(position);
        
        // 이미 존재하는 타일이면 return
        if (_activeTiles.TryGetValue(gridPos, out GameObject existingTile))
        {
            return;
        }
        
        // 새 타일 생성
        GameObject iceTile = Object.Instantiate(_icePrefab, position,  Quaternion.identity, GameObject.FindWithTag("MainUI").transform);
        _activeTiles.Add(gridPos, iceTile);
    }
    
    public bool IsOnIce(Vector2 position)
    {
        Vector2Int gridPos = WorldToGrid(position);
        return _activeTiles.ContainsKey(gridPos);
    }
    
    private Vector2Int WorldToGrid(Vector2 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / _tileSpacing),
            Mathf.RoundToInt(worldPos.y / _tileSpacing)
        );
    }
}
