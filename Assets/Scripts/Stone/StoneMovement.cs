using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Debug = UnityEngine.Debug;
using Vector2 = UnityEngine.Vector2;

public class StoneMovement
{
    // 임시 데이터
    private readonly float _deceleration = 50.0f;   // 감속량(마찰력)
    private readonly float _bounceDamping = 0.9f;   // 충돌시 에너지 손실양
    public readonly float _collisionRadius = 0.45f; // 충돌 범위
    private Vector2 _currentVelocity;
    private bool _isMoving = false;
    private MapRuleExecutor _ruleExecutor;
    
    private readonly HashSet<Transform> _collidedThisFrame = new HashSet<Transform>(); // 중복 충돌 방지
    private StoneController _stoneController;
    private NetworkBehaviour _networkBehaviour;
    
    
    //---------------
    // Direction(Vector2) : 방향
    // Velocity(Vector2) : 방향 + 크기
    // Speed(float): 크기
    //---------------
    
    public StoneMovement(StoneController stoneController, NetworkBehaviour networkBehaviour)
    {
        _stoneController = stoneController;
        _networkBehaviour = networkBehaviour;
    }
    
    /// <summary>
    /// 외부 접근: 알을 튕기는 함수
    /// </summary>
    /// <param name="target">알(본인)</param>
    /// <param name="direction">날아가는 방향</param>
    /// <param name="speed">스피드</param>
    public void Shoot(Transform target, Vector2 direction, float speed)
    {
        // 서버에서만 호출되어야 함
        if (!_networkBehaviour.IsServer)
        {
            Debug.LogError("Shoot can only be called on Server!");
            return;
        }
        
        // 알 이동
        MoveAsync(target, direction, speed).Forget();
    }
    
