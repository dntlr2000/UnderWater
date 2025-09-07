using UnityEngine;

public class OpenableStorageBox : InteractableObject
{
    public string boxName = "storageBox";
    public bool interactable = true;
    StorageBox box;

    private void Start()
    {

    }

    public override void Interact()
    {
        if (interactable && Input.GetMouseButton(1))
        {
            UpdateGuage(true, holdDuration);
        }
        else
        {
            UpdateGuage(false, holdDuration);
        }
    }

    public void OpenBox()
    {
        UIController uIController = FindAnyObjectByType<UIController>();
        if (uIController == null)
        {
            Debug.LogError("UI에서 UI 컨트롤러를를 찾을 수 없습니다.");
            return;
        }
        uIController.SetBoxScreen(true);

        box = FindAnyObjectByType<StorageBox>();
        if (box == null)
        {
            Debug.LogError("UI에서 박스 UI 스크립트를 찾을 수 없습니다.");
            return;
        }

        box.gameObject.SetActive(true);
        box.SetBox();
        box.SetBoxName(boxName);
        box.LoadBox();
    }

    public override void HoldInteract()
    {
        OpenBox();
    }
}