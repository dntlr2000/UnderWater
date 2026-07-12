using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusPanel : WatchPanelBase
{
    [SerializeField] private Slider _hpBar;
    [SerializeField] private Slider _oxygenBar;
    [SerializeField] private Slider _durabilityBar;
    [SerializeField] private Slider _saturationBar;

    [SerializeField] private TextMeshProUGUI _hpText;
    [SerializeField] private TextMeshProUGUI _oxygenText;
    [SerializeField] private TextMeshProUGUI _durabilityText;
    [SerializeField] private TextMeshProUGUI _saturationText;

    private void Awake()
    {
        /*PanelType = WatchPanelType.Status;*/
    }

    public override void RefreshData()
    {
        SetBar(_hpBar, _hpText, 72f, 100f);
        SetBar(_oxygenBar, _oxygenText, 58f, 100f);
        SetBar(_durabilityBar, _durabilityText, 40f, 100f);
        SetBar(_saturationBar, _saturationText, 90f, 100f);
    }

    private void SetBar(Slider bar, TextMeshProUGUI label, float value, float max)
    {
        if (bar != null)
        {
            bar.maxValue = max;
            bar.value = value;
        }
        if (label != null)
            label.text = $"{(int)value}";
    }
}