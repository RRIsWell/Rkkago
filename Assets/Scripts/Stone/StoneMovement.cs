using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Debug = UnityEngine.Debug;
using Vector2 = UnityEngine.Vector2;

public class StoneMovement
{
    // 임시 데이터
    private readonly float _bounceDamping = 0.9f;   // 충돌시 에너지 손실양
    public float CollisionRadius => _stoneCollision.CollisionRadius;
    private bool _isMoving = false;
    
    // 컴포넌트
    private readonly StoneController _stoneController;
    private readonly NetworkBehaviour _networkBehaviour;
    private readonly StoneCollision _stoneCollision;
    
    private readonly HashSet<Transform> _collidedThisFrame = new HashSet<Transform>(); // 중복 충돌 방지
    
    /// <summary>
    /// 움직일 때 이벤트 -> 외부에서 필요시 구독
    /// </summary>
    public event Action<Vector2> OnMovementStarted;    // 움직임 시작 시 실행
    public event Action<Vector2> OnMovement;    // 움직이는 매 프레임 실행
    public event Action OnMovementEnded;        // 움직임 끝나고 실행
    public event Action OnCollisionEnter;       // 다른 알에 의해 충돌 당했을 때
    
    //---------------
    // Direction(Vector2) : 방향
    // Velocity(Vector2) : 방향 + 크기
    // Speed(float): 크기
    //---------------
    
    private Vector2 _currentDirection;
    private Vector2 _currentVelocity;
    private float _currentSpeed;

    /// <summary>
    /// 외부에서 알 움직임 제어할 시 사용
    /// </summary>
    public float Speed
    {
        get => _currentSpeed;
        set
        {
            _currentSpeed = Mathf.Max(0, value);
            _currentVelocity = _currentDirection * _currentSpeed;
        }
    }

    public Vector2 Direction
    {
        get => _currentDirection;
        set
        {
            _currentDirection = value.normalized;
            _currentVelocity = _currentDirection * _currentSpeed;
        }
    }
    
    public StoneMovement(StoneController stoneController, NetworkBehaviour networkBehaviour)
    {
        _stoneController = stoneController;
        _networkBehaviour = networkBehaviour;
        _stoneCollision = new StoneCollision();
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

        // 충돌 범위 설정
        _stoneCollision.CollisionRadius = target.GetComponent<StoneAttribute>().Scale * 0.45f;
        
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

        Speed = speed;
        if (direction.sqrMagnitude < 0.000001f)
        {
            Direction = Vector3.zero;
        }
        else
        {
            Direction = direction.normalized;
        }
        //_currentVelocity = _currentSpeed * _currentDirection;
        //_collidedThisFrame.Clear();

        // 움직임 시작 이벤트
        _stoneController.NotifyMovementStartedClientRpc(_currentVelocity);
            
        while (target != null && _currentSpeed > 0f)
        {
            // 충돌 체크
            if (_stoneCollision.IsOutOfOutline(target))
            {
                HandleOutOfMap(target, 1);
                break;
            }

            var normal = _stoneCollision.IsReflectCushionMap(target);
            if (normal != Vector2.zero)
            {
                ReflectStone(normal);
            }
            
            Transform collidedStone = _stoneCollision.CheckStoneCollision(target); 
        
            if (collidedStone != null && !_collidedThisFrame.Contains(collidedStone))
            {
                Debug.Log("충돌");
                _collidedThisFrame.Add(collidedStone);
            
                // 충돌 처리
                HandleCollision(target, collidedStone);
                Speed = CalculateSpeedAfterCollision(target, collidedStone, _currentSpeed);
            
                // 충돌 반영 시간 확보
                await UniTask.DelayFrame(2);
                _collidedThisFrame.Remove(collidedStone);
            }
            
            // 이동
            Vector2 pos = target.position;
            float moveStep = _currentSpeed * Time.deltaTime;
            target.position = pos + _currentDirection * moveStep;
            
            // 직전 위치로 이벤트
            _stoneController.NotifyMovementClientRpc(pos); 

            if (!_stoneCollision.IsOnIcePath(target))
            {
                // 감속
                float deceleration = _stoneController.Stone.CalculateDeceleration();
                Speed -= deceleration * Time.deltaTime;
                //_currentVelocity = _currentDirection * _currentSpeed;
            }
            
            // 다음 프레임까지 대기
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        
        _isMoving = false;
        _currentVelocity = Vector2.zero;
        _collidedThisFrame.Clear();
        
        // 경기장 밖으로 나갔는지 확인
        if (!_stoneCollision.IsInsideMap(target))
        {
            HandleOutOfMap(target, 0);
        }
        
        // 움직임 끝났을 때 이벤트
        _stoneController.NotifyMovementEndedClientRpc();
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
        ReflectStone(collisionNormal);
        
        // 상대방 알도 힘을 받고 움직임
        StoneController otherController = otherStone.GetComponent<StoneController>();
        StoneMovement otherMovement = otherController.StoneMovement;
        otherMovement?._collidedThisFrame.Add(target);
        
        if (otherMovement != null && !otherMovement._isMoving)
        {
            // 정지한 상태일 때
            float impactSpeed = otherController.Stone.CalculateCollisionSpeed(_currentSpeed);
            otherController.TriggerShootFromCollision(collisionNormal, impactSpeed);
            //otherMovement.MoveAsync(otherStone, collisionNormal, impactSpeed).Forget();
        }
        else
        {
            // TODO: 상대도 움직이는 상태에서 충돌했을 때
        }
        
        // 겹침 방지
        float distance = Vector2.Distance(target.position, otherStone.position);
        float overlap = _stoneCollision.CollisionRadius * 2 - distance;
        
        if (overlap > 0)
        {
            Vector2 separation = -collisionNormal * ((overlap / 2) + 0.1f);
            target.position = (Vector2)target.position + 2.0f * separation;
            //otherStone.position = (Vector2)otherStone.position - separation;
        }
    }

    /// <summary>
    /// 알을 반사하는 함수
    /// </summary>
    /// <param name="collisionNormal"></param>
    private void ReflectStone(Vector2 collisionNormal)
    {
        Vector2 reflectedDirection = Vector2.Reflect(_currentDirection, -collisionNormal);
        Direction = reflectedDirection;
        Speed *= _bounceDamping;
        //_currentVelocity = reflectedVelocity * _bounceDamping;
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
        if(!target.CompareTag("Shadow"))
            EffectManager.Instance.DestroyEffectClientRpc(target.transform.position);
        _stoneController.Stone.SetAnimatorTriggerClientRpc(Stone.HashDead);
    }
    
    // ----------- 이벤트 호출 ----------
    public void TriggerMovementStartedEvent(Vector2 velocity)
    {
        OnMovementStarted?.Invoke(velocity);
    }
    public void TriggerMovementEvent(Vector2 position)
    {
        OnMovement?.Invoke(position);
    }
    
    public void TriggerMovementEndedEvent()
    {
        OnMovementEnded?.Invoke();
    }
    
    public void TriggerCollisionEnterEvent()
    {
        OnCollisionEnter?.Invoke();
    }
}
