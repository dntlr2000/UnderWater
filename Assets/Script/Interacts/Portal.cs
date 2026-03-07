using UnityEngine;

public class Portal : InteractableObject
{
    public override InteractionType GetInteractionType() => InteractionType.Gauge;
    public bool Interactable = true;

    //고정 좌표
    [Header("Fixed Position")]
    public Vector3 coordinate = Vector3.zero;

    //유동 좌표
    [Header("Enable when using Fluid Position")]
    public bool useTarget = false;
    public Transform target;

    public override void Interact() //카메라가 이 오브젝트를 바라볼 때 호출됨
    {

        if (Interactable && Input.GetMouseButton(1))
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

        if (player== null)
        {
            Debug.LogWarning("Player 정보가 입력된 상태가 아닙니다.");
            return;
        }
        else
        {
            if (!useTarget) player.gameObject.transform.position = coordinate;
            else player.gameObject.transform.position = target.position + coordinate;

        }
    }

}
