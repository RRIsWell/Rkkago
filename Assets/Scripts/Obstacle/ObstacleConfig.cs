using UnityEngine;

public enum ObstacleType
{
    GravityCore,
    BounceCore,
    Bomb
}

[CreateAssetMenu(menuName = "Obstacle/ObstacleConfig")]
public class ObstacleConfig : ScriptableObject
{
    [Header("Identity")]
    public string obstacleName;
    public ObstacleType type;

    [Header("Visual")]
    public Sprite sprite;
    public Vector2 size = Vector2.one;

    [Header("Spawn")]
    [Range(0f, 1f)] public float weight = 1f; // 랜덤 뽑기 가중치

    [Header("Gravity Core")]
    public float gravityRadius = 2.5f;
    public float gravityStrength = 15f;

    [Header("Bounce Core")]
    public float bounceMultiplier = 1.3f; // 튕길 때 속도 배수
    public float bonusMultiplier = 2.0f; // 원 안에 들어오면 보너스 속도 배수
    public float bonusRadius = 1.0f;

    [Header("Bomb")]
    public int explodeAfterTurns = 4;
    public float explodeRadius = 2.0f;
    public float explodeImpulse = 12f;
}
