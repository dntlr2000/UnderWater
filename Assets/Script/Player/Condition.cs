using Photon.Pun;
using System.Collections;
using UnityEngine;

public class Condition : MonoBehaviour
{
    [Header("Dependancy_Objects")]
    public Player player;
    private Inventory inventory;

    #region Max_Values
    [Header("Condition_MAX")]
    public float MAX_HEALTH = 100f;
    public float MAX_HUNGER = 100f;
    public float MAX_THIRST = 100f;
    public float MAX_OXYGEN = 100f;
    private float humanOxygen = 100f;
    public float MAX_VITALITY = 100f;
    public float MAX_STAMINA = 100f;
    #endregion

    #region Player_State
    [Header("Condition")]
    public float health = 100f;    //체력
    public float hunger = 100f;    //허기
    public float thirst = 100f;    //수분
    public float oxygen = 100f;    //산소
    private float usingOxgenSpeed = 1f;
    public float vitality = 0f;    //피로도
    public float stamina = 100f;    //스테미너

    [Header("State")]
    //public bool isMoving = false;
    public bool isRunning = false;

    //private bool isSleep = false;
    public bool isFainted = false;
    public bool onSit = false;
    public bool isUnderwater = false;
    public bool onGround = false;

    private bool isBusy = false;
    public bool onWork = false;
    //private bool interactable = true;

    private int OxygenCylinderSlotIndex = -1;
    public Coroutine BusyCoroutine;

    [Header("Faint")]
    [SerializeField, Min(1f)] private float faintDuration = 60f;
    [SerializeField, Range(0.01f, 1f)] private float reviveHealthRatio = 0.3f; //부활 시 체력
    [SerializeField, Range(0f, 1f)] private float reviveOxygenRatio = 1f; //부활 시 산소
    [SerializeField, Min(0.1f)] private float maxReviveDistance = 10f;

    private double faintEndTime = -1d;
    private bool isFaintRequestPending;
    private bool isResolvingFaint;
    private int normalPlayerLayer;
    private int faintPlayerLayer;
    #endregion

    #region UI
    [Header("각 상태에 대응되는 바UI")]
    public StateUICollection stateUICollection;

    StateUIManager healthBar;
    StateUIManager hungerBar;
    StateUIManager thirstBar;
    StateUIManager oxygenBar;
    StateUIManager fatigueBar;
    StateUIManager staminaBar;

    #endregion

    #region Timer
    private float oxygenTickTimer = 0f;
    private const float oxygenTickInterval = 0.1f;

    #endregion

    /// <summary>
    /// 플레이어의 기본 레이어와 빈사 상호작용용 레이어를 저장합니다.
    /// </summary>
    private void Awake()
    {
        normalPlayerLayer = gameObject.layer;
        faintPlayerLayer = LayerMask.NameToLayer("Raycast");

        if (faintPlayerLayer < 0)
        {
            faintPlayerLayer = normalPlayerLayer;
            Debug.LogWarning("Raycast 레이어를 찾지 못해 기존 플레이어 레이어를 유지합니다.");
        }
    }

    public void SetCondition(Player player)
    {
        this.player = player;
        //ResetCondition();
        if (player.photonView.IsMine)
        {
            ConnectStateBarUI();
        }
    }

    void Start()
    {

    }

    /// <summary>
    /// MasterClient에서 빈사 제한시간 만료를 확인하고 패널티 부활을 요청합니다.
    /// </summary>
    void Update()
    {
        if (!isFainted || isResolvingFaint)
        {
            return;
        }

        bool canResolveTimeout = !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;
        if (canResolveTimeout && GetFaintClockTime() >= faintEndTime)
        {
            ResolvePenaltyRespawn();
        }
    }

    #region StateControllers
    public void ResetCondition()
    {
        health = MAX_HEALTH;
        hunger = MAX_HUNGER;
        thirst = MAX_THIRST;
        oxygen = MAX_OXYGEN;
        vitality = MAX_VITALITY;
        stamina = MAX_STAMINA;
    }

