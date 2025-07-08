using System.Collections;
using Photon.Pun;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class Player : MonoBehaviourPun
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float swimSpeed = 3f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;
    public float swimUpForce = 5f;

    [Header("Camera")]
    public Transform cameraTransform;
    public Transform cameraPivot;
    public float mouseSensitivityX = 500f;
    public float mouseSensitivityY = 500f;
    public bool canMoveCamera = true; //급한대로 추가함. 이후 이거와 관련된 기능 추가해야함

    [Header("Animation")]
    public Animator animator;
    public PlayerStateMachine stateMachine;
    public bool isBusy = false;

    [Header("States")]
    public bool isUnderwater = false;
    //private IEnumerator UseOxygen; //수중 상태일 때 산소를 소모하기 위한 코루틴
    public float runSpeedMultiply = 3f;

    private Rigidbody rb;
    private float verticalAngle;
    private float horizontalAngle;

    public LayerMask groundLayer;
    public LayerMask waterLayer;
    public float checkDistance = 2f;
    [SerializeField] private float waterSurfaceY = 7f;

    [Header("Condition")]
    public float health = 100f;    //체력
    public float hunger = 100f;    //허기
    public float thirst = 100f;    //수분
    public float oxygen = 100f;    //산소
    public float fatigue = 0f;   //피로도
    public float stamina = 100f;   //스테미너

    private bool isSleep = false;
    private bool isMoving = false; //뛰는 로직 구현하기 위해 필요
    private bool Running = false;
    

    [Header("각 상태에 대응되는 바UI")]
    public StateUICollection stateUICollection;

    StateUIManager healthBar;
    StateUIManager hungerBar;
    StateUIManager thirstBar;
    StateUIManager oxygenBar;
    StateUIManager fatigueBar;
    StateUIManager staminaBar;

    //수중 상태일 때 체력이 

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        stateMachine = new PlayerStateMachine();
    }
    private void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        GameObject water = GameObject.FindWithTag("Water");
        if (water != null)
        {
            Collider waterCollider = water.GetComponent<Collider>();
            if (waterCollider != null)
            {
                waterSurfaceY = waterCollider.bounds.max.y;
            }
            else
            {
                waterSurfaceY = water.transform.position.y;
            }
        }

        stateMachine.Initialize(new PlayerIdleState(this, stateMachine, "Idle"));

        if (!photonView.IsMine)
        {
            if (cameraTransform != null)
                cameraTransform.gameObject.SetActive(false);

            if (cameraPivot != null)
                cameraPivot.gameObject.SetActive(false);
        }

        SetStateBar();
        StartCoroutine(getHungry());
        changeWaterState(true); //산소 메커니즘을 테스트하기 위해 임시로 Start에 배치
    }

    private void Update()
    {
        //if (photonView == null) Debug.LogError("photonView가 비어있습니다.");
        if (!photonView.IsMine) return;

        stateMachine.currentState.Update();

        if (canMoveCamera) RotateView();

        if (!isBusy)
        {
            Animate();
        }

        if(!isUnderwater) Run();

    }
    private void FixedUpdate()
    {
        if (!photonView.IsMine) return;

        bool grounded = IsGrounded();

        // 예시: 땅에 있을 때만 점프 가능
        if (grounded && Input.GetKeyDown(KeyCode.Space))
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 5f, rb.linearVelocity.z);
        }
        if (!isBusy)
        {
            CheckWaterState();

            if (isUnderwater)
                SwimMove();
            else
                GroundMove();
        }
    }
    void RotateView()
    {
        // Mouse X → 플레이어 회전
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX * Time.deltaTime;
        horizontalAngle += mouseX;
        transform.rotation = Quaternion.Euler(0, horizontalAngle, 0);

        // Mouse Y → 카메라 상하 회전
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY * Time.deltaTime;
        verticalAngle -= mouseY;
        verticalAngle = Mathf.Clamp(verticalAngle, -89f, 89f);

        if (cameraPivot != null)
        {
            cameraPivot.localRotation = Quaternion.Euler(verticalAngle, 0f, 0f);
        }
    }

    void GroundMove()
    {
        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical")).normalized;

        if (input.magnitude >= 0.1f)
        {
            Vector3 moveDir = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0) * input;
            Vector3 targetVelocity;
            if (Running) targetVelocity = moveDir * moveSpeed * runSpeedMultiply;
            else targetVelocity = moveDir * moveSpeed;
            
            targetVelocity.y = rb.linearVelocity.y + gravity * Time.fixedDeltaTime;

            rb.linearVelocity = targetVelocity;
            isMoving = true; //Run 메서드와 연계
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y + gravity * Time.fixedDeltaTime, 0);
            isMoving = false;
        }
    }
    void SwimMove()
    {
        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical")).normalized;
        float verticalInput = 0f;

        // Space: 위로, Ctrl: 아래로
        if (Input.GetKey(KeyCode.Space)) verticalInput += 1f;
        if (Input.GetKey(KeyCode.LeftControl)) verticalInput -= 1f;

        Vector3 moveDir = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0) * input;
        moveDir += Vector3.up * verticalInput;

        rb.linearVelocity = moveDir.normalized * swimSpeed;
    }
    void Animate()
    {
        if (animator == null) return;
        float speed = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).magnitude;
        //animator.SetFloat("Speed", speed);
        //animator.SetBool("Underwater", isUnderwater);
    }

    // 상태 관련 메서드 --------------------------------------------------------------------------

    private void CheckWaterState()
    {
        if (cameraTransform != null)
        {
            isUnderwater = cameraTransform.position.y < waterSurfaceY;
        }
    }
    public bool IsGrounded()
    {
        float radius = 0.3f;
        Vector3 position = transform.position + Vector3.down * 0.5f;
        return Physics.CheckSphere(position, radius, groundLayer);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            isUnderwater = true;
            Debug.Log("Entered water");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            isUnderwater = false;
            Debug.Log("Exited water");
        }
    }

    void SetStateBar()
    {
        stateUICollection = FindAnyObjectByType<StateUICollection>();
        if (stateUICollection == null) return;
        
        healthBar = stateUICollection.healthBar;
        hungerBar = stateUICollection.hungerBar;
        thirstBar = stateUICollection.thirstBar;
        oxygenBar = stateUICollection.oxygenBar;
        fatigueBar = stateUICollection.fatigueBar;
        staminaBar = stateUICollection.staminaBar;
        
        UIController uIController = FindAnyObjectByType<UIController>();
        OptionManager optionScript = FindAnyObjectByType<OptionManager>();
        if (uIController != null) uIController.playerScript = this;
        if (optionScript != null) optionScript.player = this;

        SetBarUI();
    }

    public void SetBarUI()
    {
        healthBar.SetBarUI(health);
        hungerBar.SetBarUI(hunger);
        thirstBar.SetBarUI(thirst);
        oxygenBar.SetBarUI(oxygen);
        fatigueBar.SetBarUI(fatigue);
        staminaBar.SetBarUI(stamina);
    }

    public void Damaged(float value)
    {
        health -= value;
        healthBar.SetBarUI(health);
    }

    public IEnumerator getHungry()
    {
        while (true)
        {
            hunger -= 1f;
            thirst -= 1f; //일단 허기, 목마름, 피로 증가 매커니즘이 아예 동일할 것으로 생각되어 하나의 메서드 안에 통합
            fatigue += 0.5f;
            SetBarUI();
            yield return new WaitForSeconds(5f);
        }
    }

    public void getFood(float thirst, float hunger)
    {
        this.thirst += thirst;
        this.hunger += hunger;
        SetBarUI();
    }

    public IEnumerator useOxygen()
    {
        //산소 관련 주석을 남겨둔 이유: 좀 더 효율적인 방법이나 현재 구조로 문제가 발생할 경우 이전 구조로 되돌리기 쉽게 남겨둠. 이후에도 작동에 문제 없으면 삭제 예정
        //oxygen -= 0.1f;
        //yield return new WaitForSeconds(0.1f);
        
        while (isUnderwater )
        {
            oxygen -= 0.1f;
            yield return new WaitForSeconds(0.1f);
            oxygenBar.SetBarUI(oxygen);

            if (oxygen <= 0) break; //사?망
        }
    }
    public void chargeOxygen(float amount)
    {
        oxygen += amount;
        oxygenBar.SetBarUI(oxygen);
    }
    public void changeWaterState(bool ifWater)
    {
        if (ifWater)
        {
            isUnderwater = true;
            //UseOxygen = useOxygen();
            //StartCoroutine(UseOxygen);
            StartCoroutine(useOxygen());
        }
        else
        {
            isUnderwater = false;
            //StopCoroutine(UseOxygen);
        }
    }

    public IEnumerator getSleepCoroutine()
    {
        while (isSleep)
        {
            yield return new WaitForSeconds(1f);
            if (isSleep)
            {
                fatigue -= 1f;
                fatigueBar.SetBarUI(fatigue);
            }
        }
    }

    public void Run()
    {
        if (stamina < 5f && Running == false)
        {
            stamina += 0.01f;
            Running = false;
            return; //뛸 수 없는 상태
        }

        if (Input.GetKey(KeyCode.LeftShift) && isMoving == true)
        {
            Running = true;
            stamina -= 0.1f;
            if (stamina < 0.1f) Running = false;
        }
        else
        {
            if (stamina >= 100f) stamina = 100f;
            else stamina += 0.05f;
            Running = false;
          
        }
        staminaBar.SetBarUI(stamina);
        return;
    }
}
