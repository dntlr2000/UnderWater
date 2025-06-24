//using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StateUIManager : MonoBehaviour
{
    //public Image BackgroundBar;
    public RawImage FilledBar;
    //public TextMeshPro Text;

    public float maxValue = 100f;
    private float currentValue;


    void Start()
    {
        currentValue = maxValue;

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
        FilledBar.rectTransform.localScale = new Vector3(newAmount, 1f, 1f);
    }

    public void SetBarUI(float value, float maxValue = 100f)
    {
        this.maxValue = maxValue;
        currentValue = value;
        UpdateBarUI();
    }

}
