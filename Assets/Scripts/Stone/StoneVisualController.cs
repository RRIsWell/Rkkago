using UnityEngine;
using Unity.Netcode;

public enum VisualState
{
    Normal = 0,
    Deception = 1, // 상대에게 같은 색으로 보임
    Invisible = 2, // (예시) 투명해짐
    Giant = 3      // (예시) 커 보임
}

/// <summary>
/// 돌의 시각적 요소를 관리하는 클래스
/// </summary>
public class StoneVisualController : NetworkBehaviour
{
    [Header("Skill Effects")]
    [SerializeField] private GameObject graffitiPrefab; // Spray Terror
    
    [SerializeField] private SpriteRenderer _renderer;
    
    private GameObject _currentEffectObject;
    
    // 현재 이 돌에 적용된 시각적 상태 (서버에서 관리, 모두에게 동기화)
    public NetworkVariable<VisualState> currentVisualState = new NetworkVariable<VisualState>(
        VisualState.Normal,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private int _myTeamId; // 이 돌의 진짜 팀 ID

    private void Awake()
    {
        if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        // 상태가 변할 때마다 화면 갱신 함수 호출
        currentVisualState.OnValueChanged += (prev, current) => UpdateVisuals();
    }

    /// <summary>
    /// Stone에서 팀이 설정될 때 호출해줘야 함
    /// </summary>
    public void InitializeVisuals(int teamId)
    {
        _myTeamId = teamId;
        UpdateVisuals();
    }

    /// <summary>
    /// 현재 상태와 보는 사람이 누군지를 조합해 최종 색상을 결정
    /// </summary>
    private void UpdateVisuals()
    {
        // 기본 색상 결정
        Color finalColor = (_myTeamId == 1) ? Color.cyan : new Color(1f, 0.4f, 0.4f);
        Vector3 finalScale = transform.localScale; // 필요시 스케일 로직 추가

        // 특수 상태에 따른 로직 분기
        switch (currentVisualState.Value)
        {
            case VisualState.Normal:
                break;

            case VisualState.Deception:
                // 해킹 상태일 때
                // 내가 이 돌의 주인이라면 -> 원래 색을 보여줌
                // 남이 보고 있다면 -> 모두 회색으로 통일해서 보여줌
                if (!IsOwner) 
                {
                    finalColor = Color.white;
                }
                break;
                
            // 스킬이 추가되면 여기에 case 늘리면 됨
        }

        // 최종 적용
        _renderer.color = finalColor;
    }

    /// <summary>
    /// 스킬 등 외부에서 시각적 상태를 변경 요청할 때 사용 (ServerRPC 필요)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SetVisualStateServerRpc(VisualState newState)
    {
        currentVisualState.Value = newState;
    }
    
    /// <summary>
    /// Stone에서 호출. 모든 클라이언트에게 "효과 지워!"라고 명령
    /// </summary>
    public void ResetVisuals()
    {
        if (IsServer)
        {
            ResetVisualsClientRpc();
        }
        else
        {
            // 내가 서버가 아니면 서버한테 "나 초기화 좀 시켜줘"라고 요청
            ResetVisualsServerRpc();
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void ResetVisualsServerRpc()
    {
        ResetVisualsClientRpc();
    }
    
    [ClientRpc]
    private void ResetVisualsClientRpc()
    {
        // 색상 복구 (팀 색상으로)
        GetComponent<SpriteRenderer>().color = (_myTeamId == 1) ? Color.cyan : new Color(1f, 0.4f, 0.4f);; // 혹은 원래 로직대로 복구
        
        // 지금 떠 있는 낙서가 있다면 즉시 삭제 ★
        if (_currentEffectObject != null)
        {
            Destroy(_currentEffectObject);
            _currentEffectObject = null;
        }
    }
    
    public void CastGraffitiSkill()
    {
        if (!IsOwner) return;
        RequestGraffitiServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc]
    private void RequestGraffitiServerRpc(ulong attackerId)
    {
        ulong victimId = 0;
        bool foundVictim = false;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId != attackerId)
            {
                victimId = client.ClientId;
                foundVictim = true;
                break; 
            }
        }

        if (foundVictim)
        {
            ClientRpcParams rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { victimId } }
            };

            ApplyGraffitiClientRpc(rpcParams);
        }
    }
    
    [ClientRpc]
    private void ApplyGraffitiClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (_currentEffectObject != null) Destroy(_currentEffectObject);
        
        // 현재 씬에 있는 캔버스 찾기 (캔버스가 여러개면 잘못 작동될 수 있음)
        GameObject mainCanvas = GameObject.FindGameObjectWithTag("MainUI");

        if (mainCanvas != null && graffitiPrefab != null)
        {
            // 프리팹 생성
            _currentEffectObject = Instantiate(graffitiPrefab, mainCanvas.transform);

            // UI가 다른 것보다 위에 그려지도록 순서 조정
            _currentEffectObject.transform.SetAsLastSibling(); 
            
            Debug.Log("낙서 프리팹 생성 완료!");
        }
        else
        {
            Debug.LogWarning("캔버스를 찾을 수 없거나 프리팹이 연결되지 않았습니다.");
        }
    }
}