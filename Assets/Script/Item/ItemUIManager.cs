using UnityEngine;
using UnityEngine.UI;

public class ItemUIManager : MonoBehaviour
{
    private bool showInventory = false;
    public GameObject InventoryScreen;

    public RawImage[] ItemSlots;

    //public Texture defaultSlotTexture;

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
        }
        else
        {
            showInventory= true;
            InventoryScreen.SetActive(true);
        }
    }


    public void LoadIcons(int index, Sprite spriteImage)
    {
        if (index < 0 || index >= ItemSlots.Length)
        {
            Debug.LogError("존재하지 않는 슬롯입니다.");
            return;
        }
        if (spriteImage == null)
        {
            Debug.LogError("스프라이트가 할당되지 않았습니다.");
            return;
        }



        ItemSlots[index].texture = spriteImage.texture;

        ItemSlots[index].gameObject.SetActive(true);

        Debug.Log($"added item icon on slot {index}");
    }

    public void ResetIcons(int index)
    {
        ItemSlots[index].gameObject.SetActive(false);
    }
}
