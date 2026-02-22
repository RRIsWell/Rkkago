using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class ResultFlowManager : NetworkBehaviour
{
    public static ResultFlowManager Instance;

    // 서버가 확정한 승자
    public NetworkVariable<ulong> WinnerClientId = new NetworkVariable<ulong>(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// (서버) 결과 저장 후 ResultScene으로 모두 이동
    /// </summary>
    public void Server_GoResultScene(ulong winnerId)
    {
        if (!IsServer) return;

        WinnerClientId.Value = winnerId;

        // Netcode 씬 로딩: 전원 동기화
        NetworkManager.SceneManager.LoadScene("ResultScene", LoadSceneMode.Single);
    }

    /// <summary>
    /// (서버) StartScene으로 모두 이동
    /// </summary>
    public void Server_GoLobby()
    {
        if (!IsServer) return;

        NetworkManager.SceneManager.LoadScene("StartScene", LoadSceneMode.Single);
    }
}