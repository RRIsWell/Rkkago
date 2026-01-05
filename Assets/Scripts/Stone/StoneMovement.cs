using UnityEngine;
using Cysharp.Threading.Tasks;

public class StoneMovement
{
    private float _moveDistance = 3.0f; // 맵 밖으로 나갈 정도의 이동값
    
    /// <summary>
    /// 외부 접근: 알을 튕기는 함수
    /// </summary>
    /// <param name="transform">알</param>
    /// <param name="mousePosition">마우스 월드 좌표</param>
    /// <param name="speed">속력</param>
    public void Shoot(Transform transform, Vector2 mousePosition, float speed)
    {
        Vector2 objectPos = transform.position;
        Vector2 direction = objectPos - mousePosition;
        Vector2 destination = objectPos + direction * _moveDistance;
        
        // 알 이동
        MoveAsync(transform, destination, speed).Forget();
    }
    
    /// <summary>
    /// 알을 일정 속도로 목적지까지 이동시키는 함수
    /// </summary>
    private async UniTask MoveAsync(Transform target, Vector2 destination, float speed)
    {
        while (target != null)
        {
            // 목적지와의 거리 계산
            float distance = Vector2.Distance(target.position, destination);

            if (distance < 0.01f)
                break;

            // 이동
            target.position = Vector2.MoveTowards(
                target.position,
                destination,
                speed * Time.deltaTime
            );

            // 다음 프레임까지 대기
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
    }
    
    // TODO: 충돌 감지
    // TODO: 충돌했을 때 반사하는 Task
}
