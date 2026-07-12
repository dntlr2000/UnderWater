using System.Linq;
using UnityEngine;

public class PlayerStepUp : MonoBehaviour
{
    [Header("Step Up")]
    // Step-up은 낮은 지형 턱만 처리하고, 점프/공중 상태에서는 Player가 false를 넘겨 비활성화합니다.
    public bool useStepUp = true;
    public string stepGroundTag = "Ground";
    public float maxStepHeight = 0.35f;
    public float stepCheckDistance = 0.45f;
    public float stepLiftSpeed = 12f;
    public float lowerStepRayHeight = 0.08f;
    public float upperStepClearance = 0.08f;
    public float stepSideRayOffset = 0.25f;
    // Keep these if we need to reintroduce extra clearance for sticky step edges.
    // public float stepForwardNudge = 0.08f;
    // public float stepExtraLift = 0.02f;
    public bool drawStepUpGizmos = true;
    public Collider stepReferenceCollider;

    // PlayerFoot에 붙어 있어도 실제 이동은 부모 Playerable의 Rigidbody에 적용합니다.
    private Rigidbody rb;
    private Transform ownerRoot;
    private Vector3 lastStepMoveDir;
    private Transform OwnerRoot => ownerRoot != null ? ownerRoot : transform;

    private void Awake()
    {
        // 이 스크립트가 발 오브젝트에 있어도 부모에서 플레이어 본체 Rigidbody를 찾습니다.
        rb = GetComponentInParent<Rigidbody>();
        ownerRoot = rb != null ? rb.transform : transform;
    }

    public bool TryStepUp(Vector3 moveDir, bool isGrounded)
    {
        // 발 콜라이더(PlayerFoot)를 기준으로 턱 높이와 감지 레이 위치를 계산합니다.
        if (!useStepUp || !isGrounded || rb == null || stepReferenceCollider == null) return false;

        // Step-up은 수평 이동 중에만 의미가 있으므로 Y축 성분은 버립니다.
        moveDir.y = 0f;
        if (moveDir.sqrMagnitude < 0.01f) return false;
        moveDir.Normalize();
        lastStepMoveDir = moveDir;

        // 발 콜라이더의 월드 Bounds를 기준으로 레이 시작 높이와 좌우 폭을 계산합니다.
        Bounds bounds = stepReferenceCollider.bounds;
        float footY = bounds.min.y;
        float bodyRadius = Mathf.Min(bounds.extents.x, bounds.extents.z);
        float rayDistance = bodyRadius + stepCheckDistance;
        float sideOffset = Mathf.Min(stepSideRayOffset, bodyRadius * 0.8f);

        Vector3 baseCenter = new Vector3(bounds.center.x, footY, bounds.center.z);
        Vector3 sideDir = Vector3.Cross(Vector3.up, moveDir).normalized;

        // 중앙, 좌우 3지점을 검사해서 모서리로 접근할 때도 턱을 놓치지 않게 합니다.
        if (TryStepUpFromOffset(baseCenter, moveDir, rayDistance, Vector3.zero)) return true;
        if (sideOffset > 0f && TryStepUpFromOffset(baseCenter, moveDir, rayDistance, sideDir * sideOffset)) return true;
        if (sideOffset > 0f && TryStepUpFromOffset(baseCenter, moveDir, rayDistance, -sideDir * sideOffset)) return true;

        return false;
    }

    private bool TryStepUpFromOffset(Vector3 baseCenter, Vector3 moveDir, float rayDistance, Vector3 offset)
    {
        // 1. 낮은 레이가 Ground 태그를 맞추면 턱 후보로 봅니다.
        Vector3 lowerOrigin = baseCenter + offset + Vector3.up * lowerStepRayHeight;
        if (!TryGetStepGroundHit(lowerOrigin, moveDir, rayDistance, out RaycastHit lowerHit)) return false;

        // 2. 위쪽 레이가 막히면 낮은 턱이 아니라 벽으로 판단합니다.
        Vector3 upperOrigin = baseCenter + offset + Vector3.up * (maxStepHeight + upperStepClearance);
        if (RaycastHitsBlockingCollider(upperOrigin, moveDir, rayDistance)) return false;

        // 3. 턱 위에서 아래로 쏴서 실제로 디딜 수 있는 Ground 표면 높이를 찾습니다.
        Vector3 topProbeOrigin = baseCenter
            + offset
            + moveDir * Mathf.Min(rayDistance, lowerHit.distance + 0.12f)
            + Vector3.up * (maxStepHeight + upperStepClearance);

        if (!TryGetStepGroundHit(
            topProbeOrigin,
            Vector3.down,
            maxStepHeight + upperStepClearance + 0.2f,
            out RaycastHit topHit))
        {
            return false;
        }

        float stepDelta = topHit.point.y - baseCenter.y;
        if (stepDelta <= 0.01f || stepDelta > maxStepHeight) return false;

        // 한 프레임에 너무 많이 튀지 않도록 stepLiftSpeed로 상승량을 제한합니다.
        float liftAmount = Mathf.Min(stepDelta, stepLiftSpeed * Time.fixedDeltaTime);
        rb.MovePosition(rb.position + Vector3.up * liftAmount);
        return true;
    }

