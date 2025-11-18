using System.Collections;
using Photon.Realtime;
using Photon.Pun;
using UnityEngine;
using ExitGames.Client.Photon;
using System.Linq;

public class Player : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback
{
    #region Movement Settings
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float swimSpeed = 3f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;
    public float swimUpForce = 5f;

    private Vector3 initialPosition;
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
    public bool canMoveCamera = true;

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
    public float fatigue = 0f;    //피로도
    public float stamina = 100f;    //스테미너

    private bool isSleep = false;
    #endregion

    #region Job Data
    public JobData currentJob;
    public JobData[] allJobs;
    public JobType CurrentJobType => currentJob.jobType;
    public static Player localPlayer; // **유지**
    private int initialJob = -1;
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

    private float syncTimer;
    #endregion

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
            localPlayer = this;
        }
    }

    private void Start()
    {
        SetupLocalPlayerCamera();

        if (photonView.IsMine)
        {
            PlayerCanvas.SetActive(true);
            FirstViewLook.SetActive(true);
            ThirdViewLook.SetActive(false);

            // 초기 직업 적용 (OnPhotonInstantiate에서 설정된 initialJob 사용)
            if (initialJob >= 0)
            {
                SetJob(initialJob);
                // JobIndex 속성 덕분에 아래 로직은 SetJob 내부에서 CustomProperties를 사용하는 것으로 대체될 수 있습니다.
                // PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "JobIndex", initialJob } }); 
            }

            SetStateBar();
            StartCoroutine(getHungry());


            RaycastInteract raycastInteract = GetComponent<RaycastInteract>();
            if (raycastInteract != null) raycastInteract.enabled = true;
        }
        else
        {
            PlayerCanvas.SetActive(false);
            FirstViewLook.SetActive(false);
            ThirdViewLook.SetActive(true);

            RaycastInteract raycastInteract = GetComponent<RaycastInteract>();
            if (raycastInteract != null) raycastInteract.enabled = false;
        }

        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        UpdateWaterSurfaceHeight();
        stateMachine.Initialize(new PlayerIdleState(this, stateMachine));
    }

    private void Update()
    {
        if (photonView.IsMine && PhotonNetwork.InRoom)
        {
            syncTimer += Time.deltaTime;
            // 1초마다 전송하여 마스터 클라이언트의 SaveManager가 캐시하도록 함
            if (syncTimer >= 1f)
            {
                syncTimer = 0f;
                SendStateToMaster();
            }
        }

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

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] data = photonView.InstantiationData;
        if (data == null || data.Length < 2)
        {
            Debug.LogWarning($"[{photonView.Owner.NickName}] InstantiationData가 비어있습니다.");
            return;
        }

        Vector3 spawnPos = (Vector3)data[0];
        int jobIndex = (int)data[1];

        // 위치 초기화
        transform.position = spawnPos;

        if (rb != null)
        {
            rb.position = spawnPos;
            rb.linearVelocity = Vector3.zero; // Rigidbody 초기화
            rb.angularVelocity = Vector3.zero;
        }

        // 직업 정보 적용
        if (allJobs != null && jobIndex >= 0 && jobIndex < allJobs.Length)
        {
            initialJob = jobIndex;
            currentJob = allJobs[jobIndex];
            Debug.Log($"[{photonView.Owner.NickName}] 초기 직업 설정 완료: {currentJob.jobName}");
        }
        else
        {
            Debug.LogWarning($"[{photonView.Owner.NickName}] 유효하지 않은 JobIndex: {jobIndex}");
        }

        // 내 로컬 플레이어일 때만 카메라와 UI 활성화
        if (photonView.IsMine)
        {
            if (PlayerCanvas != null) PlayerCanvas.SetActive(true);
            if (FirstViewLook != null) FirstViewLook.SetActive(true);
            if (ThirdViewLook != null) ThirdViewLook.SetActive(false);

            Debug.Log($"[{PhotonNetwork.LocalPlayer.NickName}] 내 로컬 플레이어 카메라 활성화");
        }
        else
        {
            if (PlayerCanvas != null) PlayerCanvas.SetActive(false);
            if (FirstViewLook != null) FirstViewLook.SetActive(false);
            if (ThirdViewLook != null) ThirdViewLook.SetActive(true);
        }

        Debug.Log($"[{photonView.Owner.NickName}] 스폰 완료 - 위치: {spawnPos}, JobIndex: {jobIndex}");
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

    #region Save & Sync Methods
    public PlayerData ToPlayerData()
    {
        string stableId = GetStablePlayerId(photonView.Owner);

        // 아이템 정보는 Inventory 등 다른 컴포넌트에서 가져와야 합니다. 현재는 빈 배열
        Item[] currentItems = new Item[0];

        return new PlayerData
        {
            playerId = stableId,
            playerName = photonView.Owner.NickName,
            position = new PlayerLocation(transform.position),
            items = currentItems,
            jobIndex = JobIndex ?? -1, // 직업이 없으면 -1 반환
        };
    }

    // SaveManager와 동일한 ID 확인 로직 사용
    private string GetStablePlayerId(Photon.Realtime.Player p)
    {
        if (p == null) return "Unknown";
        // AuthManager 및 SaveManager에서 사용하는 UserId를 사용해야 합니다.
        if (!string.IsNullOrEmpty(p.UserId)) return p.UserId;
        if (p.ActorNumber > 0) return $"Actor_{p.ActorNumber}";
        if (!string.IsNullOrEmpty(p.NickName)) return p.NickName;
        return $"Unknown_{p.ActorNumber}";
    }

    // 플레이어의 현재 상태를 마스터 클라이언트에게 전송하여 SaveManager가 캐시하도록 함 (이벤트 코드 101)
    private void SendStateToMaster()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // 마스터 클라이언트는 스스로를 캐시에 업데이트합니다.
            SaveManager.Instance?.UpdatePlayerCache(this.ToPlayerData());
            return;
        }

        // 마스터 클라이언트가 아닐 때만 이벤트 전송
        object[] content = new object[]
        {
            GetStablePlayerId(PhotonNetwork.LocalPlayer), // 안정적인 ID 사용
            transform.position,
            JobIndex ?? -1
        };

        PhotonNetwork.RaiseEvent(
            eventCode: 101, // SaveManager에서 이 코드를 구독하고 있습니다.
            eventContent: content,
            raiseEventOptions: new Photon.Realtime.RaiseEventOptions
            {
                Receivers = Photon.Realtime.ReceiverGroup.MasterClient
            },
            sendOptions: ExitGames.Client.Photon.SendOptions.SendReliable
        );
    }
