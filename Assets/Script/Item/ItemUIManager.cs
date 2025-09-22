using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemUIManager : MonoBehaviour
{
    public bool showInventory = false;
    
    public GameObject InventoryScreen;
    //public RawImage[] itemSlots;
    public ItemSlot[] itemSlots;
    public TextMeshProUGUI MoneyText;
    

    //public Texture defaultSlotTexture;

    private void Start()
    {
        SetSlotIDs();
    }

    public void SetSlotIDs()
    {
        for (int i = 0; i < itemSlots.Length; i++)
        {
            itemSlots[i].SlotID = i;
        }
    }
    /*
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            SwitchInventoryState();
        }
        
    }
    */

    public void SwitchInventoryState()
    {
        if (InventoryScreen == null)
        {
            Debug.LogError("인벤토리가 할당되지 않았습니다.");
            return;
        }

        if (showInventory)
        {
            showInventory = false;
            InventoryScreen.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            showInventory= true;
            InventoryScreen.SetActive(true);
            Cursor.lockState = CursorLockMode.None; //Option이랑 충돌나지 않게 조정 예정
            Cursor.visible = true;
            
        }
    }


    public void LoadIcons(int index, Sprite spriteImage)
    {
        if (index < 0 || index >= itemSlots.Length)
        {
            Debug.LogError("존재하지 않는 슬롯입니다.");
            return;
        }
        if (spriteImage == null)
        {
            Debug.LogError("스프라이트가 할당되지 않았습니다.");
            return;
        }



        itemSlots[index].itemSlotIcon.texture = spriteImage.texture;
        //itemSlots[index].quatitiy.text = quantity.ToString();
        itemSlots[index].itemSlotIcon.gameObject.SetActive(true);
        //itemSlots[index].quatitiy.gameObject.SetActive(true);

        //Debug.Log($"added item icon on slot {index}");
    }

    public void SetQuantity(int index, int quantity)
    {
        if (index < 0 || index >= itemSlots.Length)
        {
            Debug.LogError("존재하지 않는 슬롯입니다.");
            return;
        }
        itemSlots[index].quatitiy.text = quantity.ToString();
        itemSlots[index].quatitiy.gameObject.SetActive(true);
    }


    public void ResetIcons(int index)
    {
        itemSlots[index].itemSlotIcon.gameObject.SetActive(false);
        itemSlots[index].quatitiy.gameObject.SetActive(false);
    }

    public void UpdateMoney(int value)
    {
        MoneyText.text = value.ToString() + "G";
    }

    public void SetColors(int index, byte r = 63, byte g = 63, byte b = 63)
    {
        itemSlots[index].SetColor(r, g, b);
    }
}
