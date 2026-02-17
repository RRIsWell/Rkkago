using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;

/// <summary>
/// BounceCore의 효과를 서버에서만 적용
/// 1. 보너스 원(TriggerArea) 안에 들어오면 1번 speed *= bonusMultiplier
/// 2. 본체와 겹치면 반사 + speed *= bounceMultiplier
/// 3. Rigidbody2D 없이 : 이벤트 사용x
/// 4. 대신 OverlapCirlce / ClosestPoint로 판정
/// </summary>
public class BounceCoreObstacle : MonoBehaviour
{
    [SerializeField] private Obstacle obstacle;
    [SerializeField] private Collider2D bodyCollider; // 루트 본체 Collider2D
    [SerializeField] private float tickInterval = 0.02f;
    private float _t;

    // NonAlloc 결과 버퍼
    private readonly Collider2D[] _hits = new Collider2D[64];

    // 보너스 원 안에 들어올 때 1번을 위해
    // 이전 틱에 영역 안에 있던 StoneController 기억함
    private readonly HashSet<StoneController> _insideBonus = new();

    // FindGameObjectsWithTag 비용 최적화
    [SerializeField] private float stoneCacheInterval = 0.5f;
    private float _stoneCacheT;
    private GameObject[] _stoneCache = new GameObject[0];

    private void Awake()
    {
        if (obstacle == null) obstacle = GetComponent<Obstacle>();

        // 루트에 Collider2D 하나를 기본으로 잡음
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        if (obstacle == null || obstacle.Config == null || obstacle.TriggerArea == null) return;
        
        // BounceCore는 본체 콜라이더가 필요
        if (bodyCollider == null)
        {
            Debug.LogError($"{name}: BounceCore는 루트에 Collider2D(본체)가 필요함");
            return;
        }

        _t += Time.deltaTime;
        if (_t < tickInterval) return;
        _t = 0f;

        ApplyBonusOnceOnEnter();
        ApplyBodyBounce();
    }

    /// <summary>
    /// TriggerArea(보너스 원) 안으로 들어오는 순간 1번만 적용
    /// Enter로 구현(매 프레임 곱하면 속도가 폭주)
    /// </summary>
    private void ApplyBonusOnceOnEnter()
    {
        var cfg = obstacle.Config;
        var area = obstacle.TriggerArea;

        Vector2 center = (Vector2)area.transform.TransformPoint(area.offset);
        float r = area.radius * Mathf.Max(area.transform.lossyScale.x, area.transform.lossyScale.y);

        // 이번 틱에 안에 들어있는 알 수집
        var nowInside = new HashSet<StoneController>();

        int n = Physics2D.OverlapCircleNonAlloc(center, r, _hits);
        for (int i = 0; i < n; i++)
        {
            var col = _hits[i];
            if (col == null) continue;
            if (!col.CompareTag("Stone")) continue;

            var sc = col.GetComponent<StoneController>() ?? col.GetComponentInParent<StoneController>();
            if (sc == null) continue;

            nowInside.Add(sc);

            // Enter 판정: 전에는 없었는데 지금은 있음
            if (!_insideBonus.Contains(sc))
            {
                var mv = sc.StoneMovement;
                if (mv != null)
                {
                    mv.Speed *= cfg.bonusMultiplier;
                }
            }
        }

        // inside 상태 갱신
        _insideBonus.Clear();
        foreach (var sc in nowInside) _insideBonus.Add(sc);
    }

    /// <summary>
    /// 본체 콜라이더와의 겹침을 검사해서 반사함
    /// 1. Stone을 원(CollisionRadius)으로 보고
    /// 2. bodyCollider.ClosestPoint로 최소 거리 계산 후 겹침이면 반사 처리함
    /// </summary>
    private void ApplyBodyBounce()
    {
        // 돌 캐시 갱신 (Find 비용 줄이기)
        _stoneCacheT += tickInterval;
        if (_stoneCacheT >= stoneCacheInterval)
        {
            _stoneCacheT = 0f;
            _stoneCache = GameObject.FindGameObjectsWithTag("Stone");
        }

        var cfg = obstacle.Config;

        foreach (var go in _stoneCache)
        {
            if (go == null) continue;

            var sc = go.GetComponent<StoneController>() ?? go.GetComponentInParent<StoneController>();
            if (sc == null) continue;

            var mv = sc.StoneMovement;
            if (mv == null) continue;

            Vector2 p = sc.transform.position;

            // 돌 반지름
            float stoneR = mv.CollisionRadius;

            // 혹시 radius가 아직 0이면 StoneAttribute로 보정 (가능하면)
            if (stoneR <= 0.0001f)
            {
                var attr = sc.GetComponent<StoneAttribute>();
                if (attr != null)
                {
                    // Scale이 0이거나 이상하면 1로 보정
                    float s = Mathf.Max(attr.Scale, 1f);
                    stoneR = s * 0.45f;
                }
            }
            if (stoneR <= 0.0001f) continue;

            // 본체 콜라이더와 겹쳤는지 검사
            // 장애물 콜라이더에서 돌 중심까지의 가장 가까운 점
            Vector2 closest = bodyCollider.ClosestPoint(p);
            Vector2 diff = p - closest;

            // 겹침 판정: 돌 중심과 closest의 거리가 반지름 이하
            if (diff.sqrMagnitude <= stoneR * stoneR)
            {
                // 노멀(장애물 -> 돌 방향)
                Vector2 normal = diff.sqrMagnitude < 1e-6f
                    ? (p - (Vector2)transform.position).normalized
                    : diff.normalized;

                // StoneMovement.ReflectStone은 private라 직접 계산
                Vector2 reflected = Vector2.Reflect(mv.Direction, normal).normalized;
                mv.Direction = reflected;

                // 튕길 때 속도 배수
                mv.Speed *= cfg.bounceMultiplier;

                // 겹침 해소: 밀어내기
                float d = Mathf.Sqrt(Mathf.Max(diff.sqrMagnitude, 1e-6f));
                float push = (stoneR - d) + 0.01f;
                sc.transform.position = p + normal * push;
            }
        }
    }
}
