using UnityEngine;

public class FaintedPlayerInteractable : InteractableObject
{
    [SerializeField] private Condition targetCondition;
    [SerializeField, Min(0.1f)] private float reviveHoldDuration = 3f;

    /// <summary>
    /// 같은 플레이어 오브젝트의 Condition을 빈사 구조 대상으로 연결합니다.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();

        if (targetCondition == null)
        {
            targetCondition = GetComponent<Condition>();
        }
    }

    /// <summary>
    /// 기존 상호작용 게이지를 사용하는 구조 상호작용 형식을 반환합니다.
    /// </summary>
    public override InteractionType GetInteractionType()
    {
        return InteractionType.Gauge;
    }

    /// <summary>
    /// 살아 있는 다른 플레이어가 우클릭을 유지할 때 구조 게이지를 진행합니다.
    /// </summary>
    public override void Interact()
    {
        bool canRevive = targetCondition != null
            && targetCondition.GetIsFainted()
            && player != null
            && player != targetCondition.player
            && player.condition != null
            && !player.condition.GetIsFainted();

        UpdateGuage(canRevive && Input.GetMouseButton(1), reviveHoldDuration);
    }

    /// <summary>
    /// 구조 게이지가 완료되면 대상 Condition에 구조를 요청합니다.
    /// </summary>
    public override void HoldInteract()
    {
        if (targetCondition == null || player == null)
        {
            return;
        }

        targetCondition.RequestRevive(player);
    }
}
