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
    public float MAX_FATIGUE = 100f;
    public float MAX_STAMINA = 100f;
    #endregion

    #region Player_State
    [Header("Condition")]
    public float health = 100f;    //체력
    public float hunger = 100f;    //허기
    public float thirst = 100f;    //수분
    public float oxygen = 100f;    //산소
    private float usingOxgenSpeed = 1f;
    public float fatigue = 0f;    //피로도
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
    private bool interactable = true;

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

    #region Constructors
    public Condition(Player player)
    {
        this.player = player;
        ResetCondition();
        ConnectStateBarUI();
        
    }

    public Condition(Player player, float health, float hunger, float thirst, float oxygen, float max_fatigue, float stamina)
    {
        this.player = player;

        MAX_HEALTH = health;
        MAX_HUNGER = hunger;
        MAX_THIRST = thirst;
        MAX_OXYGEN = oxygen;
        MAX_FATIGUE = max_fatigue;
        MAX_STAMINA = stamina;

        ResetCondition();
        ConnectStateBarUI();

    }
    #endregion
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
        fatigue= 0f;
        stamina= MAX_STAMINA;
    }

    public void ConnectStateBarUI()
    {
        //if (!player.photonView.IsMine) { return; }
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

        UIController uIController = FindAnyObjectByType<UIController>();
        OptionManager optionScript = FindAnyObjectByType<OptionManager>();
        if (uIController != null) uIController.playerScript = player;
        if (optionScript != null) optionScript.player = player;

        SetBarUI();
    }

    public void SetBarUI()
    {
        healthBar.SetBarUI(health, MAX_HEALTH);
        hungerBar.SetBarUI(hunger, MAX_HUNGER);
        thirstBar.SetBarUI(thirst, MAX_THIRST);
        oxygenBar.SetBarUI(oxygen, MAX_OXYGEN);
        fatigueBar.SetBarUI(fatigue, MAX_FATIGUE);
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
            fatigue += 0.5f;
            SetBarUI();
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
                    Debug.Log("산소가 부족함! 체력이 떨어지고 있음!");
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

        //DiscountOxygenDurability();
    }

    private void DiscountOxygenDurability(float value = 1f) //2번째 버전 산소통 로직
    {
        float durability = inventory.GetDurability(OxygenCylinderSlotIndex);
        //게이지가 전부 소모되면 원래 속도로 떨어지는 방식
        if (durability > 0)
        {
            //durability -= value;
            inventory.SetDurability(OxygenCylinderSlotIndex, value);
            return;
        }
        else
        {
            usingOxgenSpeed = 1f;
        } 
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
        fatigue = Mathf.Max(fatigue - value, 0);

        fatigueBar.SetBarUI(fatigue);
    }

    public IEnumerator getSleepCoroutine()
    {
        //onWork = true;
        int maxCount = 10;
        while (onWork && maxCount > 0)
        {
            yield return new WaitForSeconds(1f);
            if (onWork)
            {
                RecoverFatigue(1);
                maxCount--;
            }
        }
        onWork = false;
        yield return null;
    }
    #endregion

    public void Run()
    {
        if (stamina < 5f && isRunning == false)
        {
            stamina += 0.01f;
            isRunning = false;
            player.isRunning = false;
            return; //뛸 수 없는 상태
        }

        if (Input.GetKey(KeyCode.LeftShift) && player.isMoving)
        {
            isRunning = true;
            player.isRunning = true;
            stamina -= 0.1f;
            if (stamina < 0.1f)
            {
                isRunning = false;
                player.isRunning = false;
            }
        }
        else
        {
            stamina = Mathf.Min(stamina + 0.05f, 100f);
            isRunning = false;
            player.isRunning = false;

        }
        staminaBar.SetBarUI(stamina);
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
        MAX_FATIGUE = 100f;
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
}
