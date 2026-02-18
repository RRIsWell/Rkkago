using UnityEngine;


/// <summary>
/// 장애물의 기본 설정(스프라이트, 스케일, 트리거 반경) 담당
/// 실제 효과(중력/바운스/폭탄)은 _Obstacle 이 담당
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Obstacle : MonoBehaviour
{
    [SerializeField] private ObstacleConfig config;
    [SerializeField] private CircleCollider2D triggerArea; // 자식 TriggerArea

    private SpriteRenderer sr;

    public ObstacleConfig Config => config;
    public CircleCollider2D TriggerArea => triggerArea;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        // TriggerArea가 슬롯에 없으면 자식에서 찾음
        if (triggerArea == null)
            triggerArea = GetComponentInChildren<CircleCollider2D>(true);

        // config 기반으로 비주얼/반경 적용
        ApplyFromConfig();
    }


    /// <summary>
    /// 런타임에 config 바꿔야 할 경우 사용
    /// </summary>
    /// <param name="cfg"></param>
    public void SetConfig(ObstacleConfig cfg)
    {
        config = cfg;
        ApplyFromConfig();
    }

    /// <summary>
    /// config 값을 기반으로
    /// 1. 스프라이트 지정
    /// 2. 스케일 적용
    /// 3. TriggerArea의 반경 적용
    /// </summary>

    public void ApplyFromConfig()
    {
        if (config == null || sr == null) return;

        // Visual
        sr.sprite = config.sprite;
        transform.localScale = new Vector3(config.size.x, config.size.y, 1f);

        // Trigger Area 
        if (triggerArea != null)
        {
            triggerArea.isTrigger = true;

            switch (config.type)
            {
                case ObstacleType.GravityCore:
                    triggerArea.radius = config.gravityRadius;
                    break;
                case ObstacleType.BounceCore:
                    triggerArea.radius = config.bonusRadius;
                    break;
                case ObstacleType.Bomb:
                    // Bomb도 범위 쓸 때 여기서 세팅
                    // triggerArea.radius = config.explodeRadius;
                    break;
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 에디터에서 값 바꾸면 즉시 반영
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (triggerArea == null)
            triggerArea = GetComponentInChildren<CircleCollider2D>(true);

        ApplyFromConfig();
    }
#endif
}
