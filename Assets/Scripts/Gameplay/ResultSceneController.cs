using UnityEngine;
using Unity.Netcode;

public class ResultSceneController : NetworkBehaviour
{
    [Header("P1이 이김")]
    [SerializeField] private GameObject P1Badge;
    [SerializeField] private GameObject P1Profile;

    [Header("P2가 이김")]
    [SerializeField] private GameObject P2Badge;
    [SerializeField] private GameObject P2Profile;

    private void Start()
    {
        var flow = ResultFlowManager.Instance;
        if (flow == null) return;

        // WinnerClientId는 서버가 넣어두고 같이 넘어옴
        ulong winnerId = flow.WinnerClientId.Value;
        ulong hostId = NetworkManager.ServerClientId;

        // host = P1(왼쪽/파랑), client = P2(오른쪽/분홍)
        bool P1Won = (winnerId == hostId);

        SetWinnerUI(P1Won);
    }

    private void SetWinnerUI(bool P1Won)
    {
        // 파랑이 승리면: 파랑 배지+파랑 프로필 ON, 분홍 OFF
        if (P1Badge != null)   P1Badge.SetActive(P1Won);
        if (P1Profile != null) P1Profile.SetActive(P1Won);

        if (P2Badge != null)   P2Badge.SetActive(!P1Won);
        if (P2Profile != null) P2Profile.SetActive(!P1Won);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            RequestGoLobbyServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestGoLobbyServerRpc()
    {
        if (ResultFlowManager.Instance == null) return;
        ResultFlowManager.Instance.Server_GoLobby();
    }
}