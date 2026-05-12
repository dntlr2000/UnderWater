using UnityEngine;

public class FogController : MonoBehaviour
{
    public Camera cam;

    [Header("Underwater Visuals")]
    public Color underwaterColor = new Color(0.1f, 0.4f, 0.5f, 1f); // 물속 색상 (짙은 청록색)
    [Range(0.01f, 0.2f)]
    public float underwaterDensity = 0.1f; // 물속 탁도 (숫자가 클수록 앞이 안 보임)

    // 물 밖에 나왔을 때 원래대로 돌려놓기 위한 저장소
    private Color normalFogColor;
    private float normalFogDensity;
    private bool originalFogState;
    private FogMode originalFogMode;


    private CameraClearFlags originalClearFlags; // 원래 카메라 배경 모드 저장
    private Color originalBackgroundColor;       // 원래 카메라 배경색 저장
    public bool isCameraUnderwater = false;     // 현재 카메라가 물속인지 체크


    void Start()
    {
        if (cam == null) GetComponent<Camera>();

        normalFogColor = RenderSettings.fogColor;
        normalFogDensity = RenderSettings.fogDensity;
        originalFogState = RenderSettings.fog;
        originalFogMode = RenderSettings.fogMode;

        //기본 카메라 상태 저장 (스카이박스 복구용)
        if (cam != null)
        {
            originalClearFlags = cam.clearFlags;
            originalBackgroundColor = cam.backgroundColor;
        }
    }

    public void SetUnderwaterVisuals(bool isUnder)
    {
        isCameraUnderwater = isUnder;

        if (isUnder)
        {
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = underwaterColor;
            }

            // 2. 안개를 물 색깔로 짙게 설정합니다.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = underwaterColor;
            RenderSettings.fogDensity = underwaterDensity;
        }
        else
        {
            if (cam != null)
            {
                cam.clearFlags = originalClearFlags;
                cam.backgroundColor = originalBackgroundColor;
            }

            RenderSettings.fog = originalFogState;
            RenderSettings.fogMode = originalFogMode;
            RenderSettings.fogColor = normalFogColor;
            RenderSettings.fogDensity = normalFogDensity;
        }
    }
}