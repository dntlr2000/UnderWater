using System.Collections;
using Photon.Pun;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class Player : MonoBehaviourPunCallbacks
{
    #region Movement Settings
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float swimSpeed = 3f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;
    public float swimUpForce = 5f;

    private Rigidbody rb;
    private bool isMoving = false;
    private bool isRunning = false;
    #endregion

    #region Camera Settings
    [Header("Camera")]
    public Transform cameraTransform;
    public Transform cameraPivot;
    public float mouseSensitivityX = 500f;
    public float mouseSensitivityY = 500f;
    public bool canMoveCamera = true; //급한대로 추가함. 이후 이거와 관련된 기능 추가해야함

    private float verticalAngle;
    private float horizontalAngle;
    #endregion

    #region Animation
    [Header("Animation")]
    public Animator animator;
    public PlayerStateMachine stateMachine;
    public bool isBusy = false;
    #endregion

    #region Player States & Layers
    [Header("States")]
    public bool isUnderwater = false;
    public float runSpeedMultiply = 3f;

    public LayerMask groundLayer;
    public LayerMask waterLayer;
    public float checkDistance = 2f;
    public float waterSurfaceY = 7f;
    #endregion

    #region Player Condition
    [Header("Condition")]
    public float health = 100f;    //체력
    public float hunger = 100f;    //허기
    public float thirst = 100f;    //수분
    public float oxygen = 100f;    //산소
    public float fatigue = 0f;   //피로도
    public float stamina = 100f;   //스테미너

    private bool isSleep = false;
    //private IEnumerator UseOxygen; //수중 상태일 때 산소를 소모하기 위한 코루틴
    #endregion

    #region Job Data
    public JobData currentJob;
    public JobData[] allJobs;
    public JobType CurrentJobType => currentJob.jobType;
    public static Player localPlayer;
    #endregion

    #region UI References
    [Header("각 상태에 대응되는 바UI")]
    public StateUICollection stateUICollection;

    StateUIManager healthBar;
    StateUIManager hungerBar;
    StateUIManager thirstBar;
    StateUIManager oxygenBar;
    StateUIManager fatigueBar;
    StateUIManager staminaBar;

    #endregion

    #region Able Only Player
    //플레이어일 때 활성화
    public GameObject PlayerCanvas;
    public GameObject FirstViewLook;

    //다른 플레이어일 때 활성화
    public GameObject ThirdViewLook;
    #endregion

    //수중 상태일 때 체력이 
    #region Unity Callbacks(Awake, Start...)
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        stateMachine = new PlayerStateMachine();

        if (photonView.IsMine)
        {
            AssignRandomJob();
            QuestManager.Instance.InitQuestsForPlayer(this);
            localPlayer = this;
        }
    }

    private void Start()
    {
        SetupLocalPlayerCamera();

        if (photonView.IsMine)
        {
            QuestManager.Instance.RegisterLocalPlayer(this);
            QuestManager.Instance.InitQuestsForPlayer(this);
            PlayerCanvas.SetActive(true);
            ThirdViewLook.SetActive(false);
            FirstViewLook.SetActive(true);
            SetStateBar();
            StartCoroutine(getHungry());
            Inventory inventory = FindAnyObjectByType<Inventory>();
            inventory.player = this;
            SetUnderwater(true); // 물 상태 변경 및 산소 소모 코루틴 시작
        }

        else
        {
            PlayerCanvas.SetActive(false);
            FirstViewLook.SetActive(false);
            ThirdViewLook.SetActive(true);
            //this.enabled= false;
            Rigidbody rb = GetComponent<Rigidbody>();
            
        }

        if (currentJob != null)
            QuestManager.Instance.TryUnlockQuests(currentJob);
        else
            Debug.LogError("CurrentJob이 할당되지 않았습니다.");

        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        UpdateWaterSurfaceHeight();

        stateMachine.Initialize(new PlayerIdleState(this, stateMachine));

       
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        //if (Input.GetKeyDown(KeyCode.Tab))
        //{
        //QuestUI.Instance.ToggleQuestWindow();
        //}

        stateMachine.currentState.Update();

        if (Input.GetMouseButtonDown(0) && !isBusy)
        {
            stateMachine.ChangeState(new PlayerAttackState(this, stateMachine));
        }

        if (canMoveCamera)
            RotateView();

        if (!isBusy)
            Animate();


        if (!isUnderwater)
            Run();

        bool grounded = IsGrounded();
        // 예시: 땅에 있을 때만 점프 가능
        if (grounded && Input.GetKeyDown(KeyCode.Space))
        {
            Jump();
        }

    }
    private void FixedUpdate()
    {
        if (!photonView.IsMine || isBusy) return;

        CheckWaterState();

        if (isUnderwater)
            SwimMove();
        else
            GroundMove();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            SetUnderwater(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            SetUnderwater(false);
        }
    }
    #endregion

    #region Camera Method
    private void SetupLocalPlayerCamera()
    {
        if (!photonView.IsMine)
        {
            if (cameraTransform != null && cameraTransform.GetComponent<Camera>() != null)
                cameraTransform.GetComponent<Camera>().enabled = false;

            if (cameraPivot != null)
                cameraPivot.gameObject.SetActive(false);
        }
    }
    #endregion

    #region Movement Methods
    private void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 5f, rb.linearVelocity.z);
    }

    private void GroundMove()
    {
        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical")).normalized;

        if (input.magnitude >= 0.1f)
        {
            Vector3 moveDir = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0) * input;
            Vector3 targetVelocity = moveDir * moveSpeed * (isRunning ? runSpeedMultiply : 1f);
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

    private void SwimMove()
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
    #endregion

    #region Animation Mathods
    void Animate()
    {
        if (animator == null) return;
        float speed = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).magnitude;
        //animator.SetFloat("Speed", speed);
        //animator.SetBool("Underwater", isUnderwater);
    }
    #endregion

    #region Water & Ground Check
    private void CheckWaterState()
    {
        if (cameraTransform != null)
        {
            bool nowUnderwater = cameraTransform.position.y < waterSurfaceY;
            if (nowUnderwater != isUnderwater)
                SetUnderwater(nowUnderwater);
        }
    }
    public bool IsGrounded()
    {
        float radius = 0.3f;
        Vector3 position = transform.position + Vector3.down * 0.1f;
        return Physics.CheckSphere(position, radius, groundLayer);
    }

    private void SetUnderwater(bool underwater)
    {
        if (isUnderwater == underwater || !photonView.IsMine) return;

        isUnderwater = underwater;
        /*        Debug.Log(underwater ? "Entered water" : "Exited water");*/

        if (underwater)
            StartCoroutine(useOxygen());
        else
            StopCoroutine(useOxygen());
    }

    private void UpdateWaterSurfaceHeight()
    {
        GameObject water = GameObject.FindWithTag("Water");
        if (water != null)
        {
            Collider waterCollider = water.GetComponent<Collider>();
            waterSurfaceY = (waterCollider != null) ? waterCollider.bounds.max.y : water.transform.position.y;
        }
    }
    #endregion

    #region Status Bars & UI
    private void SetStateBar()
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
        if (!photonView.IsMine) { return; }
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
    #endregion

    #region Hunger, Oxygen, Sleep
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

        while (isUnderwater)
        {
            oxygen -= 0.1f;
            yield return new WaitForSeconds(0.1f);
            oxygenBar.SetBarUI(oxygen);

            if (oxygen <= 0)
            {
                //사망처리 필요시 구현
                break; //사?망
            }
        }
    }

    public void chargeOxygen(float amount)
    {
        oxygen += amount;
        oxygenBar.SetBarUI(oxygen);
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
    #endregion

    #region Run & Stamina
    public void Run()
    {
        if (stamina < 5f && isRunning == false)
        {
            stamina += 0.01f;
            isRunning = false;
            return; //뛸 수 없는 상태
        }

        if (Input.GetKey(KeyCode.LeftShift) && isMoving)
        {
            isRunning = true;
            stamina -= 0.1f;
            if (stamina < 0.1f) isRunning = false;
        }
        else
        {
            stamina = Mathf.Min(stamina + 0.05f, 100f);
            isRunning = false;

        }
        staminaBar.SetBarUI(stamina);
    }
    #endregion

    #region Job Assignment
    void AssignRandomJob()
    {
        if (allJobs.Length == 0)
        {
            Debug.LogError("모든 직업 데이터를 할당해주세요!");
            return;
        }

        int randomIndex = Random.Range(0, allJobs.Length);
        currentJob = allJobs[randomIndex];

        ExitGames.Client.Photon.Hashtable hash = new ExitGames.Client.Photon.Hashtable();
        hash["JobIndex"] = randomIndex;
        photonView.Owner.SetCustomProperties(hash);
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (targetPlayer == photonView.Owner && changedProps.ContainsKey("JobIndex"))
        {
            int index = (int)changedProps["JobIndex"];
            currentJob = allJobs[index];
        }
    }
    #endregion

}
