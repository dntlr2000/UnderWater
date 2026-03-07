using UnityEngine;

public class UIController : MonoBehaviour
{
    private ItemUIManager itemUIManager;
    private OptionManager optionManager;
    public PauseScreen pauseScreen;
    bool pauseState = false;

    private Player playerScript;

    public QuestUI questUI;
    public ShopManager shop;
    public GameObject storageBox;

    //private bool ifMouseOn = true;

    // Update is called once per frame
    private void Start()
    {
        itemUIManager= GetComponent<ItemUIManager>();
        optionManager= GetComponent<OptionManager>();

        CheckPlayerScript();
        LockCursor(true);
    }

    void Update()
    {
        // 1. [ESC] 일시정지 및 UI 닫기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscapeInput();
        }

        // 2. [I] 인벤토리
        if (Input.GetKeyDown(KeyCode.I))
        {
            // 다른 UI가 켜져있지 않을 때만
            if (!pauseState && !questUI.isActive && !shop.ifShopOn && !storageBox.activeSelf)
            {
                ToggleInventory();
            }
        }

        // 3. [Tab] 퀘스트 UI 토글
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // 다른 UI(일시정지, 상점 등)가 없을 때만 작동
            if (!pauseState && !shop.ifShopOn && !storageBox.activeSelf && !itemUIManager.showInventory)
            {
                ToggleQuestPanel();
            }
        }

        // 4. [Space] 퀘스트 UI 닫기 (UI가 켜져 있을 때만)
        if (questUI.isActive && Input.GetKeyDown(KeyCode.Space))
        {
            ToggleQuestPanel(); // 닫기
        }
    }

    private void HandleEscapeInput()
    {
        CheckPlayerScript();

        //아이템창이 켜져 있는 경우
        if (itemUIManager.showInventory)
        {
            ToggleInventory();
            return;
        }
        //창고
        else if (storageBox.activeSelf)
        {
            SetBoxScreen(false);
            return;
        }
        //퀘스트창
        else if (questUI.isActive)
        {
            ToggleQuestPanel();
            return;
        }
        //상점
        else if (shop.ifShopOn)
        {
            SetShopScreen(false);
            return;
        }
        //설정
        else if (optionManager.ifOptionActive)
        {
            optionManager.TurnOptions(false);
            return;
        }

        //그 외 -> 일시정지 종료
        else if (!pauseState)
        {
            SetPauseScreen(true);
        }
        else
        {
            SetPauseScreen(false);
        }
    }

    public void ToggleInventory()
    {
        itemUIManager.SwitchInventoryState();

        // 인벤토리 상태에 따라 커서 및 카메라 제어
        if (itemUIManager.showInventory)
        {
            //LockCursor(false);
            SetPlayerControl(false);
        }
        else
        {
            //LockCursor(true);
            SetPlayerControl(true);
        }
    }

    public void ToggleQuestPanel()
    {
        // 퀘스트 UI 내부 상태 토글
        questUI.ToggleQuestWindow();

        // UI가 켜졌는지 꺼졌는지 확인 (Toggle 후의 상태)
        bool isOpened = questUI.isActive;

        if (isOpened)
        {
            LockCursor(false);       // 마우스 보이기
            SetPlayerControl(false); // 플레이어 조작 잠금
        }
        else
        {
            LockCursor(true);        // 마우스 숨기기
            SetPlayerControl(true);  // 플레이어 조작 해제
        }
    }

    public void SetPauseScreen(bool state)
    {
        pauseState = state;
        pauseScreen.gameObject.SetActive(state);

        if (state)
        {
            LockCursor(false);
            SetPlayerControl(false);
        }
        else
        {
            LockCursor(true);
            SetPlayerControl(true);
        }
    }

    public void SetShopScreen(bool state)
    {
        shop.ifShopOn = state;
        shop.gameObject.SetActive(state);

        if (state)
        {
            shop.UpdateMoneyData();
            //LockCursor(false);
            SetPlayerControl(false);
        }
        else
        {
            shop.DisableComfirmScreen();
            shop.ResetSlot();
            //LockCursor(true);
            SetPlayerControl(true);
        }
    }

    public void SetBoxScreen(bool state)
    {
        storageBox.SetActive(state);

        if (state)
        {
            LockCursor(false);
            SetPlayerControl(false);
        }
        else
        {
            StorageBox storage = FindAnyObjectByType<StorageBox>();
            if (storage) storage.DisableComfirmScreen();
            LockCursor(true);
            SetPlayerControl(true);
        }
    }

    public void LockCursor(bool isLocked)
    {
        if (isLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
    }


    // 플레이어 조작(시점 회전 + 이동) 제어
    public void SetPlayerControl(bool canControl)
    {
        CheckPlayerScript();
        if (playerScript != null)
        {
           
            playerScript.canMoveCamera = canControl;
            // UI가 켜져있으면 isBusy를 true로 만들어 이동/공격을 막음
            playerScript.condition.SetIsBusy(!canControl);

            // 만약 움직이는 중에 UI를 켰다면 멈추게 처리
            if (!canControl)
            {
                playerScript.StopPhysics(); // Player.cs에 추가할 함수
            }
        }
    }

    public void Rotatable(bool state)
    {
        // 하위 호환성을 위해 남겨두거나 SetPlayerControl로 대체 가능
        CheckPlayerScript();
        if (playerScript != null) playerScript.canMoveCamera = state;
    }

    private void CheckPlayerScript()
    {
        if (playerScript == null)
            playerScript = FindAnyObjectByType<Inventory>().player;
    }
}
