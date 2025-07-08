using UnityEngine;

public class RaycastInteract : MonoBehaviour
{
    // !윤지님의 상호작용 스크립트가 넘어오지 않은 상태. 추후 해당 스크립트가 넘어오면 그걸 베이스로 작업할 예정
    public Camera playerCamera;
    public float distance = 5f;
    public LayerMask interactLayer; //상호작용할 레이어
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Inventory inventory;
    void Start()
    {
        //Debug.Log("RayCast 활성화");
        inventory = FindAnyObjectByType<Inventory>();
    }

    private void Update()
    {
        //Debug.Log("RayCast 활성화");
        CanInteract(); //현재 구조상 바라보고 있으면 매 프레임마다 호출되는 방식
    }

    public bool CanInteract()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, distance, interactLayer)) //ray를 발사했을 때 distance 안에서 맞춘 오브젝트가 ineractLayer에 속한 경우 hit 안에 오브젝트 정보가 들어옴
        {
            InteractableObject interactable = hit.collider.GetComponent<InteractableObject>(); //InteractableObject 인터페이스를 구현한 클래스일 경우 받아와짐
            if (interactable != null)
            {               
                interactable.Interact(); //해당 인터페이스를 구현한 클래스의 Interact 메서드를 불러온다.
            }
        }

        return true;
    }

}


public interface InteractableObject //상호작용할 수 있는 오브젝트에서 구현
{
    void Interact(); //바라보고 있을 때 호출이 되므로, 구현체에서 자세한 상호작용 조건을 통해 구현해야 함
}