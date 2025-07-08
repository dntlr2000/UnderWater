using UnityEngine;

public class Door : MonoBehaviour, Interactable
{
    public string GetCursorType() => "Door";
    public string GetInteractionID() => "Door";
    public InteractionType GetInteractionType() => InteractionType.Gauge;

    public void Interact()
    {
        Debug.Log("¹® ¿­±â");
    }
}
