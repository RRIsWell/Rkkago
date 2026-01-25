using System.Collections.Generic;
using UnityEngine;

public enum BGMName
{
    로비,
    기본맵,
}

public enum SFXName
{
    알충돌,
    알터짐,
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