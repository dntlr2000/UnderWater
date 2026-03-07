using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IDropHandler
{
    public int SlotID;
    [Header("InsideSlots")]
    public RawImage background;
    public RawImage itemSlotIcon;
    public TextMeshProUGUI quatitiy;

    [Header("ShopTexts")]
    public TextMeshProUGUI itemName;
    public TextMeshProUGUI priceText;

    void Start()
    {
        //itemSlotIcon.gameObject.SetActive(false);
    }

    public void OnDrop(PointerEventData eventData) { }

    public void SelectedShop()
    {
        ShopManager shopManager = FindAnyObjectByType<ShopManager>();
        if (shopManager != null)
        {
            shopManager.SelectSlot(SlotID);
        }
    }

    public void SelectedStorageInventory()
    {
        StorageBox storageBox = FindAnyObjectByType<StorageBox>();
        if (storageBox != null)
        {
            storageBox.SetInventorytIndex(SlotID);
        }
    }

    public void SelectedStorageBox()
    {
        StorageBox storageBox = FindAnyObjectByType<StorageBox>();
        if (storageBox != null)
        {
            storageBox.SetBoxIndex(SlotID);
        }
    }

    public void SetColor(byte r = 63, byte g = 63, byte b = 63)
    {
        background.color = new Color32(r, g, b, 71);
    }
}