    public void ConnectStateBarUI()
    {
        if (!player.photonView.IsMine) { return; }

        stateUICollection = FindAnyObjectByType<StateUICollection>();
        if (stateUICollection == null) {
            Debug.LogError("StateBarUI가 연동되지 않았습니다.");
            return;
        };

        healthBar = stateUICollection.healthBar;
        hungerBar = stateUICollection.hungerBar;
        thirstBar = stateUICollection.thirstBar;
        oxygenBar = stateUICollection.oxygenBar;
        fatigueBar = stateUICollection.fatigueBar;
        staminaBar = stateUICollection.staminaBar;

        //UIController uiController = FindAnyObjectByType<UIController>();
        //OptionManager optionScript = FindAnyObjectByType<OptionManager>();
        //if (uiController != null) uiController.playerScript = player;
        //if (optionScript != null) optionScript.player = player;

        SetBarUI();
    }

    public void SetBarUI()
    {
        Debug.Log($"상태 UI 갱신 - 체력 : {health}, 허기 : {hunger}, 수분 : {thirst}, 산소 : {oxygen}, 활력 : {vitality}, 스태미너 : {stamina}");
        healthBar.SetBarUI(health, MAX_HEALTH);
        hungerBar.SetBarUI(hunger, MAX_HUNGER);
        thirstBar.SetBarUI(thirst, MAX_THIRST);
        oxygenBar.SetBarUI(oxygen, MAX_OXYGEN);
        fatigueBar.SetBarUI(vitality, MAX_VITALITY);
        staminaBar.SetBarUI(stamina, MAX_STAMINA);
    }

    /// <summary>
    /// 생존 중에만 체력을 변경하고 체력이 소진되면 빈사 상태를 요청합니다.
    /// </summary>
    public void Damaged(float value)
    {
        if (isFainted || isFaintRequestPending)
        {
            return;
        }

        health = Mathf.Clamp(health - value, 0f, MAX_HEALTH);
        //Debug.Log($"남은 체력 : {health}");
        if (health <= 0)
        {
            //사망
            //health = 0;
            //isFainted = true;
            RequestEnterFaint();
        }

        if (healthBar != null)
        {
            healthBar.SetBarUI(health, MAX_HEALTH);
        }
    }

    public IEnumerator getHungry()
    {
        while (true)
        {
            hunger -= 1f;
            thirst -= 1f; //일단 허기, 목마름, 피로 증가 매커니즘이 아예 동일할 것으로 생각되어 하나의 메서드 안에 통합
            RecoverFatigue(-0.5f);
            //SetBarUI();
            hungerBar.SetBarUI(hunger, MAX_HUNGER);
            thirstBar.SetBarUI(thirst, MAX_THIRST);
            healthBar.SetBarUI(health, MAX_HEALTH);
            yield return new WaitForSeconds(5f);
        }
    }

    public void getFood(float thirst, float hunger)
    {
        this.thirst += thirst;
        if (this.thirst > MAX_THIRST) this.thirst = MAX_THIRST;
        this.hunger += hunger;
        if (this.hunger > MAX_HUNGER) this.hunger = MAX_HUNGER;
        SetBarUI();
    }
    /*
    public IEnumerator useOxygen()
    {
        //while (isUnderwater)
        while (GetHeadUnderwaterState())
        {
            
            if (oxygen <= 0)
            {
                //사망처리 필요시 구현
                if (OxygenCylinderSlotIndex == -1) //제 기능 안될 시 싱크 맞추기용 변수 하나 만들자
                {
                    //Debug.Log("산소가 부족함! 체력이 떨어지고 있음!");
                    Damaged(1f);
                }
                else
                {
                    //산소통 내 산소 모두 소모
                    Debug.Log("산소통 내 산소 모두 소모됨!");
                    //OxygenCylinderSlotIndex = -1;
                    //ResetStateOrigin(); //다른 장비가 구현된다면 산소 부분만 빼서 붙여넣어야 할듯
                    SyncOxygenDurability();
                    LoadHumanOxygen();
                }
            }
            else
            {

                oxygen -= 1f * usingOxgenSpeed;
                oxygenBar.SetBarUI(oxygen, MAX_OXYGEN);

                SyncOxygenDurability();
            }
            yield return new WaitForSeconds(0.1f);
        }

        
    }
    */

