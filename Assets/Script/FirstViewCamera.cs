using UnityEngine;

public class FirstViewCamera : MonoBehaviour
{
    public Transform YAxisTransform; //Y축 회전
    public Transform XAxisTransform; //X축 회전

    private float mouseSensitivityX = 50f;
    private float mouseSensitivityY = 50f;
    public float MouseSensitivityX
    {
        get { return mouseSensitivityX; }
        set { mouseSensitivityX = value; }
    }
    public float MouseSensitivityY
    {
        get { return mouseSensitivityY; }
        set { mouseSensitivityX = value; }
    }

    float xRotation = 0f;


    void Start()
    {
        if (YAxisTransform == null) YAxisTransform = transform;
        
        //SwitchCursor(true);
    }

    // Update is called once per frame
    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * MouseSensitivityX * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * MouseSensitivityY * Time.deltaTime;

        xRotation -= mouseY; //X축으로 회전 : 위 아래로 쳐다보는 것으로 y축 입력을 받음
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        XAxisTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        YAxisTransform.Rotate(Vector3.up * mouseX); 
    }

    public void SwitchCursor(bool state)
    {
        if (state)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible= false;
        }

        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible= true;
        }
    }
}
