using UnityEngine;

public class UIController : MonoBehaviour
{
    private ItemUIManager itemUIManager;
    private OptionManager optionManager;
    public PauseScreen pauseScreen;
    bool pauseState = false;

    public Player playerScript;

    public QuestUI questUI;
    public ShopManager shop;
    public GameObject storageBox;

    //private bool ifMouseOn = true;

    // Update is called once per frame
    private void Start()
    {
        itemUIManager= GetComponent<ItemUIManager>();
        optionManager= GetComponent<OptionManager>();
        LockCursor(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CheckPlayerScript();
            if (!pauseState) //옵션이 비활성화되어 있을 때
            {
                if (itemUIManager.showInventory)
                { //아이템창이 활성화되어 있으면 아이템 창 닫고 종료
                    itemUIManager.SwitchInventoryState();
                    playerScript.canMoveCamera = true;
                    return;
                }


                else if (storageBox.activeSelf)
                {
                    SetBoxScreen(false);
                    return;
                }

                
                //optionManager.TurnOptions(true); //설정창 활성화
                SetPauseScreen(true);
                playerScript.canMoveCamera = false;


            }

            else //일시정지가 활성화되어있을 때
            {
                //optionManager.TurnOptions(false);

                if (itemUIManager.showInventory) //아이템창이 활성화되어 있으면 (아마 버그가 아닌 이상 지나칠 조건)
                { //아이템창이 활성화되어 있으면 카메라 움직임 정지 유지
                    playerScript.canMoveCamera = false;
                    return;
                }

                else if (optionManager.ifOptionActive) //옵션이 활성화되어있을 때
                {
                    optionManager.TurnOptions(false);
                    return;
                }

                else if (questUI.isActive)
                {
                    TurnQuestPanel(false);
                }

                else if (shop.ifShopOn)
                {
                    SetShopScreen(false);
                }

                else
                {
                    playerScript.canMoveCamera = true;
                    SetPauseScreen(false);
                    return;
                }
                
            }

        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            if (itemUIManager.showInventory == false && optionManager.ifOptionActive == false)
            {
                itemUIManager.SwitchInventoryState();
                playerScript.canMoveCamera = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            //QuestUI.Instance.ToggleQuestWindow();
            //questUI.ToggleQuestWindow();
            if (!questUI.isActive)
            {
                TurnQuestPanel(true);
                LockCursor(false);
            }
            else
            {
                TurnQuestPanel(false);
                LockCursor(true);

            }
        }
    }

    public void Rotatable(bool state)
    {
        playerScript.canMoveCamera = state;
    }


    public void LockCursor(bool state)
    {
        if (state)
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

    public void SetPauseScreen(bool state)
    {
        if (state)
        {
            pauseScreen.gameObject.SetActive(true);
            LockCursor(false);
            pauseState = true;
            Rotatable(false);
        }

        else
        {
            pauseScreen.gameObject.SetActive(false);
            LockCursor(true);
            pauseState = false;
            Rotatable(true);
        }
    }

    public void TurnQuestPanel(bool state)
    {
        //ToggleQuestWindow를 최대한 보존한 채로 사용하려다보니 구조가 복잡해짐 이후 구조 개편을 허가 받으면 수정할 예정
        if (state) //state이 true -> isActive = false -> ToggleQuerstWindow가 false를 기준으로 동작 -> 이후 isActive를 뒤집어서 정정
        {
            //QuestUI.Instance.gameObject.SetActive(true);
            //QuestUI.Instance.isActive = false;
            
            //questUI.gameObject.SetActive(true);
            questUI.isActive = false;
        }
        else
        {
            //QuestUI.Instance.gameObject.SetActive(false);
            //QuestUI.Instance.isActive = true;

            //questUI.gameObject.SetActive(false);
            questUI.isActive = true;
        }

        //QuestUI.Instance.ToggleQuestWindow();
        //QuestUI.Instance.isActive = !QuestUI.Instance.isActive;
        questUI.ToggleQuestWindow();
        questUI.isActive = state;
        if (pauseState == false)
        {
            LockCursor(true);
        }
    }

    private void CheckPlayerScript()
    {
        if (playerScript == null)
            playerScript = FindAnyObjectByType<Player>();
    }

    public void SetShopScreen(bool state)
    {
        if (state)
        {
            shop.gameObject.SetActive(true);
            shop.ifShopOn = true;
            shop.UpdateMoneyData();
            
            
        }

        else
        {
            shop.DisableComfirmScreen();
            shop.SetBuyMenu(false);
            shop.SetSellMenu(false);
            shop.ResetSlot();
            shop.gameObject.SetActive(false);
            shop.ifShopOn = false;
        }
    }
    
    public void SetBoxScreen(bool state)
    {
        //StorageBox boxScript = storageBox.GetComponent<StorageBox>();
        if (state)
        {
            storageBox.SetActive(true);
            LockCursor(false);
            Rotatable(false);
            //boxScript.ifBoxOpen = true;
        }
        else
        {
            StorageBox storage = FindAnyObjectByType<StorageBox>();
            storage.DisableComfirmScreen();
            storageBox.SetActive(false);
            LockCursor(true);
            Rotatable(true);
            //boxScript.ifBoxOpen = false;
            
            //storage.ifBoxOpen= false;
        }
    }
}
