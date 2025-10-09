using UnityEngine;

public class Interaction : MonoBehaviour
{
    public InteractionUI interactionUI;

    [Header("Interaction Settings")]
    public float interactRange = 3f;
    public LayerMask interactLayer;
    public float holdDuration = 1.5f;

    [Header("Reference")]
    public Player player;

    private Camera cam;
    private float holdTimer = 0f;
    private Interactable currentTarget;
    public bool isBusy = false;

    private void Start()
    {
        cam = Camera.main;
        interactionUI.ResetUI();
    }
    private void Update()
    {
        if (!isBusy)
            HandleInteraction();
    }
    private void HandleInteraction()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer);

        if (hitSomething)
        {
            var interactable = hit.collider.GetComponent<Interactable>();
            if (interactable != null)
            {
                currentTarget = interactable;
                interactionUI.SetCursor(interactable.GetCursorType());

                switch (interactable.GetInteractionType())
                {
                    case InteractionType.Instant:
                        interactionUI.ShowCursor();
                        if (Input.GetKeyDown(KeyCode.E))
                        {
                            interactable.Interact();
                            ResetInteractionState();
                        }
                        break;

                    case InteractionType.Gauge:
                        interactionUI.ShowGauge();
                        if (Input.GetKey(KeyCode.E))
                        {
                            holdTimer += Time.deltaTime;
                            interactionUI.UpdateGauge(holdTimer / holdDuration);

                            if (holdTimer >= holdDuration)
                            {
                                interactable.Interact();
                                ResetInteractionState();
                            }
                        }
                        else
                        {
                            holdTimer = 0f;
                            interactionUI.UpdateGauge(0f);
                        }
                        break;
                }

                return;
            }
        }

        // 아무 것도 안 보고 있거나, 상호작용할 대상이 아니면 UI 리셋
        ResetInteractionUI();
    }

    private void ResetInteractionUI()
    {
        interactionUI.ResetUI();
        holdTimer = 0f;
        interactionUI.UpdateGauge(0f);
        currentTarget = null;
    }

    public void ResetInteractionState()
    {
        holdTimer = 0f;
        currentTarget = null;
        interactionUI.ResetUI();
        interactionUI.UpdateGauge(0f);
    }
}