    public void useOxygen(float deltaTime)
    {
        if (!GetHeadUnderwaterState()) return; // 기존 while 조건 대체

        oxygenTickTimer += deltaTime;

        // 프레임 드랍에도 정확히 보정되도록 while 사용
        while (oxygenTickTimer >= oxygenTickInterval)
        {
            oxygenTickTimer -= oxygenTickInterval;

            if (oxygen <= 0f)
            {
                if (OxygenCylinderSlotIndex == -1)
                {
                    Damaged(1f);
                }
                else
                {
                    Debug.Log("산소통 내 산소 모두 소모됨!");
                    SyncOxygenDurability();
                    LoadHumanOxygen();
                }
            }
            else
            {
                oxygen -= 1f * usingOxgenSpeed;
                oxygen = Mathf.Max(oxygen, 0f);
                oxygenBar.SetBarUI(oxygen, MAX_OXYGEN);
                SyncOxygenDurability();
            }
        }
    }
    public void ResetOxygenTickTimer()
    {
        oxygenTickTimer = 0f;
    }

    private void SyncOxygenDurability()
    {
        if (OxygenCylinderSlotIndex == -1)
        {
            humanOxygen = oxygen;
            return;
        }
        if (inventory == null) inventory = FindAnyObjectByType<Inventory>();

        //if (inventory.GetItemID(OxygenCylinderSlotIndex) == 5) //구버전 산소통 로직
        //{
        inventory.SetDurability(OxygenCylinderSlotIndex, oxygen);
        return;
        //}
    }

    public void chargeOxygen(float amount)
    {
        oxygen += amount;
        if (oxygen >= MAX_OXYGEN) oxygen = MAX_OXYGEN;
        oxygenBar.SetBarUI(oxygen, MAX_OXYGEN);
        SyncOxygenDurability();
    }

    public void restoreBreath(float amount = 1f) //물 밖에 있을 때는 폐활량만큼 산소량 충전
    {
        if (OxygenCylinderSlotIndex != -1) return;
        if (!CanAct(false, true, false)) return;

        if (!GetHeadUnderwaterState() && oxygen <= 100f)
        {
            oxygen += amount;
            if (oxygen > 100f) oxygen = 100f;
            oxygenBar.SetBarUI(oxygen, MAX_OXYGEN);
        }
    }

    public void RecoverFatigue(float value)
    {
        vitality = Mathf.Max(vitality + value, 0);
        vitality = Mathf.Min(vitality + value, 100);


        fatigueBar.SetBarUI(vitality);
    }

    //작업대 연결하느라 추가함
    public void ApplyGymExercise(float costHunger, float costWater, float bonusMaxHP, float bonusMaxStamina)
    {
        // 1. 수치 차감 (0 밑으로 떨어지지 않도록 방어)
        hunger -= costHunger;
        if (hunger < 0) hunger = 0f;

        thirst -= costWater;
        if (thirst < 0) thirst = 0f;

        // 2. 최대치 스탯 영구 증가
        MAX_HEALTH += bonusMaxHP;
        MAX_STAMINA += bonusMaxStamina;

        // 최대치가 늘어난 만큼 현재 체력/스테미너도 그만큼 즉시 회복시켜 줍니다.
        health += bonusMaxHP;
        stamina += bonusMaxStamina;

        // 3. 갱신된 스탯을 바탕으로 UI 다시 그리기
        SetBarUI();
        Debug.Log($"운동 완료! 현재 최대 체력: {MAX_HEALTH}, 최대 스테미너: {MAX_STAMINA}");
    }
    //작업대 연결하느라 추가함

    #endregion

    public void Run()
    {
        if (stamina < 5f && isRunning == false) //스태미나를 방전시킨 경우
        {
            stamina += 0.01f;
            isRunning = false;
            player.isRunning = false;
            return; //뛸 수 없는 상태
        }

        if (Input.GetKey(KeyCode.LeftShift) && player.isMoving) //뛰는 경우
        {
            isRunning = true;
            player.isRunning = true;
            stamina -= 0.1f;
            RecoverFatigue(-0.01f);
            if (stamina < 0.1f) //방전
            {
                isRunning = false;
                player.isRunning = false;
            }
        }
        else //뛸 수 있는데 안뛰는 경우
        {
            stamina = Mathf.Min(stamina + 0.05f, 100f);
            isRunning = false;
            player.isRunning = false;

        }
        staminaBar.SetBarUI(stamina);
        fatigueBar.SetBarUI(vitality);
    }

