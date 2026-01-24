using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class EffectManager : NetworkBehaviour
{
    public static EffectManager Instance { get; private set; }
    private CameraEffect _cameraEffect;
    void Awake()
    {
        Instance = this;
        
        _cameraEffect = GetComponent<CameraEffect>();
    }


    [ClientRpc]
    public void CollisionEffectClientRpc(Vector3 position)
    {
        //if (!IsServer) return;
        CollisionEffect(position).Forget();
    }
    
    /// <summary>
    /// 두 알이 충돌했을 때
    /// </summary>
    /// <param name="position"></param>
    private async UniTask CollisionEffect(Vector3 position)
    {
        
    }


    [ClientRpc]
    public void DestroyEffectClientRpc(Vector3 position)
    {
        //if (!IsServer) return;
        DestroyEffect(position).Forget();
    }
    
    /// <summary>
    /// 알이 사라질 때
    /// </summary>
    /// <param name="position"></param>
    private async UniTask DestroyEffect(Vector3 position)
    {
        await _cameraEffect.ZoomToPosition(position, 3.0f, 0.3f, this.GetCancellationTokenOnDestroy());
        _cameraEffect.ShakeAsync(0.3f, 0.1f, this.GetCancellationTokenOnDestroy()).Forget();
        
        await UniTask.Delay(500);
        
        await _cameraEffect.ZoomOut(0.1f, this.GetCancellationTokenOnDestroy());
    }
}
