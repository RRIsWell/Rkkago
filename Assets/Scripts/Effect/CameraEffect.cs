using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CameraEffect
{
    // 카메라 세팅
    private readonly Camera _camera;
    private readonly float _originalZoomSize;
    private readonly Vector3 _originalPosition;
    
    // 줌 설정
    private readonly AnimationCurve _zoomCurve =  AnimationCurve.EaseInOut(0, 0, 1, 1);

    private CancellationTokenSource _cts;
    
    public CameraEffect()
    {
        _camera = Camera.main;
        if (_camera != null)
        {
            _originalZoomSize = _camera.orthographicSize;
            _originalPosition = _camera.transform.position;
        }
    }
    
    /// <summary>
    /// 특정 위치로 줌인
    /// </summary>
    /// <param name="targetPosition"></param>
    /// <param name="size"></param>
    /// <param name="duration"></param>
    /// <param name="cancellationToken"></param>
    public async UniTask ZoomToPosition(Vector2 targetPosition, float size, float duration, CancellationToken cancellationToken = default)
    {
        // 이전 작업 취소
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            await ZoomAsync(targetPosition, size, duration, _cts.Token);
        }
        catch (System.OperationCanceledException)
        {
            // 취소됨 - 정상 동작
        }
    }

    /// <summary>
    /// 원래 상태로 복귀
    /// </summary>
    /// <param name="duration"></param>
    /// <param name="cancellationToken"></param>
    public async UniTask ZoomOut(float duration, CancellationToken cancellationToken = default)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            await ZoomAsync(_originalPosition, _originalZoomSize, duration, _cts.Token);
        }
        catch (System.OperationCanceledException)
        {
            // 취소됨 - 정상 동작
        }
    }

    /// <summary>
    /// 줌 Task
    /// </summary>
    /// <param name="targetPosition"></param>
    /// <param name="duration"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="size"></param>
    private async UniTask ZoomAsync(Vector2 targetPosition, float size, float duration, CancellationToken cancellationToken)
    {
        float startSize = _camera.orthographicSize;
        Vector3 startPos = _originalPosition;
        Vector3 endPos = new Vector3(targetPosition.x, targetPosition.y, _originalPosition.z);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = _zoomCurve.Evaluate(elapsed / duration);

            _camera.orthographicSize = Mathf.Lerp(startSize, size, t);
            _camera.transform.position = Vector3.Lerp(startPos, endPos, t);

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        // 최종 값 보정
        _camera.orthographicSize = size;
        _camera.transform.position = endPos;
    }
}
