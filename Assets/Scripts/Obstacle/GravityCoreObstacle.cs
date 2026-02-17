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

            // 중력 방향: 돌 -> 코어
            Vector2 stonePos = sc.transform.position;
            Vector2 pull = ((Vector2)transform.position - stonePos);
            float dist = Mathf.Max(pull.magnitude, 0.1f);
            pull /= dist;

            // 방향을 서서히 꺾음
            // gravityStrength = 방향이 얼마나 강하게 꺾이는지
            Vector2 newDir = (mv.Direction + pull * (cfg.gravityStrength * tickInterval)).normalized;
            mv.Direction = newDir;

            // 살짝 가속도 가능
            // mv.Speed += cfg.gravityStrength * 0.03f * tickInterval;
        }
    }
}
