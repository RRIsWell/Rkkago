using System.Collections.Generic;
using UnityEngine;
using System.Data.Common;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

// 환경값 세팅용
[System.Serializable]
public class GameObjectRow
{
    public List<Transform> spawnPoints;
}

[CreateAssetMenu(menuName = "Map/MapConfig")]
public class MapConfig : ScriptableObject
{
    [Header("기본 규칙")]
    public int stonesPerPlayer = 5;
    public bool allowHalfOut = true;

    [Header("표면")]
    public float friction = 0.8f;
    public bool slippery = false;

    [Header("장애물")]
    public bool useObstacle;
    public int obstacleCount = 5;
    public float obstacleMargin = 1.5f; // 벽과 겹쳐서 스폰 방지

    [Header("패배 판정")]
    public int loseStoneCount = 5;
    
    [Header("맵 BGM")]
    public BGMName bgmName;
    
    [Header("알 스폰 지점")]
    public List<GameObjectRow> stoneSpawnPoints = new List<GameObjectRow>();
}