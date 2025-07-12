using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IDropHandler
{
    public int SlotID;
    public RawImage itemSlotIcon;
    //public Texture defaultSlotTexture;
    public TextMeshProUGUI quatitiy;

    void Start()
    {
        // 아이콘 초기 상태: 없을 땐 꺼두거나 기본 텍스처
        itemSlotIcon.gameObject.SetActive(false);
    }

    public void OnDrop(PointerEventData eventData) { }
}
