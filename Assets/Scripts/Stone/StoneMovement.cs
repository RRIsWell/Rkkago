using UnityEngine;
using Cysharp.Threading.Tasks;

public class StoneMovement
{
    private readonly float _deceleration = 50.0f;
    
    /// <summary>
    /// 외부 접근: 알을 튕기는 함수
    /// </summary>
    /// <param name="target">알</param>
    /// <param name="direction">날아가는 방향</param>
    /// <param name="speed">속력</param>
    public void Shoot(Transform target, Vector2 direction, float speed)
    {
        // 알 이동
        MoveAsync(target, direction, speed).Forget();
    }
    
    /// <summary>
    /// 알을 이동시키는 함수
    /// </summary>
    private async UniTask MoveAsync(Transform target, Vector2 direction, float speed)
    {
        float deceleration = _deceleration; // 감속 정도
        float currentSpeed = speed;
        
        while (target != null)
        {
            if (currentSpeed <= 0f)
                break;
            
            Vector2 pos = target.position;
            float moveStep = currentSpeed * Time.deltaTime;
            target.position = pos + direction * moveStep;

            // 감속
            currentSpeed -= deceleration * Time.deltaTime;

            // 다음 프레임까지 대기
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
    }
    
    // TODO: 충돌 감지
    // TODO: 충돌했을 때 반사하는 Task
}
