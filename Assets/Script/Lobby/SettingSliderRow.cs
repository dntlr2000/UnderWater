using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingSliderRow : MonoBehaviour
{
    [SerializeField] private TMP_Text _labelText;
    [SerializeField] private Slider _slider;
    [SerializeField] private TMP_Text _valueText;

    public Slider Slider => _slider;

    public void Setup(string label, float value)
    {
        if (_labelText != null) _labelText.text = label;
        if (_slider != null) _slider.SetValueWithoutNotify(value);
        UpdateValueText(value);
    }

    public void UpdateValueText(float value)
    {
        if (_valueText != null) _valueText.text = Mathf.RoundToInt(value).ToString();
    }
}