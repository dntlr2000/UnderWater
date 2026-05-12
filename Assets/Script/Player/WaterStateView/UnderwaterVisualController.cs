using UnityEngine;

public class UnderwaterVisualController : MonoBehaviour
{
    [Header("References")]
    public FogController fogController;
    public Transform cameraTransform;

    [Header("Sensors")]
    public float sensorRadius = 0.05f;
    public LayerMask waterLayer;

    [Header("Submarine Interior (물 판정 무시 구역)")]
    public LayerMask interiorLayer; // 잠수함 내부 판정용 레이어

    void Update()
    {
        if (fogController == null || cameraTransform == null) return;

        Vector3 camPos = cameraTransform.position;

        //잠수함 내부인지 먼저 체크 (내부라면 물 판정 무시하고 맑게 유지)
        bool isInsideInterior = Physics.CheckSphere(camPos, sensorRadius, interiorLayer);
        if (isInsideInterior)
        {
            if (fogController.isCameraUnderwater)
            {
                fogController.SetUnderwaterVisuals(false);
            }
            return;
        }

        //잠수함 밖이라면 정상적으로 물속인지 체크
        bool isUnder = Physics.CheckSphere(camPos, sensorRadius, waterLayer);

        if (isUnder != fogController.isCameraUnderwater)
        {
            fogController.SetUnderwaterVisuals(isUnder);
        }
    }
}