    public IEnumerator workRoutine(float duration)
    {
        onWork = true;
        if (duration >= 0)
        {
            yield return new WaitForSeconds(duration);
            onWork = false;
            //SetInteractable(false); //행동 가능하므로 1회용 허가증 필요 없음
        }

        //Debug.Log("행동불가 상태 해제됨");
        BusyCoroutine = null;
    }

    public bool GetIsBusy() {
        return isBusy; 
    }

    public void SetIsBusy(bool isBusy)
    {
        Debug.Log($"[Condition] SetIsBusy 값 호출됨 : {isBusy}");
        this.isBusy = isBusy;
    }

    public void ResetStateOrigin()
    {
        MAX_HEALTH = 100f;
        MAX_HUNGER = 100f;
        MAX_THIRST = 100f;
        //if (MAX_OXYGEN > 101f && isUnderwater) oxygen = 0f; //무한산소 꼼수 방지용
        usingOxgenSpeed= 1f;
        MAX_OXYGEN = 100f;
        MAX_VITALITY = 100f;
        MAX_STAMINA = 100f;
        OxygenCylinderSlotIndex = -1;
        //LoadHumanOxygen();
    }


    public void EquipEffect(int itemId, int slots, float durability= -1f) //장착중인 장비 효과 반영, 
    {
        Debug.Log($"현재 산소량: {oxygen}");
        if (itemId == -1) {
            Debug.Log($"본래 호흡으로 돌아옵니다. 잔여 산소량 : {humanOxygen}");
            LoadHumanOxygen();
            if (oxygen > MAX_OXYGEN) { oxygen = MAX_OXYGEN; }
            return;
        } 
        //else if (itemId == 5)
        //{
        //    MAX_OXYGEN = ItemDatabase.Instance.getMaxDurability(itemId);
            
        //    oxygen = durability;
        //}

        else if (ItemDatabase.Instance.GetEquipEffectType(itemId) == "oxygen")
        {
            //내구도가 없는 산소통은 장비 슬롯에 남아 있어도 활성 산소원으로 취급하지 않음
            if (durability <= 0f)
            {
                OxygenCylinderSlotIndex = -1;
                usingOxgenSpeed = 1f;
                Debug.Log("내구도가 없는 산소통이므로 활성화하지 않습니다.");
                return;
            }

            SaveHumanOxygen(oxygen); //장착 전 산소량 저장
            oxygen = durability;
            //Debug.Log($"현재 산소량은 저장됩니다 : {humanOxygen}, 새로운 산소량 : {oxygen}");
            usingOxgenSpeed = 0.5f;
            OxygenCylinderSlotIndex = slots;
        }


        if (oxygen > MAX_OXYGEN) { oxygen = MAX_OXYGEN; }
    }

    public void SaveHumanOxygen(float value)
    {
        Debug.Log($"현재 산소량은 저장됩니다 : {value}");
        humanOxygen = value;
    }

    public void LoadHumanOxygen()
    {
        OxygenCylinderSlotIndex = -1;
        //oxygen = humanOxygen;
        oxygen = Mathf.Clamp(humanOxygen, 0f, MAX_OXYGEN); //범위 초과 방지용
        usingOxgenSpeed = 1f;
        //humanOxygen = 0f;
    }

    /// <summary>
    /// 요청된 조건에 따라 현재 플레이어가 행동할 수 있는지 반환합니다.
    /// </summary>
    public bool CanAct(bool CheckBusy, bool CheckFainted, bool CheckOnWork)
    {
        bool value = true;
        if (CheckBusy && this.isBusy) value = false;
        if (CheckFainted && (this.isFainted || isFaintRequestPending)) value = false;
        if (CheckOnWork && this.onWork) value = false;

        return value;
    }

    public void ResetMove()
    {
        Rigidbody rb = player.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        //rb.angularVelocity = Vector3.zero;
    }

    public ConditionData ToConditionData()
    {
        return new ConditionData
        {
            isSaved = true,
            health = this.health,
            hunger = this.hunger,
            thirst = this.thirst,
            oxygen = this.oxygen,
            vitality = this.vitality,
            stamina = this.stamina
        };
    }

