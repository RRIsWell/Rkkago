using System;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class EffectManager : NetworkBehaviour
{
    public static EffectManager Instance { get; private set; }
    private CameraEffect _cameraEffect;
    [SerializeField]
    private GameObject effectPrefab;
    private GameObject _effectObject;
    
    void Awake()
    {
        Instance = this;
        
        _cameraEffect = GetComponent<CameraEffect>();
        _effectObject = Instantiate(effectPrefab);
        _effectObject.SetActive(false);
    }

    [ClientRpc]
    public void CollisionEffectClientRpc(Vector3 position)
    {
        CollisionEffect(position).Forget();
    }
    
    /// <summary>
    /// 두 알이 충돌했을 때
    /// </summary>
    /// <param name="position"></param>
    private async UniTask CollisionEffect(Vector3 position)
    {
        _effectObject.SetActive(true);
        _effectObject.transform.position = position;
        
        await UniTask.Delay(300);
        
        _effectObject.SetActive(false);
    }

    [ClientRpc]
    public void DestroyEffectClientRpc(Vector3 position)
    {
        DestroyEffect(position).Forget();
    }

    /// <summary>
    /// 알이 사라질 때
    /// </summary>
    /// <param name="position"></param>
    private async UniTask DestroyEffect(Vector3 position)
    {
        // camera
        await _cameraEffect.ZoomToPosition(position, 3.0f, 0.3f, this.GetCancellationTokenOnDestroy());
        _cameraEffect.ShakeAsync(0.3f, 0.1f, this.GetCancellationTokenOnDestroy()).Forget();
        
        await UniTask.Delay(500);
        
        await _cameraEffect.ZoomOut(0.1f, this.GetCancellationTokenOnDestroy());
    }

    /// <summary>
    /// 빙판길 생성
    /// </summary>
    /// <param name="position"></param>
    /// <param name="ownerRef"></param>
    public void CreateIceTile(Vector2 position, NetworkObjectReference ownerRef)
    {
        CreateIceTileServerRpc(position, ownerRef);
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void CreateIceTileServerRpc(Vector2 position, NetworkObjectReference ownerRef)
    {
        CreateIceTileClientRpc(position, ownerRef);
    }
    
    [ClientRpc]
    private void CreateIceTileClientRpc(Vector2 position, NetworkObjectReference ownerRef)
    {
        // NetworkObjectReference를 통해 원래 Stone 찾기
        if (ownerRef.TryGet(out NetworkObject netObj))
        {
            var controller = netObj.GetComponent<StoneController>();
            if (controller != null)
            {
                var skill = controller.SkillContainer.GetSkillByName(SkillName.IceAge);
                if (skill is IceAge iceAge)
                {
                    iceAge.CreateSingleIceTile(position);
                }
            }
        }
    }
}