#endregion

    #region Job Assignment
    // SaveManager의 GetSavedJob()과 호환되는 JobIndex 프로퍼티
    public int? JobIndex
    {
        get
        {
            if (photonView.Owner.CustomProperties.TryGetValue("JobIndex", out object jobIndexObj))
                return (int)jobIndexObj;
            return null;
        }
    }

    // SaveManager가 호출하여 직업을 설정하는 메서드
    public void SetJob(int jobIndex)
    {
        if (jobIndex < 0 || jobIndex >= allJobs.Length)
        {
            Debug.LogError("[Player] Invalid JobIndex: " + jobIndex);
            return;
        }

        currentJob = allJobs[jobIndex];

        // 직업 인덱스를 Custom Properties에 저장하여 다른 클라이언트에게 동기화
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable { { "JobIndex", jobIndex } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        // QuestManager.Instance?.TryUnlockQuests(currentJob); // Optional: null check
        Debug.Log($"[Player] Job set: {currentJob.jobName}");
    }

    // SaveManager가 위치를 로드할 때 호출하는 메서드
    public void TeleportTo(Vector3 newPos)
    {
        if (!photonView.IsMine) return;

        rb.position = newPos;
        rb.linearVelocity = Vector3.zero; // 순간이동이므로 속도 초기화
        transform.position = newPos;
    }
    // ... (OnPlayerPropertiesUpdate, JobSetting 등 기존 Job 로직 유지) ...
    #endregion
}
