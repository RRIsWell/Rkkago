using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CameraEffect : MonoBehaviour
{
    // 카메라 세팅
    private Camera _camera;
    private float _originalZoomSize;
    private Vector3 _originalPosition;
    
    // 줌 설정
    private readonly AnimationCurve _zoomCurve =  AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    // 카메라 흔들림 설정
    [Header("Shake Settings")]
    [SerializeField] private AnimationCurve shakeCurve;

    private CancellationTokenSource _cts;
    
    private void Start()
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
    
    /// <summary>
    /// 카메라 흔들림
    /// </summary>
    /// <param name="duration"></param>
    /// <param name="magnitude"></param>
    /// <param name="cancellationToken"></param>
    public async UniTask ShakeAsync(float duration, float magnitude, CancellationToken cancellationToken)
    {
        var originalPos = _camera.transform.position;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            float curveValue = shakeCurve.Evaluate(progress);
            
            var offsetX = Random.Range(-1f, 1f) * magnitude;
            var offsetY = Random.Range(-1f, 1f) * magnitude;

            _camera.transform.position = new Vector3(
                originalPos.x + offsetX,
                originalPos.y + offsetY,
                originalPos.z
            );

            elapsed += Time.deltaTime;
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        _camera.transform.position = originalPos;
    }
}
