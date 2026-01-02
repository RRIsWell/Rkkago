using UnityEngine;

[CreateAssetMenu(fileName = "StoneData", menuName = "Scriptable Objects/StoneData")]
public class StoneData : ScriptableObject
{
    // 예시 입니다
    // 기획 구체화 되고 개발하면서 추가할 예정
    [Header("Physics")]
    public float speed;
    public float scale;
    public float power;

    [Header("Design")] 
    public Sprite sprite;
}
