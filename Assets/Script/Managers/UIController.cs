using UnityEngine;

public class UIController : MonoBehaviour
{
    private ItemUIManager itemUIManager;
    private OptionManager optionManager;
    public Player playerScript;

    //private bool ifMouseOn = true;

    // Update is called once per frame
    private void Start()
    {
        itemUIManager= GetComponent<ItemUIManager>();
        optionManager= GetComponent<OptionManager>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (optionManager.ifOptionActive == false) //옵션이 비활성화되어 있을 때
            {
                if (itemUIManager.showInventory) { //아이템창이 활성화되어 있으면 아이템 창 닫고 종료
                    itemUIManager.SwitchInventoryState();
                    playerScript.canMoveCamera = true;
                    return; 
                } 
                   
                optionManager.TurnOptions(true); //설정창 활성화
                playerScript.canMoveCamera = false;
            }

            else //옵션이 활성화되어있을 때
            {
                optionManager.TurnOptions(false);

                if (itemUIManager.showInventory)
                { //아이템창이 활성화되어 있으면 카메라 움직임 정지 유지
                    playerScript.canMoveCamera = false;
                    return;
                }
                playerScript.canMoveCamera = true;
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
}
