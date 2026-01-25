using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    
    [Header("Sound Database")]
    public SoundData soundData;
    
    [Header("Audio Sources")]
    private AudioSource _bgmSource;
    private List<AudioSource> _sfxSources = new List<AudioSource>();
    
    [Header("Settings")]
    [SerializeField] private int sfxSourceCount = 5;
    
    //TODO: 사운드 데이터 딕셔너리로 저장
    
    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 초기화
        InitializeAudioSources();
    }
    
    private void InitializeAudioSources()
    {
        // BGM
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;
        
        // SFX
        for (int i = 0; i < sfxSourceCount; i++)
        {
            AudioSource sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            _sfxSources.Add(sfxSource);
        }
    }
    
    /// <summary>
    /// BGM을 재생하는 함수
    /// </summary>
    /// <param name="soundName">재생할 사운드 이름</param>
    public void PlayBGM(BGMName soundName)
    {
        if (soundData == null)
        {
            Debug.LogWarning("SoundDatabase 할당 안됨");
            return;
        }

        var data = soundData.bgmList.Find(x => x.soundName == soundName);
        if (data != null && data.audioClip != null)
        {
            _bgmSource.clip = data.audioClip;
            _bgmSource.volume = data.volume;
            _bgmSource.loop = data.loop;
            _bgmSource.Play();
        }
        else
        {
            Debug.LogWarning($"BGM '{soundName}'을 찾을 수 없음");
        }
    }

    /// <summary>
    /// BGM 정지
    /// </summary>
    public void StopBGM()
    {
        _bgmSource.Stop();
    }

    /// <summary>
    /// BGM 일시정지
    /// </summary>
    public void PauseBGM()
    {
        _bgmSource.Pause();
    }

    /// <summary>
    /// BGM 재개
    /// </summary>
    public void ResumeBGM()
    {
        _bgmSource.UnPause();
    }
    
    /// <summary>
    /// SFX를 재생하는 함수
    /// </summary>
    /// <param name="soundName">재생할 사운드 이름</param>
    public void PlaySFX(SFXName soundName)
    {
        if (soundData == null)
        {
            Debug.LogWarning("SoundDatabase 할당 안됨");
            return;
        }
        
        var data = soundData.sfxList.Find(x => x.soundName == soundName);
        if (data != null && data.audioClip != null)
        {
            // 사용 가능한 오디오 소스 찾기
            AudioSource source = GetAvailableSFXSource();
            if (source != null)
            {
                source.clip = data.audioClip;
                source.volume = data.volume;
                source.loop = data.loop;
                source.Play();
            }
        }
        else
        {
            Debug.LogWarning($"SFX '{soundName}'을 찾을 수 없음");
        }
    }
    private AudioSource GetAvailableSFXSource()
    {
        // 사용 가능한 소스가 없으면 재생이 끝난 소스를 찾아서 반환
        foreach (AudioSource source in _sfxSources)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }
        
        // 모든 소스가 사용 중이면 첫 번째 소스를 강제로 사용
        return _sfxSources[0];
    }
}
