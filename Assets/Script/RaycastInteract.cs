using UnityEngine;

public class RaycastInteract : MonoBehaviour
{
    [Header("기본 설정들")]
    public Camera playerCamera;
    public float distance = 5f;
    public LayerMask interactLayer; //상호작용할 레이어
    public bool isBusy = false; //isBusy가 지금은 필요한지 모르겠음
    //public Player player;

    [Header("인벤토리")]
    public Inventory inventory;

    [Header("상호작용 기능 관련 설정")]
    //private Interactable currentTarget; //Interaction 코드를 보면 currentTarget은 없어도 되지 않을까 싶음
    public InteractionUI interactionUI;

    //private float holdTimer = 0f;
    //public float holdDuration = 1.5f;
    //홀드 타이머는 필드 오브젝트의 스크립트 안에서 구현

    void Start()
    {
        //Debug.Log("RayCast 활성화");
        inventory = FindAnyObjectByType<Inventory>();
        //playerCamera = Camera.main;
    }

    private void Update()
    {
        //Debug.Log("RayCast 활성화");
        CanInteract(); //현재 구조상 바라보고 있으면 매 프레임마다 호출되는 방식
    }

    public void CanInteract()
    {

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, distance, interactLayer)) //ray를 발사했을 때 distance 안에서 맞춘 오브젝트가 ineractLayer에 속한 경우 hit 안에 오브젝트 정보가 들어옴
        {
            Interactable interactable = hit.collider.GetComponent<Interactable>(); //InteractableObject 인터페이스를 구현한 클래스일 경우 받아와짐
            interactionUI.ShowCursor();
            if (interactable != null)
            {
                //currentTarget = interactable; 
                interactionUI.SetCursor(interactable.GetCursorType()); //정상 반영됨
                //interactionUI.ShowCursor();
                
                
                interactable.Interact(); //해당 인터페이스를 구현한 클래스의 Interact 메서드를 불러온다.
                //ResetInteractionState();
                return;
            }

            else
            {
                
            }
            
        }
        ResetInteractionUI();
        return;
    }


    private void ResetInteractionUI()
    {
        interactionUI.ResetUI();
        //holdTimer = 0f;
        interactionUI.UpdateGauge(0f);
        //currentTarget = null;
    }

    public void ResetInteractionState()
    {
        //holdTimer = 0f;
       //currentTarget = null;
        interactionUI.ResetUI();
        interactionUI.UpdateGauge(0f);
    }
    
    //public bool CheckInteractable()
    //{
    //    if (isBusy) return false;
    //    return inventory.HoldingInteractableItem();
    //}
    
}

/*
public interface InteractableObject //상호작용할 수 있는 오브젝트에서 구현
{

    void Interact(); //바라보고 있을 때 호출이 되므로, 구현체에서 자세한 상호작용 조건을 통해 구현해야 함
}
*/