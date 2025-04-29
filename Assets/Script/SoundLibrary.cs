using UnityEngine;

public class SoundLibrary : MonoBehaviour
{
    //private float VolumeSFX = 1f;
    //private float VolumeBGM = 1f;

    public float volumeSFX
    {
        get; //return VolumeSFX;
        private set; //VolumeSFX = value;
    } = 1f;
    public float volumeBGM
    {
        get; //return VolumeBGM;
        private set; //VolumeBGM = value;
    } = 1f;
    //getter, setter의 주석을 남겨둔 이유 : 옵션 설계를 어떻게 해야할지 아직 구상이 된 것이 아니기 때문에 주석친 내용으로 되돌아갈 가능성이 있음.

    public AudioClip[] BackgroundMusics;
    public AudioClip[] SFXs;

    private static SoundLibrary instance; // 싱글톤 인스턴스

    public AudioSource BGMSource; //카메라의 오디오소스를 할당하면 될듯

    void Awake()
    {
        // 기존 인스턴스가 있으면 현재 오브젝트 삭제 (중복 방지)
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject); // 씬 이동 시 삭제되지 않음
    }

    public AudioClip GetSFX(int index)
    {
        return SFXs[index];
    }

    //AudioSource에 자신의 Source나 타 오브젝트의 source를 전달하여 재생할 위치를 지정해야 함
    public void PlaySFX(int index, AudioSource source)
    {
        source.volume = volumeSFX;

        source.PlayOneShot(SFXs[index]);
    }

    public void PlaySFX(int index)
    {
        BGMSource.volume = volumeSFX;

        BGMSource.PlayOneShot(SFXs[index]);
    }

    public void PlayBGM(int index, AudioSource source)
    {
        source.volume = volumeBGM;

        source.clip = BackgroundMusics[index];
        source.Play();
        source.loop = true;
    }
    public void PlayBGM(int index) //컴포넌트눈 매개변수의 기본값으로 전달하지 못하는 것 같아 오버로딩으로 구현
    {
        BGMSource.volume = volumeBGM;

        BGMSource.clip = BackgroundMusics[index];
        BGMSource.Play();
        BGMSource.loop = true;
    }

    public void StopBGM(AudioSource source)
    {
        source.Stop();
    }

    public void ChangeVolume_SFX(float value) //바깥에서 값을 조절할 수 있게 수정하면 그냥 이 메서드가 필요없지 않을까?
    {
        volumeSFX = value;
    }

    public void ChangeVolume_BGM(float value)
    {
        volumeBGM = value;
        BGMSource.volume = volumeBGM;
    }

}
