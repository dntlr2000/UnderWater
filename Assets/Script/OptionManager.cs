using UnityEngine;
using UnityEngine.UI;

public class OptionManager : MonoBehaviour
{
    public Slider xSlider;
    public Slider ySlider;
    public Slider BGMSlider;
    public Slider SFXSlider;

    float xValue = 50f;
    float yValue = 50f;
    float BgmValue = 50f;
    float SFXValue = 50f;

    public FirstViewCamera playerCamera; //플레이어 마우스 감도 조정용
    public AudioManager audioScript; //오디오 매니저의 볼륨 조정용. 현재 본인 컴퓨터에는 해당 스크립트가 존재하지 않아 연동 불가

    public SaveController saveController;


    //일단 테스트용으로 여기다가 임시로 구현, 이후 적절한 곳에 옮기면서 설정창 활성화 로직을 수정할 수 있음
    //이후 일시정지창을 구현한 후에 옮기는 것이 좋아보임
    public GameObject OptionScreen;
    private bool ifOptionActive = false;


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (ifOptionActive == false) //옵션 활성화
            {
                ifOptionActive = true;
                OptionScreen.SetActive(true);
                //audioManager.PlaySFX(0)//여는 효과음 추가
                playerCamera.LockCursor(false);
                LoadOptions();
            }

            else //옵션 비활성화
            {
                ExitOptions();
            }

        }
    }

    private void Awake()
    {
        //LoadOptions();
        if(ifOptionActive == false)
        {
            playerCamera.LockCursor(true);
        }
    }

    private void SaveOptions() // 버튼을 이용해서 저장을 할 경우 public으로 전환 바람
    {
        xValue = xSlider.value;
        yValue = ySlider.value;
        BgmValue = BGMSlider.value;
        SFXValue = SFXSlider.value;
        saveController.SaveOptions(xValue, yValue, BgmValue, SFXValue);
        
    }

    private void LoadOptions() //// 버튼을 이용해서 불러올 경우 public으로 전환 바람
    {
        SaveController.Options options = saveController.LoadOptions();

        if (options == null)
        {
            Debug.LogWarning("경로에 파일이 없습니다.");
            return;
        }

        float xValue = options.SensivityX;
        float yValue = options.SensivityY;
        float BgmValue = options.BGMVolume;
        float SFXValue = options.BGMVolume;

        xSlider.value = xValue;
        ySlider.value = yValue;
        BGMSlider.value = BgmValue;
        SFXSlider.value = SFXValue;

        ApplyOptions();

    }

    public void ExitOptions()
    {
        playerCamera.LockCursor(true);
        SaveOptions();
        ApplyOptions();
        ifOptionActive = false;
        OptionScreen.SetActive(false);
        //audioManager.PlaySFX(1)//닫는 효과음 추가
    }



    public void OnXSliderChanged(float value) 
    {
        playerCamera.MouseSensitivityX = value;
    }
    public void OnYSliderChanged(float value) 
    {
        playerCamera.MouseSensitivityY = value;
    }
     
    public void OnBGMChanged(float value) //슬라이더 변경 시 실시간 반영을 위해
    {
        //audioScript.ChangeVolume_BGM(value);
        audioScript.SetVolume(SoundType.BGM, BgmValue);
    }
    public void OnSFXChanged(float value) //슬라이더 변경 시 실시간 반영을 위해
    {
        //audioScript.ChangeVolume_BGM(value);
        audioScript.SetVolume(SoundType.SFX, SFXValue);
    }

    private void ApplyOptions() //전체 적용
    {
        playerCamera.MouseSensitivityX = xValue;
        playerCamera.MouseSensitivityY = yValue;
        //audioScript.ChangeVolume_BGM(BGMValue);
        //audioScript.ChangeVolume_SFX(SFXValue);
        audioScript.SetVolume(SoundType.BGM, BgmValue);
        audioScript.SetVolume(SoundType.SFX, SFXValue);
    }
}
