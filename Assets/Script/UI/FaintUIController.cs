using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FaintUIController : MonoBehaviour
{
    [SerializeField] private GameObject faintPanel;
    [SerializeField] private TMP_Text respawnTimerText;
    //[SerializeField] private Button immediateRespawnButton;
    [SerializeField] private UIController uiController;

    private Condition targetCondition;
    private bool wasVisible;

    //빈사 패널과 즉시 부활 버튼을 초기 상태로 설정합니다.
    private void Awake()
    {
        faintPanel.SetActive(false);
        //immediateRespawnButton.onClick.AddListener(OnImmediateRespawnClicked);
    }


    //로컬 플레이어의 Condition을 빈사 UI에 연결합니다.

    public void Bind(Condition condition)
    {
        targetCondition = condition;
        RefreshVisibility();
    }

    //빈사 상태와 자동 부활까지 남은 시간을 매 프레임 표시합니다.
    private void Update()
    {
        if (targetCondition == null)
        {
            return;
        }

        //RefreshVisibility();

        if (!targetCondition.GetIsFainted())
        {
            return;
        }

        int remainingSeconds = Mathf.CeilToInt(targetCondition.GetRemainingFaintTime());

        //respawnTimerText.text = $"자동 부활까지 {remainingSeconds / 60:00}:{remainingSeconds % 60:00}";
        respawnTimerText.text = $"사망까지 {remainingSeconds} 초 남았습니다.";
    }


    //현재 빈사 상태에 맞춰 패널과 플레이어 UI 입력 상태를 전환합니다.

    public void RefreshVisibility()
    {
        bool shouldShow = targetCondition != null
            && targetCondition.GetIsFainted();

        if (wasVisible == shouldShow)
        {
            return;
        }

        wasVisible = shouldShow;
        faintPanel.SetActive(shouldShow);
        //immediateRespawnButton.interactable = shouldShow;
        uiController.SetFaintUIState(shouldShow);
    }

    //즉시 부활 버튼을 잠그고 로컬 플레이어의 패널티 부활을 요청합니다.
    private void OnImmediateRespawnClicked()
    {
        if (targetCondition == null || !targetCondition.GetIsFainted())
        {
            return;
        }

        //immediateRespawnButton.interactable = false;
        targetCondition.GiveUpAndRespawn();
    }

    //등록했던 버튼 이벤트를 제거합니다.
    private void OnDestroy()
    {
        //immediateRespawnButton.onClick.RemoveListener(OnImmediateRespawnClicked);
    }

    public void RespawnButtion()
    {
        OnImmediateRespawnClicked();
    }
}