    private bool TryGetStepGroundHit(Vector3 origin, Vector3 direction, float distance, out RaycastHit stepHit)
    {
        // 여러 콜라이더를 맞출 수 있으므로 가까운 순서대로 Ground 태그만 골라냅니다.
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits.OrderBy(hit => hit.distance))
        {
            if (IsOwnCollider(hit.collider)) continue;
            if (!hit.collider.CompareTag(stepGroundTag)) continue;

            stepHit = hit;
            return true;
        }

        stepHit = default;
        return false;
    }

    private bool RaycastHitsBlockingCollider(Vector3 origin, Vector3 direction, float distance)
    {
        // 위쪽 레이는 Ground 여부와 상관없이 앞을 막는 콜라이더가 있는지만 확인합니다.
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            if (IsOwnCollider(hit.collider)) continue;
            return true;
        }

        return false;
    }

    private bool IsOwnCollider(Collider other)
    {
        // PlayerFoot/몸통처럼 플레이어 자신에게 속한 콜라이더는 레이 판정에서 제외합니다.
        return other.transform.IsChildOf(OwnerRoot);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawStepUpGizmos || stepReferenceCollider == null) return;

        // Scene 뷰에서 플레이어 선택 시 Step-up 감지 레이를 표시합니다.
        // 플레이 중에는 마지막 이동 방향, 편집 중에는 발 오브젝트의 앞 방향을 사용합니다.
        Vector3 drawDir = Application.isPlaying && lastStepMoveDir.sqrMagnitude > 0.01f
            ? lastStepMoveDir
            : OwnerRoot.forward;

        drawDir.y = 0f;
        if (drawDir.sqrMagnitude < 0.01f) return;
        drawDir.Normalize();

        Bounds bounds = stepReferenceCollider.bounds;
        float footY = bounds.min.y;
        float bodyRadius = Mathf.Min(bounds.extents.x, bounds.extents.z);
        float rayDistance = bodyRadius + stepCheckDistance;
        float sideOffset = Mathf.Min(stepSideRayOffset, bodyRadius * 0.8f);

        Vector3 baseCenter = new Vector3(bounds.center.x, footY, bounds.center.z);
        Vector3 sideDir = Vector3.Cross(Vector3.up, drawDir).normalized;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
        Gizmos.DrawSphere(baseCenter, 0.04f);

        DrawStepUpRaySet(baseCenter, drawDir, rayDistance, Vector3.zero);
        if (sideOffset > 0f)
        {
            DrawStepUpRaySet(baseCenter, drawDir, rayDistance, sideDir * sideOffset);
            DrawStepUpRaySet(baseCenter, drawDir, rayDistance, -sideDir * sideOffset);
        }
    }

    private void DrawStepUpRaySet(Vector3 baseCenter, Vector3 moveDir, float rayDistance, Vector3 offset)
    {
        // 초록/회색: 턱 후보를 찾는 낮은 레이입니다.
        Vector3 lowerOrigin = baseCenter + offset + Vector3.up * lowerStepRayHeight;
        Vector3 lowerEnd = lowerOrigin + moveDir * rayDistance;
        bool lowerHitGround = TryGetStepGroundHit(lowerOrigin, moveDir, rayDistance, out RaycastHit lowerHit);

        // 빨강: 이 높이에서 막히면 올라갈 턱이 아니라 벽으로 봅니다.
        Vector3 upperOrigin = baseCenter + offset + Vector3.up * (maxStepHeight + upperStepClearance);
        Vector3 upperEnd = upperOrigin + moveDir * rayDistance;

        // 하늘색: 턱 위쪽에서 아래로 쏴서 실제 발을 올릴 지면 높이를 확인합니다.
        Vector3 topProbeOrigin = baseCenter
            + offset
            + moveDir * (lowerHitGround ? Mathf.Min(rayDistance, lowerHit.distance + 0.12f) : rayDistance)
            + Vector3.up * (maxStepHeight + upperStepClearance);
        Vector3 topProbeEnd = topProbeOrigin + Vector3.down * (maxStepHeight + upperStepClearance + 0.2f);
        RaycastHit topHit = default;
        bool topHitGround = false;
        if (lowerHitGround)
        {
            topHitGround = TryGetStepGroundHit(
                topProbeOrigin,
                Vector3.down,
                maxStepHeight + upperStepClearance + 0.2f,
                out topHit);
        }

        Gizmos.color = lowerHitGround ? Color.green : Color.gray;
        Gizmos.DrawLine(lowerOrigin, lowerEnd);
        Gizmos.DrawSphere(lowerOrigin, 0.025f);
        if (lowerHitGround) Gizmos.DrawSphere(lowerHit.point, 0.04f);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(upperOrigin, upperEnd);
        Gizmos.DrawSphere(upperOrigin, 0.025f);

        Gizmos.color = lowerHitGround ? Color.cyan : Color.gray;
        Gizmos.DrawLine(topProbeOrigin, topProbeEnd);
        Gizmos.DrawSphere(topProbeOrigin, 0.025f);
        if (topHitGround)
        {
            Gizmos.DrawSphere(topHit.point, 0.04f);

            float stepDelta = topHit.point.y - baseCenter.y;
            if (stepDelta > 0.01f && stepDelta <= maxStepHeight)
            {
                Vector3 appliedStep = Vector3.up * stepDelta;
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(baseCenter + offset, baseCenter + offset + appliedStep);
                Gizmos.DrawSphere(baseCenter + offset + appliedStep, 0.035f);
            }
        }
    }
}
