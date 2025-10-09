using Photon.Pun;
using UnityEngine;

public class SubmarineOutside : MonoBehaviourPun
{
    public Player player;
    public Camera SubmarineCamera;
    Camera PlayerCamera;

    public float moveSpeed = 5f;
    //public float swimUpForce = 5f;
    //public float rotationSpeed = 10f;

    Rigidbody rb;

    bool controllable = false;

    void Start()
    {
        SubmarineCamera.gameObject.SetActive(false);
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (controllable && Input.GetKeyDown(KeyCode.E)) //잠수함 상태 벗어나기
        {
            rb.linearVelocity = Vector3.zero;
            //rb.angularVelocity = Vector3.zero;
            SwitchSubmarineState(false);
        }

        if (controllable)
        {
            //Player의 Swim()에서 가져옴
            Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical")).normalized;
            float verticalInput = 0f;

            // Space: 위로, Ctrl: 아래로
            if (Input.GetKey(KeyCode.Space)) verticalInput += 1f;
            if (Input.GetKey(KeyCode.LeftControl)) verticalInput -= 1f;

            Vector3 moveDir = Quaternion.Euler(0, 1, 0) * input;
            moveDir += Vector3.up * verticalInput;

            rb.linearVelocity = moveDir.normalized * moveSpeed;

            //잠수함 회전 
        }

        
    }

    public void SwitchSubmarineState(bool state)
    {
        if (state) //참 -> 잠수함 조종 상태
        {
            controllable = true;
            player.gameObject.SetActive(false); //이후 움직임 통제하는 것으로 기능 교체 예정
            SubmarineCamera.gameObject.SetActive(true);
        }
        else
        {
            controllable = false;
            player.gameObject.SetActive(true);
            SubmarineCamera.gameObject.SetActive(false);
        }
    }
}
