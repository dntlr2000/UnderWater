using Photon.Pun;
using UnityEngine;

public class SubmarineOutside : MonoBehaviourPun
{
    public Player player;
    CameraRotator camRotator;
    GameObject cameraObj;

    public float moveSpeed = 5f;
    public float rotationSpeed = 0.5f;

    public Rigidbody rb;

    public bool controllable = false;

    float frontSpeed = 0f;
    float turnSpeed = 0f;
    float verticalSpeed = 0f;

    public Handle ConnectedHandle;
    public int usingPlayerId = -1;

    public bool onWaterState = false;

    void Start()
    {
        camRotator = GetComponent<CameraRotator>();
        cameraObj = camRotator.cameraTransform.gameObject;
        cameraObj.SetActive(false);
        rb = GetComponent<Rigidbody>();
        PhotonView pv = GetComponent<PhotonView>();
    }

    // Update is called once per frame

    private void Update()
    {
        if (controllable && Input.GetKeyDown(KeyCode.E)) //잠수함 상태 벗어나기
        {
            if (ConnectedHandle == null) return;
            if (usingPlayerId != ConnectedHandle.player.photonView.ViewID) return;

            rb.linearVelocity = Vector3.zero;
            SwitchSubmarineState(false);
            DisconnectHandle();
        }
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine) return;
        if (controllable && usingPlayerId == ConnectedHandle.player.photonView.ViewID)
        {
            if (onWaterState) //수중 상태
            {
                //입력 및 속도 계산
                float turnInput = Input.GetAxis("Horizontal"); //좌우 회전
                turnSpeed = CaculateMovement(turnInput, turnSpeed, rotationSpeed, 0.05f);

                float forwardInput = Input.GetAxis("Vertical"); //앞뒤
                frontSpeed = CaculateMovement(forwardInput, frontSpeed, moveSpeed);

                float verticalInput = 0f;
                if (Input.GetKey(KeyCode.Space)) verticalInput += 1f;
                if (Input.GetKey(KeyCode.LeftControl)) verticalInput -= 1f;
                verticalSpeed = CaculateMovement(verticalInput, verticalSpeed, moveSpeed / 2);
                rb.angularVelocity = new Vector3(0, turnSpeed, 0f);
                rb.linearVelocity = (transform.forward * frontSpeed) + (Vector3.up * verticalSpeed);
            }
            else //바깥으로 나온 상태
            {
                turnSpeed = CaculateMovement(0, turnSpeed, rotationSpeed, 0.05f);
                frontSpeed = CaculateMovement(0, frontSpeed, moveSpeed);
                verticalSpeed = rb.linearVelocity.y;
                rb.angularVelocity = new Vector3(0, turnSpeed, 0f);
                //rb.linearVelocity = (transform.forward * frontSpeed);
            }
        }

        else
        {
            if (onWaterState) //수중 상태
            {
                //입력 및 속도 계산
                turnSpeed = CaculateMovement(0, turnSpeed, rotationSpeed, 0.05f);

                frontSpeed = CaculateMovement(0, frontSpeed, moveSpeed);

                verticalSpeed = CaculateMovement(0, verticalSpeed, moveSpeed / 2);
                rb.angularVelocity = new Vector3(0, turnSpeed, 0f);
                rb.linearVelocity = (transform.forward * frontSpeed) + (Vector3.up * verticalSpeed);
            }
            else //바깥으로 나온 상태
            {
                turnSpeed = CaculateMovement(0, turnSpeed, rotationSpeed, 0.05f);
                frontSpeed = CaculateMovement(0, frontSpeed, moveSpeed);
                verticalSpeed = rb.linearVelocity.y;
                rb.angularVelocity = new Vector3(0, turnSpeed, 0f);
            }
        }

        
    }

    private void OnTriggerEnter(Collider other)
    {
        //if (!photonView.IsMine) return;
        if (other.CompareTag("Water"))
        {
            SetWaterState(true);
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            SetWaterState(false);
        }
    }

    public void SwitchSubmarineState(bool state)
    {
        if (state) //참 -> 잠수함 조종 상태
        {
            if (!photonView.IsMine)
            {
                photonView.RequestOwnership(); //PhotonTransformView - Takeover로 설정 -> 해당 오브젝트의 포톤오너 변경
                //->PhotonTransformView의 동기화 권한 옮기기가 가능함
            }
            controllable = true;
            player.gameObject.SetActive(false); //이후 움직임 통제하는 것으로 기능 교체 예정
            cameraObj.SetActive(true);
            camRotator.canActivate = true;

            OptionManager optionManager = FindAnyObjectByType<OptionManager>();
            if (optionManager != null)
            {
                Vector2 sensivity = optionManager.GetSensivity();
                camRotator.MouseSensitivityX = sensivity.x;
                camRotator.MouseSensitivityY = sensivity.y;
            }
        }
        else
        {
            if (controllable == false) return;
            camRotator.canActivate = false;
            controllable = false;
            player.gameObject.SetActive(true);
            cameraObj.SetActive(false);
        }
    }

    public void ConnectHandle(Handle toConnect)
    {
        ConnectedHandle = toConnect;
        usingPlayerId = ConnectedHandle.usingPlayerID;
    }

    public void DisconnectHandle()
    {
        ConnectedHandle.RequestSetUsing(false, -1);
        ConnectedHandle = null;
        usingPlayerId = -1;
    }

    float CaculateMovement(float InputParameter, float moveVariable, float maxValue, float correctionValue = 0.1f)
    {
        if (InputParameter > 0) moveVariable = Mathf.Min(maxValue, moveVariable + correctionValue * InputParameter);
        else if (InputParameter < 0) moveVariable = Mathf.Max(-maxValue, moveVariable + correctionValue * InputParameter);
        else
        {
            if (moveVariable > 0) moveVariable = Mathf.Max(0, moveVariable - correctionValue);
            else if (moveVariable < 0) moveVariable = Mathf.Min(0, moveVariable + correctionValue);
        }

        return moveVariable;
    }

    void SetWaterState(bool state)
    {
        onWaterState = state;
        rb.useGravity = !state;
    }
}


