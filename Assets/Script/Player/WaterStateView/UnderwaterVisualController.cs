using UnityEngine;

public class UnderwaterVisualController : MonoBehaviour
{
    [Header("References")]
    public FogController fogController;
    public Transform cameraTransform;

    [Header("Sensors")]
    public float sensorRadius = 0.05f;
    public LayerMask waterLayer;

    [Header("Submarine Interior (�� ���� ���� ����)")]
    public LayerMask interiorLayer; // ����� ���� ������ ���̾�

    void Update()
    {
        if (fogController == null || cameraTransform == null) return;

        Vector3 camPos = cameraTransform.position;

        //����� �������� ���� üũ (���ζ�� �� ���� �����ϰ� ���� ����)
        bool isInsideInterior = Physics.CheckSphere(camPos, sensorRadius, interiorLayer);
        if (isInsideInterior)
        {
            if (fogController.isCameraUnderwater)
            {
                fogController.SetUnderwaterVisuals(false);
            }
            return;
        }

        //����� ���̶�� ���������� �������� üũ
        bool isUnder = Physics.CheckSphere(camPos, sensorRadius, waterLayer);

        if (isUnder != fogController.isCameraUnderwater)
        {
            fogController.SetUnderwaterVisuals(isUnder);
        }
    }
}
