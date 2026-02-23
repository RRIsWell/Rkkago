using Unity.Netcode;
using UnityEngine;

public class Teleportation : SkillBase
{
    private Stone stone;
    private StoneController _stoneController;
    private NetworkObject _networkObject;
    
    private bool _isFirst = true;
    
    
    public Teleportation(Stone stone, SkillSO data) : base(stone, data)
    {
        this.stone = stone;
        _stoneController = stone.GetComponent<StoneController>();
        _networkObject = _stoneController.NetworkObject;
    }

    public override void Activate()
    {
        RequestToTeleportation();
        
    }

    public override void Deactivate()
    {
        _isFirst = true;
        base.Deactivate();
    }

    /// <summary>
    /// 네트워크 호출을 통한 텔포 함수 실행
    /// </summary>
    private void RequestToTeleportation()
    {
        // EffectManager를 통해 네트워크 호출(분신 생성 후 날림)
        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.DoTeleportation(
                new NetworkObjectReference(_networkObject)
            );
        }
    }

    public void DoingTeleportation()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        
        // 사운드
        SoundManager.Instance.PlaySFXClientRpc(SFXName.텔포);
        
        var ruleExecutor = GameObject.FindObjectOfType<MapRuleExecutor>();
        if (ruleExecutor == null || ruleExecutor.Config == null) return;

        MapConfig config = ruleExecutor.Config;
        Vector3 targetPos;
    
        // 알의 충돌 반경
        float stoneRadius = 0.5f; 
    
        // [중요] 레이어 마스크 설정 (Stone과 Obstacle 레이어만 검사)
        int layerMask = LayerMask.GetMask("Stone", "Obstacle"); 
    
        int maxAttempts = 10;
        bool isValidPosition = false;

        do
        {
            targetPos = (Vector3)config.center;
            float margin = 0.9f;

            if (config.isCircleMap)
            {
                Vector2 randomPoint = Random.insideUnitCircle * (config.mapRadius * margin);
                targetPos += new Vector3(randomPoint.x, randomPoint.y, 0);
            }
            else
            {
                float rx = Random.Range(-config.mapHalfSize.x * margin, config.mapHalfSize.x * margin);
                float ry = Random.Range(-config.mapHalfSize.y * margin, config.mapHalfSize.y * margin);
                targetPos += new Vector3(rx, ry, 0);
            }
            
            Collider2D hit = Physics2D.OverlapCircle(targetPos, stoneRadius, layerMask);

            if (hit == null)
            {
                isValidPosition = true;
            }
            else
            {
                maxAttempts--;
            }

        } while (!isValidPosition && maxAttempts > 0);

        // 위치 적용
        stone.transform.position = targetPos;
        
        EffectManager.Instance.TeleportPositionClientRpc(new NetworkObjectReference(_networkObject), targetPos);
    }
}
