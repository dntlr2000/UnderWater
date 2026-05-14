using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Linq;
using UnityEngine;

public class Player : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback
{
    #region Movement Settings
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float swimSpeed = 3f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;
    public float swimUpForce = 5f;

    public float sinkSpeed = -0.3f; //가라앉는 속도

    private Vector3 initialPosition;
    private Rigidbody rb;

    private GameObject otherPlayer;
    private float pushRadius = 1.0f; //플레이어 끼리 미는 판정 거리
    private float pushForce = 3f; //플레이어끼리 미는 판정 힘
    #endregion

    #region Camera Settings
    [Header("Camera")]
    public FirstViewCamera firstViewCamera;
    public bool canMoveCamera = true;
    #endregion

    #region Environment Controllers
    [Header("Water & Visuals")]
    public UnderwaterVisualController visualController;
    public BuoyancyController buoyancyController;
    #endregion

    #region Animation
    [Header("Animation")]
    public Animator animator;

    public EngineerAnimator thirdViewAnimator;
    #endregion

    #region Player States & Layers
    [Header("States")]

    public bool isMoving; //Condition으로 옮기려 했으나 Player에 연계되는 메서드가 많아 보류
    public bool isRunning; //위와 동일

    public Condition condition;

    public float runSpeedMultiply = 3f;

    public LayerMask groundLayer;
    public LayerMask waterLayer;
    public float checkDistance = 2f;
    #endregion

    #region Job Data
    public JobData currentJob;
    public JobData[] allJobs;
    public JobType CurrentJobType => currentJob.jobType;
    public static Player localPlayer; // **유지**
    private int initialJob = -1;
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
        thirdViewAnimator = GetComponent<EngineerAnimator>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (photonView.IsMine)
        {
            localPlayer = this;
            Inventory inventory = FindAnyObjectByType<Inventory>();
            if (inventory != null)
                inventory.player = this;
        }

