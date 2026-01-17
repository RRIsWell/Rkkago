using UnityEngine;

public class DragLine : MonoBehaviour
{
    public void SetTransform(Transform stoneTransform)
    {
        transform.position = stoneTransform.position;
        transform.localScale = new Vector3(1, 0, 1);
    }
    
    public void UpdateDragLine(Transform stoneTransform, Vector2 direction, float distance)
    {
        // 회전값
        float angle = Vector2.SignedAngle(Vector2.up, direction);
        transform.rotation = Quaternion.Euler(0, 0, angle);
        
        // 길이
        Vector3 scale = transform.localScale;
        scale.y = distance;
        transform.localScale = scale;
        
        transform.position = stoneTransform.position + (Vector3)(direction * (distance * 0.5f));
    }
}
