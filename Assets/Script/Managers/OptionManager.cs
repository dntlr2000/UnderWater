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

    public float sensitivityMultiplyValue = 10; // 감도 배율

    //public FirstViewCamera playerCamera; //플레이어 마우스 감도 조정용
    public Player player;
    public AudioManager audioScript; //오디오 매니저의 볼륨 조정용. 현재 본인 컴퓨터에는 해당 스크립트가 존재하지 않아 연동 불가

    public SaveController saveController;


    //일단 테스트용으로 여기다가 임시로 구현, 이후 적절한 곳에 옮기면서 설정창 활성화 로직을 수정할 수 있음
    //이후 일시정지창을 구현한 후에 옮기는 것이 좋아보임
    public GameObject OptionScreen;
    public bool ifOptionActive = false;


    void Start()
    {
        //LockCursor(true);
        //OptionScreen.SetActive(false);
        //LoadOptions();
        //ApplyOptions();
    }

    private void Awake()
    {
        audioScript = FindAnyObjectByType<AudioManager>();
    }

    private void SaveOptions()
    {
        xValue = xSlider.value;
        yValue = ySlider.value;
        BgmValue = BGMSlider.value;
        SFXValue = SFXSlider.value;
        saveController.SaveOptions(xValue, yValue, BgmValue, SFXValue);
        
    }

    public void LoadOptions() 
    {
        SaveController.Options options = saveController.LoadOptions();

        if (options == null)
        {
            Debug.LogWarning("경로에 파일이 없습니다.");
            return;
        }

        xValue = options.SensivityX;
        yValue = options.SensivityY;
        BgmValue = options.BGMVolume;
        SFXValue = options.BGMVolume;

        xSlider.value = xValue;
        ySlider.value = yValue;
        BGMSlider.value = BgmValue;
        SFXSlider.value = SFXValue;

        //Debug.Log($"설정 불러오기! : x감도 : {player.firstViewCamera.MouseSensitivityX}, y감도 : {player.firstViewCamera.MouseSensitivityY}");
        ApplyOptions();

    }

    public void TurnOptions(bool state)
    {
        if (!state)
        {
            //LockCursor(true);
            SaveOptions();
            ApplyOptions();
            ifOptionActive = false;
            OptionScreen.SetActive(false);
            //player.canMoveCamera = true;
        }

        else
        {
            ifOptionActive = true;
            OptionScreen.SetActive(true);
            //audioManager.PlaySFX(0)//여는 효과음 추가
            //LockCursor(false);
            LoadOptions();   
        }
        //audioManager.PlaySFX(1)//닫는 효과음 추가
    }


    /* 여긴 설정창을 종료한 후에 반영해도 문제가 없음, 부하를 줄이기 위해 슬라이더 별로 적용 메서드를 나눔
    public void OnXSliderChanged(float value) 
    {
        //playerCamera.MouseSensitivityX = value;
    }
    public void OnYSliderChanged(float value) 
    {
        //playerCamera.MouseSensitivityY = value;
    }
     */
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
        Vector2 sensivity = GetSensivity();
        FindPlayer();
        if (player == null) Debug.LogError("플레이어가 할당되지 않은 상태에서 설정이 적용되었습니다!");
        player.firstViewCamera.MouseSensitivityX = sensivity.x;
        player.firstViewCamera.MouseSensitivityY = sensivity.y;


        audioScript.SetVolume(SoundType.BGM, BgmValue);
        audioScript.SetVolume(SoundType.SFX, SFXValue);

        Debug.Log($"설정 적용 완료! : x감도 : {player.firstViewCamera.MouseSensitivityX}, y감도 : {player.firstViewCamera.MouseSensitivityY}");
    }

    public Vector2 GetSensivity()
    {
        Vector2 sensivity;
        sensivity.x = xValue * sensitivityMultiplyValue;
        sensivity.y = yValue * sensitivityMultiplyValue;

        return sensivity;
    }

    void FindPlayer()
    {
        if (player == null) player = FindAnyObjectByType<Inventory>().player;
    }

}
