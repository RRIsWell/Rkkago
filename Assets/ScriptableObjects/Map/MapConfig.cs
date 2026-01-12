using UnityEngine;
using System.Data.Common;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

[CreateAssetMenu(menuName = "Map/MapConfig")]
public class MapConfig : ScriptableObject
{
    [Header("기본 규칙")]
    public int stonesPerPlayer = 5;
    public bool allowHalfOut = true;

    [Header("표면")]
    public float friction = 0.8f;
    public bool slippery = false;

    [Header("장애물")]
    public bool useObstacle;

    [Header("패배 판정")]
    public int loseStoneCount = 5;
}