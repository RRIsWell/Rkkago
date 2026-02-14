using UnityEngine;

public class GravityCoreObstacle : MonoBehaviour
{
    private ObstacleConfig cfg;
    private TriggerRelay relay;

    public void Init(ObstacleConfig config, CircleCollider2D area)
    {
        cfg = config;

        if (area == null)
        {
            Debug.LogError("TriggerArea(CircleCollider2D)가 없습니다. 프리팹에 TriggerArea를 만들고 연결하세요.");
            return;
        }

        area.isTrigger = true;
        area.radius = cfg.gravityRadius;

        relay = area.GetComponent<TriggerRelay>();
        if (relay == null) relay = area.gameObject.AddComponent<TriggerRelay>();

        relay.OnStay -= HandleStay;
        relay.OnStay += HandleStay;
    }

    private void OnDestroy()
    {
        if (relay != null) relay.OnStay -= HandleStay;
    }

    private void HandleStay(Collider2D other)
    {
        if (!other.CompareTag("Stone")) return;

        var rb = other.attachedRigidbody;
        if (rb == null) return;

        Vector2 dir = (Vector2)transform.position - rb.position;
        float dist = Mathf.Max(dir.magnitude, 0.1f);
        dir /= dist;

        rb.AddForce(dir * cfg.gravityStrength, ForceMode2D.Force);
    }
}
