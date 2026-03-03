using Photon.Realtime;
using UnityEngine;

public class CameraRotator : MonoBehaviour
{
    [Header("Allocations")]
    public Transform cameraTransform;
    public Transform cameraPivot;

    [Header("Sendivity")]
    public float MouseSensitivityX = 25f;
    public float MouseSensitivityY = 25f;

    protected float verticalAngle;
    protected float horizontalAngle;

    protected float sensivityMultiply = 2f; //감도가 너무 낮은 것 같아서 배율 적용

    public bool canActivate = false;

    protected virtual void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (canActivate) RotateView();
    }

    public virtual void RotateView()
    {
        if (!canActivate) return;
        else if (cameraTransform == null) Debug.LogError("카메라가 비어있습니다!");
            // Mouse X → 플레이어 회전
        float mouseX = Input.GetAxis("Mouse X") * MouseSensitivityX * sensivityMultiply * Time.deltaTime;
        horizontalAngle += mouseX;
        //cameraPivot.rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0);

        // Mouse Y → 카메라 상하 회전
        float mouseY = Input.GetAxis("Mouse Y") * MouseSensitivityY * sensivityMultiply * Time.deltaTime;
        verticalAngle -= mouseY;
        verticalAngle = Mathf.Clamp(verticalAngle, -89f - 90f, 89f - 90f);

        if (cameraPivot != null)
        {
            cameraPivot.localRotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0);
        }
    }
}
