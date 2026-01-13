using UnityEngine;

public class SurfaceController : MonoBehaviour
{
    Collider2D surface;

    public void Init(MapConfig config)
    {
        surface = GetComponent<Collider2D>();

        var mat = new PhysicsMaterial2D();
        mat.friction = config.friction;
        mat.bounciness = config.slippery ? 0.1f : 0f;

        surface.sharedMaterial = mat;
    }
}
