using UnityEngine;
using System.Collections.Generic;

public enum SoundType
{
    BGM,
    SFX
}

[System.Serializable]
public class Sound
{
    public string name;
    public SoundType type;
    public AudioClip clip;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Sound List")]
    public List<Sound> sounds;

    private Dictionary<string, Sound> soundDict = new Dictionary<string, Sound>();

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float volumeBGM = 0.5f;
    [Range(0f, 1f)] public float volumeSFX = 0.5f;

    private Queue<AudioSource> sfxSourcePool = new Queue<AudioSource>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // BGM Source가 할당되지 않았다면 자동으로 추가
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
        }

        // SFX Source도 자동으로 할당
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        foreach (Sound s in sounds)
        {
            if (!soundDict.ContainsKey(s.name))
                soundDict[s.name] = s;
        }

        // SFX용 AudioSource 풀 초기화
        InitializeSFXPool(10); // 풀 크기를 10으로 설정
        UpdateVolumes();
    }

    void InitializeSFXPool(int poolSize)
    {
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            sfxSourcePool.Enqueue(audioSource);
        }
    }

    void Start()
    {
        // 게임 시작 시 배경음 재생
        Play("BGM_Track1");  // BGM 이름을 Play 메서드에 전달하여 재생
    }

    public void Play(string name)
    {
        if (!soundDict.ContainsKey(name))
        {
            Debug.LogWarning("Sound not found: " + name);
            return;
        }

        Sound sound = soundDict[name];
        switch (sound.type)
        {
            case SoundType.BGM:
                bgmSource.clip = sound.clip;
                bgmSource.volume = volumeBGM;
                bgmSource.loop = true;
                bgmSource.Play();
                break;

            case SoundType.SFX:
                sfxSource.PlayOneShot(sound.clip, volumeSFX);
                break;
        }
    }

    void PlaySFX(Sound sound)
    {
        if (sfxSourcePool.Count > 0)
        {
            AudioSource sfxSource = sfxSourcePool.Dequeue(); // 풀에서 AudioSource 가져오기
            sfxSource.clip = sound.clip;
            sfxSource.volume = volumeSFX;
            sfxSource.Play();
            StartCoroutine(ReturnToPool(sfxSource, sound.clip.length)); // 재생이 끝난 후 풀로 반환
        }
        else
        {
            Debug.LogWarning("No available AudioSource in the pool!");
        }
    }

    // AudioSource를 풀로 반환하는 코루틴
    private IEnumerator<WaitForSeconds> ReturnToPool(AudioSource audioSource, float delay)
    {
        yield return new WaitForSeconds(delay);
        sfxSourcePool.Enqueue(audioSource); // 풀에 반환
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void SetVolume(SoundType type, float value)
    {
        switch (type)
        {
            case SoundType.BGM:
                volumeBGM = value;
                bgmSource.volume = volumeBGM;
                break;
            case SoundType.SFX:
                volumeSFX = value;
                break;
        }
    }

    public void UpdateVolumes()
    {
        bgmSource.volume = volumeBGM;
        // sfx는 PlayOneShot에서 직접 볼륨 전달됨
    }
}
