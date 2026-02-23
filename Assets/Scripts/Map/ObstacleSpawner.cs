using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject[] obstaclePrefabs;

    [Header("Spawn Area")]
    [SerializeField] private Collider2D spawnArea;

    [Header("Options")]
    [SerializeField] private bool spawnOnce = true;          // Init이 여러 번 불려도 1회만 스폰
    [SerializeField] private bool clearBeforeSpawn = true;   // 다시 스폰할 때 기존 제거
    [SerializeField] private int maxTriesPerObstacle = 30;   // 위치 찾기 시도 횟수

    [Header("Spacing")]
    [SerializeField] private bool useMinDistance = false;    // 장애물끼리 최소 간격 적용 여부
    [SerializeField] private float minDistance = 1.0f;       // 장애물 간 최소 거리

    private bool _spawned = false;                           // 중복 스폰 방지
    private readonly List<NetworkObject> _spawnedNetObjects = new(); // 스폰한 장애물 추적

    /// <summary>
    /// MapConfig 기반으로 장애물 스폰 시작.
    /// 보통 Map 시작 시(서버에서) 1번 호출하는 형태.
    /// </summary>
    public void Init(MapConfig config)
    {
        // 서버에서만 스폰
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;
        
        if (config == null)
        {
            Debug.LogError($"{name}: MapConfig가 null이라 장애물을 스폰할 수 없음");
            return;
        }

        if (!config.useObstacle) return;

        if (spawnOnce && _spawned) return;

        if (clearBeforeSpawn)
            ClearSpawned(); // 기존 스폰 제거

        SpawnInternal(config);
        _spawned = true;
    }

    /// <summary>
    /// 실제 스폰 로직
    /// </summary>
    private void SpawnInternal(MapConfig config)
    {
        // spawnArea/prefabs 검증
        if (spawnArea == null)
        {
            Debug.LogError($"{name}: spawnArea(BoxCollider2D)가 연결되지 않음");
            return;
        }

        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
        {
            Debug.LogError($"{name}: obstaclePrefabs가 비어있음(0). 프리팹 배열에 GravityCore/BounceCore 넣어야 함");
            return;
        }

        int count = Mathf.Max(0, config.obstacleCount);
        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            // 위치 찾기 
            if (!TryGetValidPosition(config.obstacleMargin, out Vector2 pos))
            {
                Debug.LogWarning($"{name}: 유효한 스폰 위치를 못 찾음 (i={i})");
                continue;
            }

            
            // prefab 변수 먼저 선언
            GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];


            // Instantiate 후 NetworkObject.Spawn()
            GameObject go = Instantiate(prefab, pos, Quaternion.identity);
            var netObj = go.GetComponent<NetworkObject>();
            if (netObj == null)
            {
                Debug.LogError($"{prefab.name} 프리팹에 NetworkObject가 없음");
                Destroy(go);
                continue;
            }

            netObj.Spawn(true); // 모든 클라에 스폰 동기화
            _spawnedNetObjects.Add(netObj);
        }
    }

    /// <summary>
    /// 스폰했던 장애물들을 모두 제거.
    /// 턴 종료마다 재배치/재스폰 같은 이벤트를 만들 때 사용
    /// </summary>
    public void ClearSpawned()
    {
        // 서버에서만 처리
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;
        
        for (int i = _spawnedNetObjects.Count - 1; i >= 0; i--)
        {
            var no = _spawnedNetObjects[i];
            if (no == null) continue;

            if (no.IsSpawned)
                no.Despawn(true); // 네트워크로 제거(클라도 같이 사라짐)
            else
                Destroy(no.gameObject);
        }

        _spawnedNetObjects.Clear();
        _spawned = false;
    }

    /// <summary>
    /// 스폰 위치를 찾는 함수.
    /// 1. margin 반영
    /// 2. 장애물끼리 최소 거리 체크
    /// </summary>
    private bool TryGetValidPosition(float margin, out Vector2 pos)
    {
        Bounds b = spawnArea.bounds;

        for (int t = 0; t < maxTriesPerObstacle; t++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(b.min.x + margin, b.max.x - margin),
                Random.Range(b.min.y + margin, b.max.y - margin)
            );

            // 콜라이더 내부인지 확인
            if (!spawnArea.OverlapPoint(candidate))
                continue;

            // 최소 거리 옵션
            if (useMinDistance)
            {
                bool ok = true;
                for (int i = 0; i < _spawnedNetObjects.Count; i++)
                {
                    var o = _spawnedNetObjects[i];
                    if (o == null) continue;

                    if (Vector2.Distance(candidate, o.transform.position) < minDistance)
                    {
                        ok = false;
                        break;
                    }
                }
                if (!ok) continue;
            }

            pos = candidate;
            return true;
        }

        pos = default;
        return false;
    }
}
