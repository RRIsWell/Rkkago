using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Obstacle : MonoBehaviour
{
    [SerializeField] private ObstacleConfig config;
    private SpriteRenderer sr;

    [Header("Trigger Area")]
    [SerializeField] private CircleCollider2D triggerArea;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        if (triggerArea == null)
            triggerArea = GetComponentInChildren<CircleCollider2D>(true);
    }

    public void Init(ObstacleConfig cfg)
    {
        config = cfg;

        sr.sprite = cfg.sprite;
        transform.localScale = new Vector3(cfg.size.x, cfg.size.y, 1f);

        AttachAndInitEffect(cfg);
    }

    private void AttachAndInitEffect(ObstacleConfig cfg)
    {
        switch (cfg.type)
        {
            case ObstacleType.GravityCore:
                var g = GetOrAdd<GravityCoreObstacle>();
                g.Init(cfg, triggerArea);
                break;

            case ObstacleType.BounceCore:
                var b = GetOrAdd<BounceCoreObstacle>();
                b.Init(cfg, triggerArea);
                break;
        }
    }

    private T GetOrAdd<T>() where T : Component
    {
        if (TryGetComponent<T>(out var c))
            return c;

        return gameObject.AddComponent<T>();
    }
}
