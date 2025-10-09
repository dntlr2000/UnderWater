using UnityEngine;

public class SharkSpawnerTemp : InteractableObject
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
        MonsterManager.Instance.SpawnMonsterPhoton("SharkV1", 1, target);
    }
}
