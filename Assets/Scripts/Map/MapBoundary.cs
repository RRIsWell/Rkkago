using UnityEngine;

public class MapBoundary : MonoBehaviour
{
    private MapRuleExecutor rule;

    public void Init(MapConfig config, MapRuleExecutor executor)
    {
        rule = executor;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if(!rule || !rule.IsServer) return; // 서버만 판정하도록
        
        var stone = other.GetComponent<Stone>();
        if (stone == null) return;

        rule.OnStoneOut(stone);
    }
}
