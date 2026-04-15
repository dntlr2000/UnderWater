using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System; // Action(예약된 행동)을 쓰기 위해 반드시 필요합니다!

public class GlobalProgressBar : MonoBehaviour
{
    // 어디서든 부를 수 있게 싱글톤으로 만듭니다.
    public static GlobalProgressBar Instance;

    [Header("UI 연결")]
    public GameObject progressPanel; // 게이지바 전체를 감싸는 부모 패널 (평소엔 끄기 위함)
    public Image fillImage;          // Image Type을 'Filled'로 설정한 차오르는 이미지
    public TMP_Text actionText;      // "요리 중...", "제작 중..." 글자를 띄울 텍스트

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // 게임 시작 시 게이지바 화면에서 숨기기
        if (progressPanel != null)
        {
            progressPanel.SetActive(false);
        }
    }

    // 다른 스크립트(작업대 등)에서 이 함수를 부르면 게이지가 차오르기 시작합니다!
    // message: 화면에 띄울 텍스트
    // duration: 걸리는 시간 (초)
    // onComplete: 게이지가 100%가 되면 실행할 코드 뭉치
    public void StartProgress(string message, float duration, Action onComplete)
    {
        // 코루틴(시간이 걸리는 작업) 시작
        StartCoroutine(FillRoutine(message, duration, onComplete));
    }

    private IEnumerator FillRoutine(string message, float duration, Action onComplete)
    {
        // 1. UI 켜고 텍스트 세팅
        progressPanel.SetActive(true);
        if (actionText != null) actionText.text = message;

        // 2. 게이지 0% (텅 빈 상태)에서 시작
        if (fillImage != null) fillImage.fillAmount = 0f;

        float timePassed = 0f;

        // 3. 설정된 시간(duration) 동안 반복하며 게이지 채우기
        while (timePassed < duration)
        {
            timePassed += Time.deltaTime; // 실제 흐른 시간 누적

            // 핵심: 0.0 ~ 1.0 사이로 비율을 구해서 채워줍니다.
            if (fillImage != null) fillImage.fillAmount = timePassed / duration;

            yield return null; // 다음 프레임까지 잠시 대기 (부드러운 애니메이션을 위해 필수)
        }

        // 4. 시간이 다 지나면 오차를 없애기 위해 100%로 확실히 고정하고 UI 끄기
        if (fillImage != null) fillImage.fillAmount = 1f;
        progressPanel.SetActive(false);

        // 5. 예약해둔 보상 지급 로직(인벤토리 처리 등)을 실행!
        onComplete?.Invoke();
    }
}