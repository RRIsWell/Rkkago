using System.Collections.Generic;
using UnityEngine;


// 환경값 세팅용
[System.Serializable]
public class GameObjectRow
{
    public List<Transform> spawnPoints;
}

public enum MapRuleType
{
    Default, // Map1 (기본 맵) 룰
    Billiard, // Map2 (당구 맵) 룰
    Culling // Map3 (컬링 맵) 룰
}

[CreateAssetMenu(menuName = "Map/MapConfig")]
public class MapConfig : ScriptableObject
{
    // 기본 맵 룰을 디폴트로 설정
    public MapRuleType ruleType = MapRuleType.Default;

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

    /// <summary>
    /// Map2 (Billiard)
    /// </summary>
    [Header("Map2 - Billiard")]
    public float damage = 0.5f;
    
    
    /// <summary>
    /// Map3 (Culling)
    /// </summary>
    [Header("Map3 - Culling")]
    public int maxTurnPairs = 15; // 서로 한 번씩 15턴
    public float outMargin = 0.05f; // 화면 밖 판정
    public Vector2 center = Vector2.zero; // 중앙점
    
    
    /// <summary>
    /// 스킬 및 텔레포트용 범위 설정
    /// </summary>
    [Header("스킬/텔레포트 범위")]
    public bool isCircleMap = false; // 원형 맵 여부 체크
    
    [Tooltip("사각 맵일 때 사용: 중심점에서 가로/세로 이동 가능 거리")]
    public Vector2 mapHalfSize = new Vector2(3.5f, 3.5f); 
    
    [Tooltip("원형 맵일 때 사용: 중심점으로부터의 최대 반지름")]
    public float mapRadius = 3.5f;
}