    /// <summary>
    /// 알을 이동시키는 함수
    /// </summary>
    /// <param name="target">알(본인)</param>
    /// <param name="direction">날아가는 방향</param>
    /// <param name="speed">스피드</param>
    private async UniTask MoveAsync(Transform target, Vector2 direction, float speed)
    {
        if (_isMoving)
            return;
        
        _isMoving = true;

        float currentSpeed = speed;
        _currentVelocity = currentSpeed * direction.normalized;
        //_collidedThisFrame.Clear();
        
        NetworkBehaviour netBehaviour = target.GetComponent<NetworkBehaviour>();
        bool isServer = netBehaviour != null && netBehaviour.IsServer;
        
        while (target != null && currentSpeed > 0f)
        {
            // 충돌 체크
            if (IsOutOfOutline(target))
            {
                HandleOutOfMap(target, 1);
                break;
            }
            Transform collidedStone = CheckStoneCollision(target); 
        
            if (collidedStone != null && !_collidedThisFrame.Contains(collidedStone))
            {
                Debug.Log("충돌");
                _collidedThisFrame.Add(collidedStone);
            
                // 충돌 처리
                currentSpeed = CalculateSpeedAfterCollision(target, collidedStone, currentSpeed);
                HandleCollision(target, collidedStone);
            
                // 충돌 반영 시간 확보
                await UniTask.DelayFrame(2);
                _collidedThisFrame.Remove(collidedStone);
            }
            
            // 이동
            Vector2 pos = target.position;
            float moveStep = currentSpeed * Time.deltaTime;
            target.position = pos + _currentVelocity.normalized * moveStep;

            // 감속
            currentSpeed -= _deceleration * Time.deltaTime;
            _currentVelocity = _currentVelocity.normalized * currentSpeed;

            // 다음 프레임까지 대기
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        
        _isMoving = false;
        _currentVelocity = Vector2.zero;
        _collidedThisFrame.Clear();
        
        // 경기장 밖으로 나갔는지 확인
        if (!IsInsideMap(target))
        {
            HandleOutOfMap(target, 0);
        }
    }

    /// <summary>
    /// 다른 물체와 충돌했는지 감지하는 함수
    /// </summary>
    /// <param name="target">충돌하는 주체(본인)</param>
    /// <returns>충돌한 알</returns>
    private Transform CheckStoneCollision(Transform target)
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
    private bool IsInsideMap(Transform target)
    {
        int mapMask = LayerMask.GetMask("Map");

        var hits = Physics2D.OverlapCircle(
            target.position,
            _collisionRadius,
            mapMask
        );
        
        return hits != null;
    }
    
    /// <summary>
    /// Outline을 벗어났는지 판단
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    private bool IsOutOfOutline(Transform target)
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
    /// 충돌 이후 스피드 변화를 계산하는 함수
    /// </summary>
    /// <param name="target">알(본인)</param>
    /// <param name="otherStone">충돌한 알(상대)</param>
    /// <param name="speed">충돌 전 스피드</param>
    /// <returns>충돌 이후 스피드</returns>
    private float CalculateSpeedAfterCollision(Transform target, Transform otherStone, float speed)
    {
        // 충돌 방향
        Vector2 collisionNormal = ((Vector2)otherStone.position - (Vector2)target.position).normalized;
        
        float hitStrength = Mathf.Abs(Vector2.Dot(collisionNormal, _currentVelocity.normalized)); // 내적 이용
        float damping = Mathf.Lerp(0.1f, 0.9f, hitStrength);
        
        return speed * (1.0f - damping);
    }
    
    /// <summary>
    /// 충돌을 처리하는 함수
    /// </summary>
    /// <param name="target">알(본인)</param>
    /// <param name="otherStone">충돌한 알(상대)</param>
    private void HandleCollision(Transform target, Transform otherStone)
    {
        // 이펙트
        EffectManager.Instance.CollisionEffectClientRpc(((Vector2)otherStone.position + (Vector2)target.position) / 2);
        
        // 충돌 방향
        Vector2 collisionNormal = ((Vector2)otherStone.position - (Vector2)target.position).normalized;
        
        // 현재 알의 반사 벡터
        Vector2 reflectedVelocity = Vector2.Reflect(_currentVelocity, -collisionNormal);
        _currentVelocity = reflectedVelocity * _bounceDamping;
        
        // 상대방 알도 힘을 받고 움직임
        StoneMovement stoneMovement = otherStone.GetComponent<StoneController>().StoneMovement;
        stoneMovement?._collidedThisFrame.Add(target);
        
        if (stoneMovement != null && !stoneMovement._isMoving)
        {
            // 정지한 상태일 때
            float impactSpeed = _currentVelocity.magnitude * _bounceDamping;
            otherStone.GetComponent<StoneController>().TriggerShootFromCollision(collisionNormal, impactSpeed);
            stoneMovement.MoveAsync(otherStone, collisionNormal, impactSpeed).Forget();
        }
        else
        {
            // TODO: 상대도 움직이는 상태에서 충돌했을 때
        }
        
        // 겹침 방지
        float distance = Vector2.Distance(target.position, otherStone.position);
        float overlap = _collisionRadius * 2 - distance;
        
        if (overlap > 0)
        {
            Vector2 separation = -collisionNormal * ((overlap / 2) + 0.1f);
            target.position = (Vector2)target.position + separation;
            otherStone.position = (Vector2)otherStone.position - separation;
        }
    }

    /// <summary>
    /// 경기장 범위를 벗어났을 때 처리하는 함수
    /// </summary>
    private void HandleOutOfMap(Transform target, int outCase)
    {
        switch (outCase)
        {
            case 0:
                // 경기장 밖
                Debug.Log("경기장 밖");
                DestroyAsync(target).Forget();
                break;
            case 1:
                // Outline 밖
                Debug.Log("Outline 밖");
                OnDestroy(target);
                break;
        }
    }

    private async UniTask DestroyAsync(Transform target)
    {
        await UniTask.Delay(500);

        OnDestroy(target);
    }

    private void OnDestroy(Transform target)
    {
        // 서버에서 승패/디스폰/스킬 분배까지 처리
        if(_networkBehaviour.IsServer && _ruleExecutor != null)
        {
            var stone = target.GetComponent<Stone>();
            if(stone != null)
            {
                _ruleExecutor.OnStoneOut(stone);
            }
            else
            {
                Debug.LogError("[StoneMovement] target에 Stone 컴포넌트 없음");
            }
        }
        
        EffectManager.Instance.DestroyEffectClientRpc(target.transform.position);
        _stoneController.Stone.SetAnimatorTrigger(Stone.HashDead);
    }

    /// <summary>
    /// MapRuleExecutor를 Set으로 받게 함
    /// </summary>
    public void SetRuleExecutor(MapRuleExecutor executor)
    {
        _ruleExecutor = executor;
    }
    
}
