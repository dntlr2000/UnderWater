using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GaugeWarningFrame : MonoBehaviour
{
    [Header("Image")]
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite warningSprite;

    [Header("Raw Image")]
    [SerializeField] private RawImage targetRawImage;
    [SerializeField] private Texture warningTexture;

    private Sprite originalSprite;
    private Texture originalTexture;

    // 허기와 수분처럼 하나의 테두리를 공유하는 상태를 함께 관리합니다.
    private readonly HashSet<StateUIManager> warningSources =
        new HashSet<StateUIManager>();

    /// <summary>
    /// 현재 사용 중인 일반 이미지 또는 텍스처를 저장합니다.
    /// </summary>
    private void Awake()
    {
        if (targetImage != null)
        {
            originalSprite = targetImage.sprite;
        }

        if (targetRawImage != null)
        {
            originalTexture = targetRawImage.texture;
        }
    }

    /// <summary>
    /// 요청한 게이지의 경고 상태를 등록하고 최종 테두리 상태를 갱신합니다.
    /// </summary>
    public void SetWarning(StateUIManager source, bool isWarning)
    {
        if (source == null)
        {
            return;
        }

        if (isWarning)
        {
            warningSources.Add(source);
        }
        else
        {
            warningSources.Remove(source);
        }

        ApplyWarningVisual(warningSources.Count > 0);
    }

    /// <summary>
    /// Image 또는 RawImage에 일반·경고 이미지를 적용합니다.
    /// </summary>
    private void ApplyWarningVisual(bool showWarning)
    {
        if (targetImage != null)
        {
            targetImage.sprite =
                showWarning && warningSprite != null
                    ? warningSprite
                    : originalSprite;
        }

        if (targetRawImage != null)
        {
            targetRawImage.texture =
                showWarning && warningTexture != null
                    ? warningTexture
                    : originalTexture;
        }
    }

    /// <summary>
    /// UI가 다시 활성화되면 현재 경고 요청 상태를 복구합니다.
    /// </summary>
    private void OnEnable()
    {
        ApplyWarningVisual(warningSources.Count > 0);
    }

    /// <summary>
    /// UI가 비활성화될 때 일반 이미지로 복구합니다.
    /// </summary>
    private void OnDisable()
    {
        ApplyWarningVisual(false);
    }
}