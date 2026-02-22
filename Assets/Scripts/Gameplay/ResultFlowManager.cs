using UnityEngine;

public class ResultFlowManager : MonoBehaviour
{
    public static ResultFlowManager Instance;

    // 둘 다 저장해야 하므로 static으로 둬도 OK
    public ulong WinnerClientId = ulong.MaxValue;

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

    public void SetWinner(ulong winnerId)
    {
        WinnerClientId = winnerId;
    }
}