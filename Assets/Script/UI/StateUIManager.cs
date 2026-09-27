//using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StateUIManager : MonoBehaviour
{
    //public Image BackgroundBar;
    public Image FilledBar;
    //public TextMeshPro Text;
    private RectTransform originTransform;
    private float originLength;

    public float maxValue = 100f;
    private float currentValue;

    public GuageTypes guageType = GuageTypes.Bar_Horizontal;

    [Header("BlinkingWarning")]
    public Color blinkColor = Color.red;
    [SerializeField, Min(0.01f)]
    private float normalColorDuration = 0.3f; // 원래색 유지 시간

    [SerializeField, Min(0.01f)]
    private float warningColorDuration = 0.1f; // 경고색 유지 시간
    private bool isBlinking = false;
    private Color originColor;
    [SerializeField, Range(0f, 1f)] private float blinkRatio = 0.25f; //깜빡임 시작 비율
    private float blinkElapsed = 0f; //깜빡임 빈도 계산용
    //private float blinkDuration = 0.1f;
    private bool isShowingBlinkColor;
    [SerializeField] private bool useBlinkingWarning;
    [SerializeField] private GaugeWarningFrame warningFrame;




    public enum GuageTypes //단순한 형태는 필요가 없으나, 이후 일렁이는 효과를 추가하기 위해서는 필요하기 때문에 미리 추가함
    {
        Bar_Vertical,
        Bar_Horizontal,
        Circle
    }


    void Start()
    {
        currentValue = maxValue;
        originTransform = FilledBar.rectTransform;
        originColor = FilledBar.color;

        //일렁이는 효과의 이미지가 추가되면, Fill로 조절하는게 아니라, 마스크를 씌우고, 길이만큼 체력바의 위치를 내리는 방식으로 구현할 예정
        if (guageType == GuageTypes.Bar_Horizontal) originLength = originTransform.rect.width;
        else if (guageType == GuageTypes.Bar_Vertical) originLength = originTransform.rect.height;
    }

    private void Update()
    {
        if (!isBlinking || FilledBar == null)
        {
            return;
        }

        blinkElapsed += Time.deltaTime;
        while (true)
        {
            float phaseDuration = isShowingBlinkColor
                ? warningColorDuration
                : normalColorDuration;

            phaseDuration = Mathf.Max(phaseDuration, 0.01f);

            if (blinkElapsed < phaseDuration)
            {
                break;
            }

            blinkElapsed -= phaseDuration;
            isShowingBlinkColor = !isShowingBlinkColor;

            FilledBar.color = isShowingBlinkColor
                ? blinkColor
                : originColor;
        }

    }


    public void TakeDamage(float damage, bool ifHeal = false)
    {
        if (ifHeal)
        {
            currentValue += damage;
            if (currentValue > maxValue) currentValue = maxValue;
        }
        else currentValue -= damage;
        currentValue = Mathf.Clamp(currentValue, 0f, maxValue);
        UpdateBarUI();
    }

    private void UpdateBarUI()
    {
        float newAmount = currentValue / maxValue;
        //구버전
        //if (guageType == GuageTypes.Bar_Horizontal) FilledBar.rectTransform.localScale = new Vector3(newAmount, 1f, 1f);
        //else if (guageType == GuageTypes.Bar_Vertical) FilledBar.rectTransform.localScale = new Vector3(1f, newAmount, 1f);
        //else FilledBar.rectTransform.localScale = new Vector3()

        //신버전 (추후 수정 예정)
        FilledBar.fillAmount = newAmount;
        CheckUnderWarningRatio();

    }

    public void SetBarUI(float value, float maxValue = 100f)
    {
        this.maxValue = maxValue;
        currentValue = value;
        UpdateBarUI();
    }

    public void CheckUnderWarningRatio()
    {
        bool isUnderWarning;
        //isUnderWarning = (currentValue <= 0) ? false : currentValue / maxValue <= blinkRatio;
        isUnderWarning = currentValue / maxValue <= blinkRatio;

        SetBlinkingWarning(isUnderWarning);


    }

    public void SetBlinkingWarning(bool shouldBlink)
    {
        if (isBlinking == shouldBlink)
        {
            return;
        }

        isBlinking = shouldBlink;
        blinkElapsed = 0f;

        isShowingBlinkColor = false;

        if (FilledBar != null)
        {
            FilledBar.color = originColor;
        }

        // 테두리는 수치가 경고 범위인 동안 붉은 이미지로 유지합니다.
        if (warningFrame != null)
        {
            warningFrame.SetWarning(this, shouldBlink);
        }
    }

    private void OnEnable()
    {
        if (isBlinking && warningFrame != null)
        {
            warningFrame.SetWarning(this, true);
        }
    }

    private void OnDisable()
    {
        blinkElapsed = 0f;
        isShowingBlinkColor = false;

        if (FilledBar != null)
        {
            FilledBar.color = originColor;
        }


        if (warningFrame != null)
        {
            warningFrame.SetWarning(this, false);
        }
    }

}