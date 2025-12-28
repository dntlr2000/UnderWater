using UnityEngine;

public class ItemSpawnerTemp : InteractableObject
{

    public override void Interact()
    {
        if (Input.GetMouseButton(1))
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
        Vector3 target = player.gameObject.transform.position + player.gameObject.transform.forward;
        ItemDatabase.Instance.GenerateItemPhoton(1, 3, target);
    }
}
