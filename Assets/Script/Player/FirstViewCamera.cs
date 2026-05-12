using Photon.Pun;
using UnityEngine;
using System.Collections;

public class FirstViewCamera : CameraRotator
{
    public Player player;

    private Vector3 originalLocalPos;
    private Coroutine shakeCoroutine; // 무한 진동을 제어하기 위한 변수 추가

    protected override void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        FindAnyObjectByType<OptionManager>().LoadOptions();

        if (cameraPivot != null)
            originalLocalPos = cameraPivot.localPosition;
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
    public void TriggerShake(float duration = 0.5f, float magnitude = 0.1f)
    {
        StopAllCoroutines();
        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            if (cameraPivot != null)
            {
                cameraPivot.localPosition = new Vector3(originalLocalPos.x + x, originalLocalPos.y + y, originalLocalPos.z);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (cameraPivot != null)
        {
            cameraPivot.localPosition = originalLocalPos;
        }
    }

    public void StartContinuousShake(float magnitude = 0.1f)
    {
        // 이미 흔들리고 있다면 중지하고 새로 시작
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ContinuousShakeRoutine(magnitude));
    }

    public void StopContinuousShake()
    {
        // 흔들림 코루틴 강제 종료
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        // 카메라 위치를 완벽하게 원래대로 복구
        if (cameraPivot != null)
        {
            cameraPivot.localPosition = originalLocalPos;
        }
    }

    private IEnumerator ContinuousShakeRoutine(float magnitude)
    {

        float currentMagnitude = magnitude * 5.0f;
        //while(true)를 사용하여 누군가 Stop을 부를 때까지 영원히 흔들립니다.
        while (true)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            if (cameraPivot != null)
            {
                cameraPivot.localPosition = new Vector3(originalLocalPos.x + x, originalLocalPos.y + y, originalLocalPos.z);
                float rotX = Random.Range(-1f, 1f) * currentMagnitude * 2.0f; // 위아래 까닥
                float rotY = Random.Range(-1f, 1f) * currentMagnitude * 2.0f; // 좌우 까닥
                float rotZ = Random.Range(-1f, 1f) * currentMagnitude * 3.0f; // 옆으로 기우뚱

                // Mouse Y 회전값(verticalAngle)에 진동 회전값을 더해줍니다.
                cameraPivot.localRotation = Quaternion.Euler(verticalAngle + rotX, rotY, rotZ);
            }

            yield return null;
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