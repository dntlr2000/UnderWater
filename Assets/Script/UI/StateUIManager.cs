//using TMPro;
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

        //일렁이는 효과의 이미지가 추가되면, Fill로 조절하는게 아니라, 마스크를 씌우고, 길이만큼 체력바의 위치를 내리는 방식으로 구현할 예정
        if (guageType == GuageTypes.Bar_Horizontal) originLength = originTransform.rect.width;
        else if (guageType == GuageTypes.Bar_Vertical) originLength = originTransform.rect.height;
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
    }

    public void SetBarUI(float value, float maxValue = 100f)
    {
        this.maxValue = maxValue;
        currentValue = value;
        UpdateBarUI();
    }

}
