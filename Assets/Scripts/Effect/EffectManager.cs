using Cysharp.Threading.Tasks;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }
    private CameraEffect _cameraEffect;
    void Awake()
    {
        Instance = this;
        
        _cameraEffect = new CameraEffect();
    }

    /// <summary>
    /// 두 알이 충돌했을 때
    /// </summary>
    /// <param name="position"></param>
    public async UniTask CollisionEffect(Vector3 position)
    {
        
    }
    
    /// <summary>
    /// 알이 사라질 때
    /// </summary>
    /// <param name="position"></param>
    public async UniTask DestroyEffect(Vector3 position)
    {
        await _cameraEffect.ZoomToPosition(position, 3.0f, 0.3f, this.GetCancellationTokenOnDestroy());
        await UniTask.Delay(1000);
        await _cameraEffect.ZoomOut(0.1f, this.GetCancellationTokenOnDestroy());
    }
}