    /// <summary>
    /// 저장된 상태 수치를 복원하고 체력이 0이면 빈사 진입 흐름을 다시 시작합니다.
    /// </summary>
    public void ApplyLoadedData(ConditionData data)
    {
        if (data == null) return;

        this.health = data.health;
        this.hunger = data.hunger;
        this.thirst = data.thirst;
        this.oxygen = data.oxygen;
        this.vitality = data.vitality;
        this.stamina = data.stamina;

        // 데이터 덮어쓴 후 UI 즉시 갱신
        SetBarUI();

        Debug.Log("저장된 플레이어 상태(Condition) 복구 완료!");

        if (health <= 0f && player != null && player.photonView.IsMine)
        {
            RequestEnterFaint();
        }
    }


    public bool GetHeadUnderwaterState()
    {
        return player.visualController.fogController.GetUnderwaterState();
    }

    /// <summary>
    /// 체력이 소진된 로컬 플레이어가 MasterClient에 빈사 진입을 요청합니다.
    /// </summary>
    private void RequestEnterFaint()
    {
        if (isFainted || isFaintRequestPending || player == null)
        {
            return;
        }

        if (player.photonView != null && !player.photonView.IsMine)
        {
            return;
        }

        isFaintRequestPending = true;

        if (!PhotonNetwork.InRoom || player.photonView == null)
        {
            ApplyFaint(GetFaintClockTime() + faintDuration);
            return;
        }

        player.photonView.RPC(nameof(PunRPC_RequestEnterFaint), RpcTarget.MasterClient);
    }

    /// <summary>
    /// 빈사 요청의 발신자가 해당 플레이어의 소유자인지 확인합니다.
    /// </summary>
    private bool IsRequestFromOwner(PhotonMessageInfo info)
    {
        return info.Sender != null
            && player != null
            && player.photonView.Owner != null
            && info.Sender.ActorNumber == player.photonView.Owner.ActorNumber;
    }

    /// <summary>
    /// MasterClient가 소유자의 빈사 요청을 검증하고 모든 클라이언트에 반영합니다.
    /// </summary>
    [PunRPC]
    private void PunRPC_RequestEnterFaint(PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient || !IsRequestFromOwner(info) || isFainted)
        {
            return;
        }

