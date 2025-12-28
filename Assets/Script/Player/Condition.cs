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

    private bool isSleep = false;
    public bool isFainted = false;
    public bool onSit = false;
    public bool isUnderwater = false;
    public bool onGround = false;

    private bool isBusy = false;
    private bool interactable = true;

    private int OxygenCylinderSlotIndex;
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
        health -= Mathf.Min(health, 0);
        if (health <= 0)
        {
            //사망
            //health = 0;
            isFainted = true;
        }
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
                Damaged(1f);
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
        if (OxygenCylinderSlotIndex == -1) return;
        if (inventory == null) inventory = FindAnyObjectByType<Inventory>();

        if (inventory.GetItemID(OxygenCylinderSlotIndex) == 5) //구버전 산소통 로직
        {
            inventory.SetDurability(OxygenCylinderSlotIndex, oxygen);
            return;
        }

        DiscountOxygenDurability();
    }

    private void DiscountOxygenDurability(float value = 1f)
    {
        float durability = inventory.GetDurability(OxygenCylinderSlotIndex);
        if (durability > 0)
        {
            durability -= value;
            inventory.SetDurability(OxygenCylinderSlotIndex, durability);
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
        //Player의 FixedUpdate에 넣을 것
        if (!isUnderwater && oxygen <= 100f)
        {
            oxygen += 1f;
            if (oxygen > 100f) oxygen = 100f;
            oxygenBar.SetBarUI(oxygen, MAX_OXYGEN);
        }
    }

    public IEnumerator getSleepCoroutine()
    {
        isBusy = true;
        while (isSleep)
        {
            yield return new WaitForSeconds(1f);
            if (isSleep)
            {
                fatigue -= 1f;
                fatigueBar.SetBarUI(fatigue);
            }
        }
        isBusy = false;
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

    /*
    public void Attack(float duration = 0.5f)
    {
        if (isBusy) return;
        if (BusyCoroutine != null) StopCoroutine(BusyCoroutine);
        BusyCoroutine = StartCoroutine(BusyRoutine(duration));
    }
    */ //생성자로 생성된 객체인 경우 이 메서드를 사용하려고 하면 에러가 발생하기 때문에 임시조치. 추후 완전 제거 가능성 있음

    public bool GetIsBusy() {
        return isBusy; 
    }

    /*
    public void SetInteractable(bool state = true) //isBusy가 참이어도 상호작용에 쓰일 1회용 허가증같은거. 상호작용과 플레이어가 아예 분리되어 있기 때문에 추후 병합 시 필요없어짐
    {
        interactable = state;
    }
    public bool CheckInterable() 
    {
        if (interactable == true)
        {
            interactable = false;
            return true;
        }

        else return false;
    }
    */

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
    }

    public void EquipEffect(int itemId, int slots, float durability= -1f) //장착중인 장비 효과 반영, 
    {
        if (itemId == -1) {
            //if (MAX_OXYGEN > 101f && isUnderwater) oxygen = 0f;
        } 
        else if (itemId == 5)
        {
            MAX_OXYGEN = ItemDatabase.Instance.getMaxDurability(itemId);
            OxygenCylinderSlotIndex = slots;
            oxygen = durability;
        }

        else if (itemId == 6)
        {
            OxygenCylinderSlotIndex = slots;
            usingOxgenSpeed = 0.1f;
        }

        if (oxygen > MAX_OXYGEN) { oxygen = MAX_OXYGEN; }
    }


}
