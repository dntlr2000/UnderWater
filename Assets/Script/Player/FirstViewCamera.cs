using Photon.Pun;
using UnityEngine;

public class FirstViewCamera : CameraRotator
{
    public Player player;

    protected override void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
        FindAnyObjectByType<OptionManager>().LoadOptions();
    }

    // Update is called once per frame
    protected override void Update()
    {
        return;
    }

    public override void RotateView()
    {
        if (player == null) return;
        if (!player.photonView.IsMine || !player.condition.CanAct(false, true, false)) return;
        // Mouse X → 플레이어 회전
        float mouseX = Input.GetAxis("Mouse X") * MouseSensitivityX * sensivityMultiply * Time.deltaTime;
        horizontalAngle += mouseX;
        player.transform.rotation = Quaternion.Euler(0, horizontalAngle, 0);

        // Mouse Y → 카메라 상하 회전
        float mouseY = Input.GetAxis("Mouse Y") * MouseSensitivityY * sensivityMultiply * Time.deltaTime;
        verticalAngle -= mouseY;
        verticalAngle = Mathf.Clamp(verticalAngle, -89f, 89f);

        if (cameraPivot != null)
        {
            cameraPivot.localRotation = Quaternion.Euler(verticalAngle, 0f, 0f);
        }
    }

    public void SetupLocalPlayerCamera()
    {
        if (!player.photonView.IsMine)
        {
            if (cameraTransform != null && cameraTransform.GetComponent<Camera>() != null)
                cameraTransform.GetComponent<Camera>().enabled = false;

            if (cameraPivot == null) cameraPivot = transform;
            if (cameraPivot != null)
                cameraPivot.gameObject.SetActive(false);
        }
    }
    public void LockCursor(bool state)
    {
        if (state)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

}
