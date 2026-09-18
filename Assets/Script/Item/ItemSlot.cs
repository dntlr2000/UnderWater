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

    public Slider durabilityRoot;
    [SerializeField] private Image durabilityFill;


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

    public void SetDurability(float value, float maxValue)
    {
        //Debug.Log($"Setting durability for slot {SlotID}: {value}/{maxValue}");
        if (value >= maxValue || maxValue == -1 || maxValue == 0)
        {
            durabilityRoot.gameObject.SetActive(false);
            return;
        }

        if (durabilityRoot != null && durabilityFill != null)
        {
            durabilityRoot.gameObject.SetActive(true);
            durabilityRoot.maxValue = maxValue;
            durabilityRoot.value = value;
            // Set the color based on the percentage
            float percentage = value / maxValue;
            /*
            if (percentage > 0.5f)
            {
                durabilityFill.color = Color.green;
            }
            else if (percentage > 0.2f)
            {
                durabilityFill.color = Color.yellow;
            }
            else
            {
                durabilityFill.color = Color.red;
            }
            */
            
        }
    }
}
