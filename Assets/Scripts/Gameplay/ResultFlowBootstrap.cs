using UnityEngine;

public static class ResultFlowBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Ensure()
    {
        if (Object.FindAnyObjectByType<ResultFlowManager>() != null) return;

        var go = new GameObject("ResultFlowManager");
        go.AddComponent<ResultFlowManager>();
        Object.DontDestroyOnLoad(go);
    }
}