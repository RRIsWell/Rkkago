using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class ResultSceneController : NetworkBehaviour
{
    [Header("Winner Panels")]
    [SerializeField] private GameObject WinnerP1; // Canvas/WinnerP1
    [SerializeField] private GameObject WinnerP2; // Canvas/WinnerP2

    private void Start()
    {
        // 둘 다 끄고 시작
        if (WinnerP1 != null) WinnerP1.SetActive(false);
        if (WinnerP2 != null) WinnerP2.SetActive(false);
        
        var flow = ResultFlowManager.Instance;
        if (flow == null) return;

        // WinnerClientId는 서버가 넣어두고 같이 넘어옴
        ulong winnerId = flow.WinnerClientId;
        ulong hostId = NetworkManager.ServerClientId;

        // host = P1(왼쪽/파랑), client = P2(오른쪽/분홍)
        bool P1Won = (winnerId == hostId);

        // 무조건 둘 중 하나만 켜짐
        if (WinnerP1 != null) WinnerP1.SetActive(P1Won);
        if (WinnerP2 != null) WinnerP2.SetActive(!P1Won);
    }

    private void Update()
    {
        // New Input System 방식 Enter 감지
        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            RequestGoLobbyServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestGoLobbyServerRpc()
    {
        if (!IsServer) return; // 서버만 씬 로드
        NetworkManager.Singleton.SceneManager.LoadScene(
            "StartScene",
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );
    }
}