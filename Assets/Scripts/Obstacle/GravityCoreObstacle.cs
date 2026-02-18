using UnityEngine;
using Unity.Netcode;

/// <summary>
/// GravityCore의 효과를 서버에서만 적용
/// 1. 이벤트 대신 일정 틱마다 OverlapCircle로 Stone을 찾고
/// 2. StoneMovement.Direction을 중력 방향으로 끌어당김
/// </summary>
public class GravityCoreObstacle : MonoBehaviour
{
    [SerializeField] private Obstacle obstacle;

    // 틱 간격 (촘촘하면 비용 증가, 느리면 뚝뚝 끊김)
    [SerializeField] private float tickInterval = 0.02f; // 0.02=50fps, 0.05=20fps
    private float _t;

    // NonAlloc 결과 버퍼 (가비지 줄이는 용)
    private readonly Collider2D[] _hits = new Collider2D[64];

    // 코어 근처에서 통과/튀는 걸 막는 반경
    [SerializeField] private float captureRadius = 0.25f;

    // 코어 안에서 추가로 깎는 감속(드래그) 계수
    [SerializeField] private float dragMultiplier = 0.8f;

    // 코어 근처에서 속도 상한(0이면 완전 정지)
    [SerializeField] private float capturedMaxSpeed = 0.5f;

    private void Awake()
    {
        if (obstacle == null) obstacle = GetComponent<Obstacle>();
    }

    private void Update()
    {
        // 서버에서만 처리
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        if (obstacle == null || obstacle.Config == null || obstacle.TriggerArea == null) return;

        _t += Time.deltaTime;
        if (_t < tickInterval) return;
        _t = 0f;

        var cfg = obstacle.Config;
        var area = obstacle.TriggerArea;

        // TriggerArea 중심/반경 계산 (offset, scale 고려)
        Vector2 center = (Vector2)area.transform.TransformPoint(area.offset);
        float r = area.radius * Mathf.Max(area.transform.lossyScale.x, area.transform.lossyScale.y);

        // 반경 안에 있는 콜라이더 탐색 (Stone 태그로 필터)
        int n = Physics2D.OverlapCircleNonAlloc(center, r, _hits);
        for (int i = 0; i < n; i++)
        {
            var col = _hits[i];
            if (col == null) continue;
            if (!col.CompareTag("Stone")) continue;

            // StoneController를 찾아 StoneMovement 접근
            var sc = col.GetComponent<StoneController>() ?? col.GetComponentInParent<StoneController>();
            if (sc == null) continue;

            var mv = sc.StoneMovement;
            if (mv == null) continue;

            // =========================
            // 빨려들기 구현 (방향 수렴 + 감속)
            // =========================


            // 중력 방향: 돌 -> 코어
            Vector2 stonePos = sc.transform.position;
            Vector2 toCore = (Vector2)transform.position - stonePos;

            float dist = Mathf.Max(toCore.magnitude, 0.001f);
            Vector2 pullDir = toCore / dist;

            // 바깥(0) → 중심(1)으로 갈수록 강해지는 계수
            float normalized = Mathf.Clamp01(1f - (dist / r));

            // 방향을 강하게 중심으로 수렴
            float turn = cfg.gravityStrength * (0.2f + 0.8f*normalized);
            mv.Direction = Vector2.Lerp(mv.Direction, pullDir, turn * tickInterval).normalized;

            // 코어 안에서는 속도를 줄여서(드래그) 통과/튕김 방지
            float drag = (cfg.gravityStrength * dragMultiplier) * Mathf.Pow(normalized, 2f);
            mv.Speed = Mathf.Max(0f, mv.Speed - drag * tickInterval);

            // 코어의 중심 근처에 오면 더 이상 통과 못 하게 붙도록 함(캡처)
            if (dist <= captureRadius)
            {
                mv.Direction = pullDir;
                mv.Speed = Mathf.Min(mv.Speed, capturedMaxSpeed); // 0이면 완전 흡수됨
            }
        }
    }
}
