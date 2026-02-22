using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Obstacle))]
public class BombCoreObstacle : NetworkBehaviour
{
    [Header("발화점")]
    [SerializeField] private int fuseTurns = 4;

    [Header("폭발")]
    [SerializeField] private float radius = 2.5f;

    [Header("탐지")]
    [SerializeField] private string stoneTag = "Stone";   // 기존 장애물들과 통일
    [SerializeField] private LayerMask overlapMask = ~0;

    /*[Header("비주얼")]
    [SerializeField] private Transform radiusVisual;*/ // 원형 표시용 자식(스프라이트)

    private int turnsLeft;
    private bool exploded;

    public override void OnNetworkSpawn()
    {
        //ApplyRadiusVisual();

        if (!IsServer) return;

        turnsLeft = fuseTurns;
        TurnManager.OnServerTurnAdvanced += HandleTurnAdvanced_Server;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
            TurnManager.OnServerTurnAdvanced -= HandleTurnAdvanced_Server;

        base.OnNetworkDespawn();
    }

    private void HandleTurnAdvanced_Server()
    {
        if (!IsServer) return;
        if (exploded) return;
        if (!IsSpawned) return;

        turnsLeft--;

        if (turnsLeft <= 0)
        {
            Explode_Server();
        }
    }

    private void Explode_Server()
    {
        if (!IsServer) return;
        if (exploded) return;
        exploded = true;

        
        Vector2 center = transform.position;

        // 전체 Overlap -> Tag로 필터
        var hits = Physics2D.OverlapCircleAll(center, radius, overlapMask);

        foreach (var col in hits)
        {
            if (col == null) continue;
            
            // collider가 자식에 있을 수 있으니 루트 쪽으로 올라가서 판단
            Transform tr = col.transform;

            bool isStoneTagged =
                tr.CompareTag(stoneTag) ||
                (tr.parent != null && tr.parent.CompareTag(stoneTag)) ||
                (tr.root != null && tr.root.CompareTag(stoneTag));

                if (!isStoneTagged)
                    continue;

            // Stone 컴포넌트 찾기
            var stone = col.GetComponentInParent<Stone>();
            if (stone == null) continue;

            stone.SetAnimatorTriggerClientRpc(Stone.HashDead);
            
            // 사운드
            SoundManager.Instance.PlaySFXClientRpc(SFXName.폭탄);
        }

        // 폭탄 터지는 애니메이션 실행
        OnTriggerDestroyAnimationClientRpc();
        
        /*// 폭탄 자신 제거
        var no = GetComponent<NetworkObject>();
        if (no != null && no.IsSpawned) no.Despawn(true);
        else Destroy(gameObject);*/
    }

    [ClientRpc]
    private void OnTriggerDestroyAnimationClientRpc()
    {
        GetComponent<Animator>().SetTrigger("Destroy");
    }
    
    // 애니메이션 이벤트 함수 (Explode 재생 끝난 뒤 실행)
    public void OnDestroyBomb()
    {
        // 폭탄 자신 제거
        if (IsServer)
        {
            var no = GetComponent<NetworkObject>();
            if (no != null && no.IsSpawned) no.Despawn(true);
            else Destroy(gameObject);
        }
    }

    /*private void ApplyRadiusVisual()
    {
        if (radiusVisual == null) return;

        float diameter = radius * 2f;
        radiusVisual.localScale = new Vector3(diameter, diameter, 1f);
        radiusVisual.gameObject.SetActive(true);
    }*/

    private void OnValidate()
    {
        //ApplyRadiusVisual();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
