using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CursorData
{
    public string cursorType;
    public Sprite cursorSprite;
}

public class InteractionUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject crosshairUI;
    public GameObject CursorUI;
    public GameObject GaugeUI;

    [Header("Cursor")]
    public Image cursorImage;

    [SerializeField]
    private List<CursorData> cursorDataList;
    private Dictionary<string, Sprite> cursorDict;

    [Header("Gauge")]
    public Image gaugeImage;
    public Image gaugeUnfilledImage;

    private void Awake()
    {
        cursorDict = new Dictionary<string, Sprite>();

        foreach (var data in cursorDataList)
        {
            if (!cursorDict.ContainsKey(data.cursorType))
            {
                cursorDict.Add(data.cursorType, data.cursorSprite);
            }
        }
    }

    public void SetCursor(string type)
    {
        if (cursorDict == null)
        {
            Debug.LogError("cursorDict가 초기화되지 않았습니다! Awake()에서 초기화하세요.");
            return;
        }

        if (!cursorDict.TryGetValue(type, out Sprite sprite))
        {
            Debug.LogWarning($"'{type}'에 해당하는 커서 이미지가 없습니다.");
            return;
        }

        if (cursorImage == null)
        {
            Debug.LogError("cursorImage가 연결되지 않았습니다! 인스펙터에서 연결하세요.");
            return;
        }

        Debug.Log($"받은 커서 타입 : {type}");
        cursorImage.sprite = sprite;
    }

    public void ShowCursor()
    {
        //Debug.Log("############호출됨");
        crosshairUI.SetActive(false);
        CursorUI.SetActive(true);
        GaugeUI.SetActive(false);
    }

    public void ShowGauge()
    {
        crosshairUI.SetActive(false);
        CursorUI.SetActive(true);
        GaugeUI.SetActive(true);
    }

    public void UpdateGauge(float amount)
    {
        gaugeImage.fillAmount = Mathf.Clamp01(amount);
    }

    public void ResetUI()
    {
        //SetCursor("Normal");
        crosshairUI.SetActive(true);
        CursorUI.SetActive(false);
        GaugeUI.SetActive(false);

        //Debug.Log("@@@@@Reseted Icon");
    }
}
