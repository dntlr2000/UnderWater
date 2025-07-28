using UnityEngine;

public class UIController : MonoBehaviour
{
    private ItemUIManager itemUIManager;
    private OptionManager optionManager;
    public PauseScreen pauseScreen;
    bool pauseState = false;

    public Player playerScript;

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
            if (!pauseState) //옵션이 비활성화되어 있을 때
            {
                if (itemUIManager.showInventory) { //아이템창이 활성화되어 있으면 아이템 창 닫고 종료
                    itemUIManager.SwitchInventoryState();
                    playerScript.canMoveCamera = true;
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

                if (optionManager.ifOptionActive) //옵션이 활성화되어있을 때
                {
                    optionManager.TurnOptions(false);
                    return;
                }


                playerScript.canMoveCamera = true;
                SetPauseScreen(false);
                return;
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

}
