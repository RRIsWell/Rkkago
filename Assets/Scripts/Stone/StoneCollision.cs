using UnityEngine;

/// <summary>
/// 알 충돌 체크하는 클래스
/// </summary>
public class StoneCollision
{
    public readonly float _collisionRadius = 0.45f; // 충돌 범위
    private Vector3[] _mapCorners;
    
    /// <summary>
    /// 다른 물체와 충돌했는지 감지하는 함수
    /// </summary>
    /// <param name="target">충돌하는 주체(본인)</param>
    /// <returns>충돌한 알</returns>
    public Transform CheckStoneCollision(Transform target)
    {
        int stoneMask = LayerMask.GetMask("Stone");
        
        var hits = Physics2D.OverlapCircleAll(
            target.position, 
            _collisionRadius, 
            stoneMask
        );
        
        foreach (var hit in hits)
        {
            if (hit.transform == target) // 본인인 경우
                continue;
            
            return hit.transform;
        }
        return null;
    }
    
    /// <summary>
    /// 경기장 안에 있는지 판단
    /// </summary>
    /// <param name="target">알</param>
    /// <returns></returns>
    public bool IsInsideMap(Transform target)
    {
        int mapMask = LayerMask.GetMask("Map", "CushionMap");

        var hits = Physics2D.OverlapCircle(
            target.position,
            _collisionRadius,
            mapMask
        );

        return hits != null;
    }

    /// <summary>
    /// 당구맵일 때 가장자리에 충돌했는지 판단
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public Vector2 IsReflectCushionMap(Transform target)
    {
        // null 오류 방지
        if (target == null) return Vector2.zero;
        
        int mapMask = LayerMask.GetMask("CushionMap");
        
        var hits = Physics2D.OverlapCircle(
            target.position,
            _collisionRadius,
            mapMask
        );
        
        if(hits == null) return Vector2.zero;
        
        if (_mapCorners == null)
        {
            _mapCorners = new Vector3[4];
            hits.GetComponent<RectTransform>()?.GetWorldCorners(_mapCorners);
        }
        
        float minX = _mapCorners[0].x;
        float maxX = _mapCorners[2].x;
        float minY = _mapCorners[0].y;
        float maxY = _mapCorners[1].y;
        
        Vector2 pos = target.position;
        Vector2 normal = Vector2.zero;

        if (pos.x - _collisionRadius < minX)
        {
            normal = Vector2.right;   // 왼쪽 벽 → 오른쪽을 향한 법선
            pos.x = minX + _collisionRadius;
        }
        else if (pos.x + _collisionRadius > maxX)
        {
            normal = Vector2.left;    // 오른쪽 벽 → 왼쪽을 향한 법선
            pos.x = maxX - _collisionRadius;
        }
        
        if (pos.y - _collisionRadius < minY)
        {
            normal = Vector2.up;      // 아래쪽 벽 → 위
            pos.y = minY + _collisionRadius;
        }
        else if (pos.y + _collisionRadius > maxY)
        {
            normal = Vector2.down;    // 위쪽 벽 → 아래
            pos.y = maxY - _collisionRadius;
        }

        return normal;
    }
    
    /// <summary>
    /// Outline을 벗어났는지 판단
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool IsOutOfOutline(Transform target)
    {
        int outlineMask = LayerMask.GetMask("Outline");

        var hits = Physics2D.OverlapCircle(
            target.position,
            _collisionRadius,
            outlineMask
        );
        
        return hits != null;
    }
    
    /// <summary>
    /// 빙판길 확인
    /// </summary>
    public bool IsOnIcePath(Transform target)
    {
        int mask = LayerMask.GetMask("Ice");
        
        var hits = Physics2D.OverlapCircle(
            target.position,
            _collisionRadius * 2.0f,
            mask
        );
        return hits != null;
    }
}