        double endTime = PhotonNetwork.Time + faintDuration;
        player.photonView.RPC(nameof(PunRPC_ApplyFaint), RpcTarget.AllBuffered, endTime);
    }

    /// <summary>
    /// 동기화된 종료 시각을 사용해 빈사 상태를 모든 클라이언트에 반영합니다.
    /// </summary>
    [PunRPC]
    private void PunRPC_ApplyFaint(double endTime)
    {
        ApplyFaint(endTime);
    }

    /// <summary>
    /// 빈사 수치와 표현을 현재 플레이어 복제본에 적용합니다.
    /// </summary>
    private void ApplyFaint(double endTime)
    {
        health = 0f;
        faintEndTime = endTime;
        isFaintRequestPending = false;
        isResolvingFaint = false;
        SetFaint(true);
        RefreshLocalStateUI();
    }

    /// <summary>
    /// 다른 플레이어가 빈사 플레이어의 구조를 MasterClient에 요청합니다.
    /// </summary>
    public void RequestRevive(Player reviver)
    {
        if (!isFainted
            || isResolvingFaint
            || reviver == null
            || reviver == player
            || reviver.condition == null
            || reviver.condition.isFainted)
        {
            return;
        }

        if (!PhotonNetwork.InRoom || player == null || player.photonView == null)
        {
            if (Vector3.Distance(transform.position, reviver.transform.position) <= maxReviveDistance)
            {
                ApplyRevive();
            }
            return;
        }

        player.photonView.RPC(
            nameof(PunRPC_RequestRevive),
            RpcTarget.MasterClient,
            reviver.photonView.ViewID);
    }

    /// <summary>
    /// MasterClient가 구조자 소유권과 거리, 양쪽 플레이어 상태를 검증합니다.
    /// </summary>
    [PunRPC]
    private void PunRPC_RequestRevive(int reviverViewId, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient || !isFainted || isResolvingFaint)
        {
            return;
        }

        PhotonView reviverView = PhotonView.Find(reviverViewId);
        Player reviver = reviverView != null ? reviverView.GetComponent<Player>() : null;

        bool isValidReviver = reviver != null
            && reviver != player
            && reviver.condition != null
            && !reviver.condition.isFainted
            && reviverView.Owner != null
            && info.Sender != null
            && reviverView.Owner.ActorNumber == info.Sender.ActorNumber
            && Vector3.Distance(transform.position, reviver.transform.position) <= maxReviveDistance;

        if (!isValidReviver)
        {
            return;
        }

        isResolvingFaint = true;
        player.photonView.RPC(nameof(PunRPC_ApplyRevive), RpcTarget.AllBuffered);
    }

    /// <summary>
    /// 검증된 구조 결과를 모든 클라이언트의 플레이어 복제본에 적용합니다.
    /// </summary>
    [PunRPC]
    private void PunRPC_ApplyRevive()
    {
        ApplyRevive();
    }

    /// <summary>
    /// 빈사 플레이어를 현 위치에서 적은 체력과 최소 산소량으로 회복시킵니다.
    /// </summary>
    private void ApplyRevive()
    {
        if (!isFainted)
        {
            isResolvingFaint = false;
            return;
        }

        health = Mathf.Min(MAX_HEALTH, Mathf.Max(1f, MAX_HEALTH * reviveHealthRatio));
        oxygen = Mathf.Clamp(
            Mathf.Max(oxygen, MAX_OXYGEN * reviveOxygenRatio),
            0f,
            MAX_OXYGEN);

        faintEndTime = -1d;
        isFaintRequestPending = false;
        isResolvingFaint = false;
        SetFaint(false);
        RefreshLocalStateUI();

        if (player != null && player.photonView.IsMine)
        {
            SyncOxygenDurability();
            player.ForceSyncState();
        }
    }

    /// <summary>
    /// 빈사 상태의 로컬 플레이어가 즉시 패널티 부활을 선택합니다.
    /// </summary>
    public void GiveUpAndRespawn()
    {
        if (!isFainted || isResolvingFaint || player == null || !player.photonView.IsMine)
        {
            return;
        }

        RequestPenaltyRespawn();
    }

    /// <summary>
    /// 빈사 포기 요청을 MasterClient에 전달하거나 오프라인에서 즉시 처리합니다.
    /// </summary>
    private void RequestPenaltyRespawn()
    {
        if (!isFainted || isResolvingFaint)
        {
            return;
        }

        if (!PhotonNetwork.InRoom || player == null || player.photonView == null)
        {
            ResolvePenaltyRespawn();
            return;
        }

        player.photonView.RPC(nameof(PunRPC_RequestPenaltyRespawn), RpcTarget.MasterClient);
    }

    /// <summary>
    /// MasterClient가 플레이어 소유자의 패널티 부활 요청을 검증합니다.
    /// </summary>
    [PunRPC]
    private void PunRPC_RequestPenaltyRespawn(PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient
            || !IsRequestFromOwner(info)
            || !isFainted
            || isResolvingFaint)
        {
            return;
        }

        ResolvePenaltyRespawn();
    }

    /// <summary>
    /// 시간 만료 또는 포기를 하나의 패널티 부활 처리로 확정합니다.
    /// </summary>
    private void ResolvePenaltyRespawn()
    {
        if (!isFainted || isResolvingFaint)
        {
            return;
        }

        isResolvingFaint = true;
        Vector3 respawnPosition = FindRespawnPosition();

        if (PhotonNetwork.InRoom && player != null && player.photonView != null)
        {
            player.photonView.RPC(
                nameof(PunRPC_ApplyPenaltyRespawn),
                RpcTarget.AllBuffered,
                respawnPosition);
            return;
        }

        ApplyPenaltyRespawn(respawnPosition);
    }

    /// <summary>
    /// 확정된 패널티 부활 위치와 결과를 모든 클라이언트에 적용합니다.
    /// </summary>
    [PunRPC]
    private void PunRPC_ApplyPenaltyRespawn(Vector3 respawnPosition)
    {
        ApplyPenaltyRespawn(respawnPosition);
    }

    /// <summary>
    /// 소유자의 아이템을 제거하고 상태를 초기화한 뒤 부활 지점으로 이동합니다.
    /// </summary>
    private void ApplyPenaltyRespawn(Vector3 respawnPosition)
    {
        if (!isFainted)
        {
            isResolvingFaint = false;
            return;
        }

        bool isLocalOwner = player != null && player.photonView.IsMine;

        if (isLocalOwner)
        {
            if (inventory == null)
            {
                inventory = FindAnyObjectByType<Inventory>();
            }

            if (inventory != null)
            {
                inventory.LoseAllItemsOnDeath();
            }
            else
            {
                Debug.LogWarning("패널티 부활 중 로컬 인벤토리를 찾지 못했습니다.");
            }
        }

        ResetConditionForRespawn();

        if (isLocalOwner)
        {
            player.TeleportTo(respawnPosition);
        }

        faintEndTime = -1d;
        isFaintRequestPending = false;
        SetFaint(false);
        isResolvingFaint = false;
        RefreshLocalStateUI();

        if (isLocalOwner)
        {
            player.ForceSyncState();
        }
    }

    /// <summary>
    /// 패널티 부활에 필요한 상태값과 진행 중 행동을 초기화합니다.
    /// </summary>
    private void ResetConditionForRespawn()
    {
        if (BusyCoroutine != null && player != null)
        {
            player.StopCoroutine(BusyCoroutine);
            BusyCoroutine = null;
        }

        ResetCondition();
        humanOxygen = Mathf.Min(100f, MAX_OXYGEN);
        usingOxgenSpeed = 1f;
        OxygenCylinderSlotIndex = -1;
        oxygenTickTimer = 0f;
        isBusy = false;
        onWork = false;
        isRunning = false;

        if (player != null)
        {
            player.isRunning = false;
            ResetMove();
        }
    }

    /// <summary>
    /// InGameManager에 설정된 패널티 부활 위치를 조회합니다.
    /// </summary>
    private Vector3 FindRespawnPosition()
    {
        InGameManager inGameManager = FindAnyObjectByType<InGameManager>();
        if (inGameManager != null)
        {
            return inGameManager.GetRespawnPosition();
        }

        Debug.LogWarning("InGameManager를 찾지 못해 기본 좌표에서 부활합니다.");
        return new Vector3(0f, 7f, 0f);
    }

    /// <summary>
    /// 로컬 플레이어에게 연결된 상태 UI만 안전하게 갱신합니다.
    /// </summary>
    private void RefreshLocalStateUI()
    {
        if (player == null || !player.photonView.IsMine || healthBar == null)
        {
            return;
        }

        SetBarUI();
    }

    /// <summary>
    /// 네트워크 방에서는 공유 시간을, 그 외에는 로컬 시간을 반환합니다.
    /// </summary>
    private double GetFaintClockTime()
    {
        return PhotonNetwork.InRoom ? PhotonNetwork.Time : Time.timeAsDouble;
    }

    /// <summary>
    /// 빈사 UI가 표시할 남은 제한시간을 초 단위로 반환합니다.
    /// </summary>
    public float GetRemainingFaintTime()
    {
        if (!isFainted)
        {
            return 0f;
        }

        return Mathf.Max(0f, (float)(faintEndTime - GetFaintClockTime()));
    }

    /// <summary>
    /// 빈사 상태 여부를 상호작용 및 UI 코드에 제공합니다.
    /// </summary>
    public bool GetIsFainted()
    {
        return isFainted;
    }

    /// <summary>
    /// 빈사 상태의 이동, 레이어, 다운 애니메이션 표현을 함께 적용합니다.
    /// </summary>
    public void SetFaint(bool value)
    {
        isFainted = value;
        if (isFainted)
        {
            ResetMove();
            //추후 기절 시 행동 불가 상태로 전환하는 로직 추가 가능
        }

        gameObject.layer = isFainted ? faintPlayerLayer : normalPlayerLayer; //상호작용을 위해 임시로 Raycast 레이어로 변경

        if (player != null && player.thirdViewAnimator != null)
        {
            player.thirdViewAnimator.SetDown(isFainted);
        }
    }
}