        if (condition == null) condition = GetComponent<Condition>();
        condition.SetCondition(this);
    }

    private void Start()
    {
        firstViewCamera.SetupLocalPlayerCamera();

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

            RaycastInteract raycastInteract = GetComponent<RaycastInteract>();
            if (raycastInteract != null) raycastInteract.enabled = true;

            StartCoroutine(condition.getHungry());
        }
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

        if (photonView.IsMine)
        {
            //stateMachine.currentState.Update();

            if (Input.GetMouseButtonDown(0)) //공격 애니메이션 출력은 별개의 스크립트/메서드해서 할듯
            {
                //stateMachine.ChangeState(new PlayerAttackState(this, stateMachine));
                Attack(1f);
            }

            if (canMoveCamera)
                firstViewCamera.RotateView();

            bool grounded = IsGrounded();

            if (Input.GetKeyDown(KeyCode.Space))
            {
                // 땅에 있을 때만 점프
                if (grounded)
                {
                    Jump();
                }

                // 점프 여부와 상관없이 '스페이스바 누르기' 퀘스트 카운트를 1 올림
                QuestManager.Instance?.ReportObjectiveProgress(ObjectiveType.PressKey, 1);
            }
            /*
            if (!grounded && !isJumping)
            {
                rb.AddForce(Vector3.down * 20f, ForceMode.Acceleration);
                //isJumping = true; //부하를 줄이기 + 어색함을 줄이기 위해 위해 상시 적용을 피하고 싶은데 지금으로선 달리 방법이 없다..
            }
            */ 
            useOxygen();
        }

    }
    private void FixedUpdate()
    {
        if (!photonView.IsMine)
        {
            rb.linearVelocity = Vector3.zero; //부들대는 현상 제거용
            return;
        }

        if (condition.isUnderwater)
            SwimMove();
        else
            GroundMove();

        if (!photonView.IsMine || !condition.CanAct(false, true, true)) return;

        condition.Run();

        if (buoyancyController != null)
        {
            SetUnderwater(buoyancyController.IsInWater());
        }

        HandlePlayerPushing();
        //condition.restoreBreath();
    }

    public void StopPhysics()
    {
        isMoving = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!photonView.IsMine) return;

        if (collision.gameObject.CompareTag("Ground"))
        {

            condition.onGround = true;
            thirdViewAnimator.RequestSetAirState(false);
        }        
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!photonView.IsMine) return;

        if (collision.gameObject.CompareTag("Ground"))
        {
            condition.onGround = false;
            thirdViewAnimator.RequestSetAirState(true);

        }
    }
    #endregion


    #region Movement Methods
    private void Jump()
    {
        //isJumping = true;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 5f, rb.linearVelocity.z);
        thirdViewAnimator.RequestSetJumpState(true);
    }

    private void GroundMove()
    {
        Vector3 input;
        if (condition.CanAct(true, true, false)) input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical")).normalized;
        else input = Vector3.zero;


        if (input.magnitude >= 0.1f)
        {
            Vector3 moveDir = Quaternion.Euler(0, firstViewCamera.cameraTransform.eulerAngles.y, 0) * input;
            Vector3 targetVelocity = moveDir * moveSpeed * (isRunning ? runSpeedMultiply : 1f);
            targetVelocity.y = rb.linearVelocity.y + gravity * Time.fixedDeltaTime;

            rb.linearVelocity = targetVelocity;
            isMoving = true; //Run 메서드와 연계
            thirdViewAnimator.RequestSetMoveState(true, isRunning);
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y + gravity * Time.fixedDeltaTime, 0);
            isMoving = false;
            thirdViewAnimator.RequestSetMoveState(false, false);
        }

    }

    private void SwimMove()
    {
        // 1. 앞뒤좌우 (수평) 입력 계산
        Vector3 input;
        if (condition.CanAct(true, true, false)) input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical")).normalized;
        else input = Vector3.zero;

        Vector3 moveDir = Quaternion.Euler(0, firstViewCamera.cameraTransform.eulerAngles.y, 0) * input;

        // 일단 수평 이동 속도만 적용
        Vector3 targetVelocity = moveDir * swimSpeed;
        targetVelocity.y = rb.linearVelocity.y;

        // 2. 위아래 (수직) 입력 계산
        float verticalInput = 0f;
        if (Input.GetKey(KeyCode.Space)) verticalInput += 1f;
        if (Input.GetKey(KeyCode.LeftControl)) verticalInput -= 1f;

        if (verticalInput > 0)
        {
            if (buoyancyController != null && !buoyancyController.IsHeadInWater() && rb.linearVelocity.y > 0)
            {
                targetVelocity.y = 0f; // 수면에서는 더 이상 안 올라감
            }
            else
            {
                targetVelocity.y = swimUpForce; // 물속에서는 위로 헤엄침
            }
        }
        else if (verticalInput < 0)
        {
            targetVelocity.y = -swimSpeed;  // 컨트롤: 아래로 헤엄쳐서 잠수함
        }
        else
        {
            // 3. 조작을 안 할 때 수면/수중 위치에 따른 처리
            if (buoyancyController != null)
            {
                if (buoyancyController.IsHeadInWater())
                {
                    // 깊은 물 속이면 우리가 설정한 sinkSpeed 로 서서히 가라앉음
                    targetVelocity.y = sinkSpeed;
                }
                else
                {
                    // 수면 근처라면 수면 유지 (가라앉지 않음)
                    if (rb.linearVelocity.y < -1f)
                    {
                        // 다이빙해서 떨어지던 속도가 있다면 물의 저항을 받아 부드럽게 감속시킴
                        targetVelocity.y = Mathf.Lerp(rb.linearVelocity.y, 0f, Time.fixedDeltaTime * 5f);
                    }
                    else
                    {
                        // 떨어지는 관성이 다 죽었거나, 헤엄치다 올라온 상태라면 Y축 속도를 0으로 만들어 수면에 띄움 (Bobbing)
                        targetVelocity.y = buoyancyController.GetBobbingVelocity();
                    }
                }
            }
        }

        rb.linearVelocity = targetVelocity;

        // 애니메이션 처리
        if (input == Vector3.zero && verticalInput == 0)
        {
            thirdViewAnimator.RequestSetMoveState(false, false);
        }
        else
        {
            thirdViewAnimator.RequestSetMoveState(true, isRunning);
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

    public void Attack(float duration = 0.5f)
    {
        if (condition.GetIsBusy()) return;
        //condition.SetInteractable(); //1회용 허가증 발행
        if (condition.BusyCoroutine != null) StopCoroutine(condition.BusyCoroutine); //공격 딜레이
        condition.BusyCoroutine = StartCoroutine(condition.workRoutine(duration));
        thirdViewAnimator.RequestSetAttackState(0);

    }
    #endregion

    #region Water & Ground Check

    public bool IsGrounded()
    {
        return condition.onGround;
    }

    private void SetUnderwater(bool underwater)
    {
        if (!photonView.IsMine || condition.isUnderwater == underwater) return;

        condition.isUnderwater = underwater;
        /*
        if (underwater)
        {
            StartCoroutine(condition.useOxygen());
        }
        else
        {
            StopCoroutine(condition.useOxygen());
        }
        */

        thirdViewAnimator.RequestSetWaterState(underwater);
    }

    private void useOxygen()
    {
        if (photonView.IsMine)
        {
            if (condition.GetHeadUnderwaterState())
            {
                condition.useOxygen(Time.deltaTime);
            }
            else
            {
                condition.ResetOxygenTickTimer();
                condition.restoreBreath(Time.deltaTime * 10f); // 원하면 자연회복도 dt 기반으로
            }
        }
    }

    #endregion

    #region Save & Sync Methods
    public PlayerData ToPlayerData()
    {
        string stableId = GetStablePlayerId(photonView.Owner);

        // 아이템 정보는 Inventory 등 다른 컴포넌트에서 가져와야 합니다. 현재는 빈 배열
        //Item[] currentItems = new Item[0];

        return new PlayerData
        {
            playerId = stableId,
            playerName = photonView.Owner.NickName,
            position = new PlayerLocation(transform.position),
            //items = currentItems,
            items = inventory,
            jobIndex = JobIndex ?? -1, // 직업이 없으면 -1 반환

            conditionData = condition != null ? condition.ToConditionData() : null
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

        string inventoryJson = "";
        if (inventory != null)
        {
            inventoryJson = JsonUtility.ToJson(inventory);
        }

        string conditionJson = "";
        if (condition != null)
            conditionJson = JsonUtility.ToJson(condition.ToConditionData());
        else Debug.LogWarning("Condition이 Null입니다");

            // 마스터 클라이언트가 아닐 때만 이벤트 전송
            object[] content = new object[]
            {
            GetStablePlayerId(PhotonNetwork.LocalPlayer), // 안정적인 ID 사용
            transform.position,
            JobIndex ?? -1,
            inventoryJson,
            conditionJson
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

    private void HandlePlayerPushing()
    {
        // pushRadius 반경에 있는 Player 레이어의 모든 콜라이더를 탐색
        // 1 << LayerMask.NameToLayer("Player")는 Player 레이어만 검사하겠다는 비트마스크
        int playerLayerMask = 1 << LayerMask.NameToLayer("Player");
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, pushRadius, playerLayerMask);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject == gameObject) continue;
            if (!hitCollider.CompareTag("Player")) continue;

            //거리와 방향 계산
            Vector3 otherPosition = hitCollider.transform.position;
            Vector3 direction = transform.position - otherPosition;
            direction.y = 0;

            float distance = direction.magnitude;

            //거리가 0이면(완벽히 겹치면) 방향을 알 수 없어 에러가 나므로 임의의 방향 설정
            if (distance == 0)
            {
                direction = Random.insideUnitSphere;
                direction.y = 0;
                distance = 0.01f;
            }

            //밀어내기 로직
            if (distance < pushRadius)
            {
                //  겹친 만큼의 비율 (0 ~ 1)
                float overlapAmount = 1f - (distance / pushRadius);

                // 겹친 정도가 클수록 더 세게 밀어냄
                Vector3 pushVector = direction.normalized * overlapAmount * pushForce * Time.fixedDeltaTime;

                if (rb != null)
                {
                    rb.MovePosition(rb.position + pushVector);
                }
            }
        }
    }

    InventoryData inventory;

    public void SyncInventory(InventoryData data)
    {
        inventory = data;
    }
}
