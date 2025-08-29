using UnityEngine;

public class Handle : InteractableObject
{
    public SubmarineOutside submarineBody;
    public bool interactable = true;

    private void Start()
    {
        if (submarineBody == null) submarineBody = FindAnyObjectByType<SubmarineOutside>();
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

    public override void HoldInteract()
    {

        if (player == null)
        {
            Debug.LogWarning("Player 정보가 입력된 상태가 아닙니다.");
            return;
        }
        else
        {
            submarineBody.player = player;
            submarineBody.SwitchSubmarineState(true);
        }
    }
}
