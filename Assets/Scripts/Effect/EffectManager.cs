using System;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class EffectManager : NetworkBehaviour
{
    public static EffectManager Instance { get; private set; }
    private CameraEffect _cameraEffect;
    
    [SerializeField]
    private GameObject _collisionEffectPrefab;
    
    [SerializeField]
    private GameObject _activateEffectPrefab;
    
    [SerializeField]
    private GameObject _teleportationEffectPrefab;
    
    void Awake()
    {
        Instance = this;
        
        _cameraEffect = GetComponent<CameraEffect>();
    }

    /// <summary>
    /// 두 알이 충돌했을 때
    /// </summary>
    /// <param name="position"></param>
    [ClientRpc]
    public void CollisionEffectClientRpc(Vector3 position)
    {
        ShowEffect(position, 400, _collisionEffectPrefab).Forget();
    }
    
    /// <summary>
    /// 스킬 부여될 때
    /// </summary>
    /// <param name="position"></param>
    public void ActivateEffect(Vector3 position)
    {
        ShowEffect(position, 500, _activateEffectPrefab).Forget();
    }
    
    /// <summary>
    /// 텔레포테이션 스킬 시전될 때
    /// </summary>
    /// <param name="position"></param>
    public void TeleportationEffect(Vector3 position)
    {
        ShowEffect(position, 600, _teleportationEffectPrefab).Forget();
    }

    /// <summary>
    /// 이펙트를 보여주는 함수
    /// </summary>
    /// <param name="position"></param>
    /// <param name="durationMs"></param>
    /// <param name="effectPrefab"></param>
    private async UniTask ShowEffect(Vector3 position, int durationMs, GameObject effectPrefab)
    {
        if (effectPrefab == null) return;

        GameObject go = Instantiate(effectPrefab);
        go.transform.position = position;
        
        await UniTask.Delay(durationMs);
        
        Destroy(go);
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

    
    // ------------------------------- 빙판길 스킬 ---------------------------------
    
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
    
    /// <summary>
    /// 빙판길 업데이트
    /// </summary>
    /// <param name="stonePos"></param>
    /// <param name="ownerRef"></param>
    public void UpdateIceTile(Vector2 stonePos, NetworkObjectReference ownerRef)
    {
        UpdateIceTileServerRpc(stonePos, ownerRef);
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void UpdateIceTileServerRpc(Vector2 stonePos, NetworkObjectReference ownerRef)
    {
        UpdateIceTileClientRpc(stonePos, ownerRef);
    }
    
    [ClientRpc]
    private void UpdateIceTileClientRpc(Vector2 stonePos, NetworkObjectReference ownerRef)
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
                    iceAge.UpdateIceScale(stonePos);
                }
            }
        }
    }
    
    /// <summary>
    /// 빙판길 삭제
    /// </summary>
    /// <param name="ownerRef"></param>
    public void DestroyIceTile(NetworkObjectReference ownerRef)
    {
        DestroyIceTileServerRpc(ownerRef);
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void DestroyIceTileServerRpc(NetworkObjectReference ownerRef)
    {
        DestroyIceTileClientRpc(ownerRef);
    }
    
    [ClientRpc]
    private void DestroyIceTileClientRpc(NetworkObjectReference ownerRef)
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
                    iceAge.DestroySingleIceTile();
                }
            }
        }
    }
    
    /// <summary>
    /// 모든 빙판길 삭제
    /// </summary>
    /// <param name="ownerRef"></param>
    public void DestroyAllIceTiles(NetworkObjectReference ownerRef)
    {
        DestroyAllIceTilesServerRpc(ownerRef);
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void DestroyAllIceTilesServerRpc(NetworkObjectReference ownerRef)
    {
        DestroyAllIceTilesClientRpc(ownerRef);
    }
    
    [ClientRpc]
    private void DestroyAllIceTilesClientRpc(NetworkObjectReference ownerRef)
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
                    iceAge.DestroyAllIceTiles();
                }
            }
        }
    }
    
    // --------------------- Shadow Partner -----------------------------

    public void CreateAndShootShadowObjects(Vector2 velocity, NetworkObjectReference ownerRef)
    {
        CreateAndShootShadowObjectsServerRpc(velocity, ownerRef);
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void CreateAndShootShadowObjectsServerRpc(Vector2 velocity, NetworkObjectReference ownerRef)
    {
        if (ownerRef.TryGet(out NetworkObject netObj))
        {
            var controller = netObj.GetComponent<StoneController>();
            if (controller != null)
            {
                var skill = controller.SkillContainer.GetSkillByName(SkillName.ShadowPartner);
                if (skill is ShadowPartner shadowPartner)
                {
                    shadowPartner.CreateTwoShadowObjectsAndShoot(velocity);
                }
            }
        }
    }

    public void SetShadowObjects(NetworkObjectReference ownerRef, NetworkObjectReference shadowRef1,
        NetworkObjectReference shadowRef2)
    {
        SetColorAlphaClientRpc(ownerRef, shadowRef1, shadowRef2);
    }
    [ClientRpc]
    private void SetColorAlphaClientRpc(NetworkObjectReference ownerRef, NetworkObjectReference shadowRef1, NetworkObjectReference shadowRef2)
    {
        if (ownerRef.TryGet(out NetworkObject netObj))
        {
            if (netObj.OwnerClientId != NetworkManager.Singleton.LocalClientId) return;
            
            var controller = netObj.GetComponent<StoneController>();
            if (controller != null)
            {
                var skill = controller.SkillContainer.GetSkillByName(SkillName.ShadowPartner);
                if (skill is ShadowPartner shadowPartner)
                {
                    shadowPartner.SetColorAlpha(shadowRef1, shadowRef2);
                }
            }
        }
    }
    
    /// <summary>
    /// 분신 삭제
    /// </summary>
    /// <param name="ownerRef"></param>
    public void DestroyShadowObjects(NetworkObjectReference ownerRef)
    {
        DestroyShadowObjectsServerRpc(ownerRef);
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void DestroyShadowObjectsServerRpc(NetworkObjectReference ownerRef)
    {
        if (ownerRef.TryGet(out NetworkObject netObj))
        {
            var controller = netObj.GetComponent<StoneController>();
            if (controller != null)
            {
                var skill = controller.SkillContainer.GetSkillByName(SkillName.ShadowPartner);
                if (skill is ShadowPartner shadowPartner)
                {
                    shadowPartner.DestroyShadowObjects();
                }
            }
        }
    }
    
    /// <summary>
    /// 분신 모두 삭제
    /// </summary>
    /// <param name="ownerRef"></param>
    public void DestroyAllShadowObjects(NetworkObjectReference ownerRef)
    {
        DestroyAllShadowObjectsServerRpc(ownerRef);
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void DestroyAllShadowObjectsServerRpc(NetworkObjectReference ownerRef)
    {
        if (ownerRef.TryGet(out NetworkObject netObj))
        {
            var controller = netObj.GetComponent<StoneController>();
            if (controller != null)
            {
                var skill = controller.SkillContainer.GetSkillByName(SkillName.ShadowPartner);
                if (skill is ShadowPartner shadowPartner)
                {
                    shadowPartner.DestroyAllShadowObjects();
                }
            }
        }
    }
    
    
    // =================== Teleportation =============================

    public void DoTeleportation(NetworkObjectReference ownerRef)
    {
        DoTeleportationServerRpc(ownerRef);
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void DoTeleportationServerRpc(NetworkObjectReference ownerRef)
    {
        if (ownerRef.TryGet(out NetworkObject netObj))
        {
            var controller = netObj.GetComponent<StoneController>();
            if (controller != null)
            {
                var skill = controller.SkillContainer.GetSkillByName(SkillName.Teleportation);
                if (skill is Teleportation teleportation)
                {
                    teleportation.DoingTeleportation();
                }
            }
        }
    }
    
    [ClientRpc]
    public void TeleportPositionClientRpc(NetworkObjectReference ownerRef, Vector3 targetPos)
    {
        if (ownerRef.TryGet(out NetworkObject netObj))
        {
            // 모든 클라이언트에서 해당 오브젝트의 위치를 강제로 설정
            netObj.transform.position = targetPos;
            
            // 이펙트
            TeleportationEffect(targetPos);
        }
    }
}
