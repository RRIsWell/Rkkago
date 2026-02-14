using UnityEngine;

public class BounceCoreObstacle : MonoBehaviour
{
    private ObstacleConfig cfg;
    private TriggerRelay relay;

    public void Init(ObstacleConfig config, CircleCollider2D area)
    {
        cfg = config;

        if (area == null)
        {
            Debug.LogError("TriggerArea(CircleCollider2D)가 없음. 프리팹에 TriggerArea를 만들고 연결해야 함.");
            return;
        }

        area.isTrigger = true;
        area.radius = cfg.bonusRadius;

        relay = area.GetComponent<TriggerRelay>();
        if (relay == null) relay = area.gameObject.AddComponent<TriggerRelay>();

        relay.OnEnter -= HandleEnter;
        relay.OnEnter += HandleEnter;
    }

    private void OnDestroy()
    {
        if (relay != null) relay.OnEnter -= HandleEnter;
    }

    // 본체 충돌: 튕김 + 배수
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Stone")) return;

        var rb = collision.rigidbody;
        if (rb == null) return;

        Vector2 v = rb.linearVelocity;
        Vector2 n = collision.GetContact(0).normal;
        Vector2 reflected = Vector2.Reflect(v, n);

        rb.linearVelocity = reflected * cfg.bounceMultiplier;
    }

    // 보너스 원: 들어오면 1회 가속
    private void HandleEnter(Collider2D other)
    {
        if (!other.CompareTag("Stone")) return;

        var rb = other.attachedRigidbody;
        if (rb == null) return;

        rb.linearVelocity *= cfg.bonusMultiplier;
    }
}
