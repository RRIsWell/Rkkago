using UnityEngine;

public class MapBoundary : MonoBehaviour
{
    float halfThreshold;

    public void Init(MapConfig config)
    {
        halfThreshold = config.allowHalfOut ? 0.5f : 0f;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var stone = other.GetComponent<Stone>();
        if (stone == null) return;

        /*
        float outRatio = stone.GetOutRatio(); // 돌이 얼마나 밖으로 나갔는지
        if (outRatio > halfThreshold)
        {
            stone.Kill();
        }
        */
    }
}
