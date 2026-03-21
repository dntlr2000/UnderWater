using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    Canvas canvas;
    RectTransform rect;
    CanvasGroup cg;

    [HideInInspector] public Transform originalParent;
    [HideInInspector] public Vector2 originalAnchoredPos;
    [HideInInspector] public int originalSlotID;

    Inventory inventory;
    Vector2 dragOffset;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        cg = gameObject.AddComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        inventory = FindAnyObjectByType<Inventory>();

    }

    public void OnBeginDrag(PointerEventData e)
    {
        //원래 슬롯 정보 저장
        originalParent = transform.parent;
        originalAnchoredPos = rect.anchoredPosition;
        var slot = originalParent.GetComponent<ItemSlot>();
        originalSlotID = slot != null ? slot.SlotID : -1;

        //Canvas 좌표계로 이동하여 마우스와 아이콘 간의 위치 계산
        transform.SetParent(canvas.transform, true);
        RectTransform canvasRect = canvas.transform as RectTransform;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, 
            e.position, 
            cam, 
            out Vector2 pointerLocal);

        dragOffset = rect.anchoredPosition - pointerLocal; //오프셋 보정

        cg.blocksRaycasts = false;
        cg.alpha = 0.8f;
    }

    public void OnDrag(PointerEventData e)
    {
        //마우스 포인터를 따라다니게
        RectTransform canvasRect = canvas.transform as RectTransform;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, 
            e.position, 
            cam, 
            out Vector2 pointerLocal);

        rect.anchoredPosition = pointerLocal + dragOffset;
    }

    public void OnEndDrag(PointerEventData e)
    {
        cg.blocksRaycasts = true;
        cg.alpha = 1f;

        var hitGO = e.pointerCurrentRaycast.gameObject;

        // 휴지통(TrashCan)에 드롭했는지 확인
        // (휴지통 UI 오브젝트의 Tag를 "TrashCan"으로 설정해야 합니다)
        if (hitGO != null && hitGO.CompareTag("TrashCan"))
        {
            // 빈 슬롯을 드래그해서 버리려는 경우 무시
            if (inventory.GetItemID(originalSlotID) != -1)
            {
                // 버리기 로직이 정상 작동하도록 Inventory의 현재 선택 인덱스를 맞춰줍니다.
                inventory.throwIndex = originalSlotID;

                // 확정 창 띄우기
                inventory.SetThrowScreen(true);
            }
        }
        else
        {
            // 휴지통이 아닌 곳에 드롭했을 경우 기존의 슬롯 이동 로직 실행
            var dropSlot = hitGO ? hitGO.GetComponentInParent<ItemSlot>() : null;

            if (dropSlot != null && originalSlotID >= 0)
            {
                // Inventory의 교체 메서드 호출
                inventory.MoveItemSlot(originalSlotID, dropSlot.SlotID);
            }
            else
            {
                inventory.MoveItemSlot(originalSlotID, originalSlotID);
            }
        }

        transform.SetParent(originalParent, false); //원래 부모로 복원하기
        rect.anchoredPosition = originalAnchoredPos;
        rect.localScale = Vector3.one;
    }
}
