using UnityEngine;

public class RaycastInteract : MonoBehaviour
{
    public Camera playerCamera;
    public float distance = 5f;
    public LayerMask interactLayer; //상호작용할 레이어
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerCamera= GetComponent<Camera>(); //스크립트가 카메라에게 부착되어 있으므로
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
    void Interact(); //상호작용 기능을 이 메서드 안에서 구현하던지, 아니면 관련된 메서드를 여기서 호출해야 할듯?
}
