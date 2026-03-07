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

    void Update()
    {
        
    }

    #region StateControllers
    public void ResetCondition()
    {
        health= MAX_HEALTH;
        hunger= MAX_HUNGER;
        thirst= MAX_THIRST;
        oxygen= MAX_OXYGEN;
        vitality = MAX_VITALITY;
        stamina= MAX_STAMINA;
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

    public void Damaged(float value)
    {
        health -= value;
        health = Mathf.Max(health, 0);
        //Debug.Log($"남은 체력 : {health}");
        if (health <= 0)
        {
            //사망
            //health = 0;
            isFainted = true;
            ResetMove();
        }
        healthBar.SetBarUI(health, MAX_HEALTH);
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

    public IEnumerator useOxygen()
    {
        while (isUnderwater)
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

    public void restoreBreath() //물 밖에 있을 때는 폐활량만큼 산소량 충전
    {
        if (OxygenCylinderSlotIndex != -1) return;

        if (!isUnderwater && oxygen <= 100f)
        {
            oxygen += 1f;
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

    public IEnumerator BusyRoutine(float duration)
    {
        isBusy = true;
        if (duration >= 0)
        {
            yield return new WaitForSeconds(duration);
            isBusy = false;
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

        else if (itemId == 6)
        {
            SaveHumanOxygen(oxygen); //장착 전 산소량 저장
            oxygen = durability;
            //Debug.Log($"현재 산소량은 저장됩니다 : {humanOxygen}, 새로운 산소량 : {oxygen}");
            usingOxgenSpeed = 0.5f;
        }
        OxygenCylinderSlotIndex = slots;


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
        oxygen = humanOxygen;
        usingOxgenSpeed = 1f;
        //humanOxygen = 0f;
    }

    public bool CanAct(bool CheckBusy, bool CheckFainted, bool CheckOnWork)
    {
        bool value = true;
        if (CheckBusy && this.isBusy) value = false;
        if (CheckFainted && this.isFainted) value = false;
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
    }
}
