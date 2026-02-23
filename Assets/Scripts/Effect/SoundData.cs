using System.Collections.Generic;
using UnityEngine;

public enum BGMName
{
    로비,
    기본맵,
    컬링맵,
    당구맵,
}

public enum SFXName
{
    알차징,
    알날아감,
    알충돌,
    알시무룩,
    폭탄,
    빙판길생성,
    텔포,
    킹실드,
    
    //UI
    버튼클릭,
    UI보이기,
    warning,
    동전던지기,
    myTurn,
    승리,
}

[System.Serializable]
public class BGMData
{
    public BGMName soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    public bool loop = true;
}

[System.Serializable]
public class SFXData
{
    public SFXName soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    public bool loop = false;
}


[CreateAssetMenu(fileName = "SoundData", menuName = "Scriptable Objects/SoundData")]
public class SoundData : ScriptableObject
{
    public List<BGMData> bgmList = new List<BGMData>();
    public List<SFXData> sfxList = new List<SFXData>();
}