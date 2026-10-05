using UnityEngine;

public class BuoyancyController : MonoBehaviour
{
    [Header("Buoyancy Sensors")]
    public float sensorRadius = 0.05f;
    public float chestHeight = 1.5f;
    public float headHeight = 2.45f;
    public LayerMask waterLayer;
    public LayerMask interiorLayer;

    [Header("Surface Bobbing")]
    public float bobbingFrequency = 1f;
    [HideInInspector] public float bobbingAmplitude = 0.5f; // 기존 직렬화 및 외부 호출 호환용 속도 진폭
    [Min(0f)] public float bobbingHeight = 0.03f;

    [Header("Surface Floating")]
    [Min(0f)] public float surfaceClearance = 0.15f;
    [Min(0f)] public float surfaceCaptureDepth = 0.1f;
    [Min(0f)] public float surfaceReleaseDepth = 0.4f;
    [Min(0.01f)] public float surfaceFollowGain = 5f;
    [Min(0.01f)] public float maxSurfaceSpeed = 1f;
    [Min(0f)] public float maxCaptureFallSpeed = 1f;

    private bool isChestInWater;
    private bool isHeadInWater;
    private bool isSurfaceFloating;
    private bool suppressSurfaceCapture;
    private float surfaceTime;
    private float surfaceY;
    private Vector3 sampledPosition;
    private Collider currentWater;
    private Collider[] waterHits = new Collider[8];

    // 이동 직전의 물리 위치로 센서를 갱신하고 현재 물 영역의 실제 윗면을 추적합니다.
    public void RefreshWaterState(Vector3 bodyPosition)
    {
        sampledPosition = bodyPosition;
        Vector3 chestPos = bodyPosition + Vector3.up * chestHeight;
        Vector3 headPos = bodyPosition + Vector3.up * headHeight;
        if (Physics.CheckSphere(chestPos, sensorRadius, interiorLayer, QueryTriggerInteraction.Collide))
        {
            ResetWaterState();
            return;
        }

        isChestInWater = Physics.CheckSphere(chestPos, sensorRadius, waterLayer, QueryTriggerInteraction.Collide);
        isHeadInWater = Physics.CheckSphere(headPos, sensorRadius, waterLayer, QueryTriggerInteraction.Collide);
        if (!isChestInWater)
        {
            ResetWaterState();
            return;
        }

        // 겹친 물 영역에서도 같은 콜라이더가 유효한 동안 기준 수면을 바꾸지 않습니다.
        if (!TrySampleSurface(currentWater, chestPos, out surfaceY))
        {
            currentWater = null;
            isSurfaceFloating = false;
            surfaceTime = 0f;
            int count = Physics.OverlapSphereNonAlloc(chestPos, sensorRadius, waterHits, waterLayer, QueryTriggerInteraction.Collide);
            if (count == waterHits.Length)
            {
                waterHits = Physics.OverlapSphere(chestPos, sensorRadius, waterLayer, QueryTriggerInteraction.Collide);
                count = waterHits.Length;
            }

            for (int i = 0; i < count; i++)
            {
                if (TrySampleSurface(waterHits[i], chestPos, out float candidateY)
                    && (currentWater == null || candidateY > surfaceY))
                {
                    currentWater = waterHits[i];
                    surfaceY = candidateY;
                }
            }
        }

        // 잠수로 수면 복귀 구간을 벗어나면 다음 상승 시 다시 부유할 수 있습니다.
        if (currentWater != null && surfaceY - headPos.y > Mathf.Max(surfaceCaptureDepth, surfaceReleaseDepth))
            suppressSurfaceCapture = false;
    }

    // 내부에서 쏜 Ray의 미검출을 피하도록 해당 콜라이더 위에서 아래로 수면을 찾습니다.
    private bool TrySampleSurface(Collider water, Vector3 chestPos, out float height)
    {
        height = 0f;
        if (water == null || !water.enabled || !water.gameObject.activeInHierarchy
            || (waterLayer.value & (1 << water.gameObject.layer)) == 0)
            return false;
        if ((water.ClosestPoint(chestPos) - chestPos).sqrMagnitude > sensorRadius * sensorRadius)
            return false;

        Bounds bounds = water.bounds;
        Vector3 origin = new Vector3(chestPos.x, bounds.max.y + 0.1f, chestPos.z);
        if (!water.Raycast(new Ray(origin, Vector3.down), out RaycastHit hit, bounds.size.y + 0.2f)
            || hit.normal.y < 0.5f)
            return false;

        height = hit.point.y;
        return true;
    }

    // 명시적인 상승/잠수에 부유 보정이 간섭하지 않게 하며 잠수 직후의 재진입을 막습니다.
    public void StopSurfaceFloating(bool diving)
    {
        isSurfaceFloating = false;
        surfaceTime = 0f;
        suppressSurfaceCapture = diving;
    }

    // 빈사 자동 부상은 직전 잠수 입력과 관계없이 수면에 다시 정착할 수 있습니다.
    public void AllowSurfaceCapture()
    {
        suppressSurfaceCapture = false;
    }

    // 진입/해제 깊이를 분리하고 수면 상대 높이 오차를 제한된 수직 속도로 복원합니다.
    public bool TryGetSurfaceVelocity(float currentVerticalSpeed, float deltaTime, out float velocity)
    {
        velocity = 0f;
        if (!isChestInWater || currentWater == null || suppressSurfaceCapture)
            return false;

        float headDepth = surfaceY - (sampledPosition.y + headHeight);
        float releaseDepth = Mathf.Max(surfaceCaptureDepth, surfaceReleaseDepth);
        if (isSurfaceFloating && headDepth > releaseDepth)
        {
            isSurfaceFloating = false;
            surfaceTime = 0f;
            return false;
        }

        if (!isSurfaceFloating)
        {
            // 낙하 중인 플레이어를 수면에서 갑자기 붙잡지 않습니다.
            if (headDepth > surfaceCaptureDepth || currentVerticalSpeed < -maxCaptureFallSpeed)
                return false;
            isSurfaceFloating = true;
            surfaceTime = 0f;
        }

        float clearance = Mathf.Max(surfaceClearance, sensorRadius + bobbingHeight + 0.02f);
        float targetY = surfaceY - headHeight + clearance
            + Mathf.Sin(surfaceTime * bobbingFrequency) * bobbingHeight;
        velocity = Mathf.Clamp((targetY - sampledPosition.y) * surfaceFollowGain, -maxSurfaceSpeed, maxSurfaceSpeed);
        surfaceTime += Mathf.Max(0f, deltaTime);
        return true;
    }

    // 순간이동, 물 밖 이동, 비활성화 시 이전 수면과 입력 이력을 모두 버립니다.
    public void ResetWaterState()
    {
        isChestInWater = false;
        isHeadInWater = false;
        currentWater = null;
        surfaceY = 0f;
        StopSurfaceFloating(false);
    }

    // 재활성화할 때 이전 물 영역의 부유 상태가 남지 않도록 초기화합니다.
    private void OnDisable()
    {
        ResetWaterState();
    }

    // 몸의 수중 여부는 기존 수영 상태 및 애니메이션 판정에 사용합니다.
    public bool IsInWater() => isChestInWater;

    // 가슴 센서의 실제 물 접촉 결과를 반환합니다.
    public bool IsChestInWater() => isChestInWater;

    // 부유 유지 여부와 별개로 머리 센서의 실제 물 접촉 결과를 반환합니다.
    public bool IsHeadInWater() => isHeadInWater;

    // 기존 호출부를 위해 가슴은 잠기고 머리는 나온 감지 결과를 유지합니다.
    public bool IsAtSurface() => isChestInWater && !isHeadInWater;

    // 수면에서 새 Space 입력이 상승 래치를 켜지 않도록 유지 상태를 제공합니다.
    public bool IsSurfaceFloating() => isSurfaceFloating;

    // 기존 API를 유지하되 속도를 적분해 표류하지 않도록 높이 복원 속도를 반환합니다.
    public float GetBobbingVelocity()
    {
        return TryGetSurfaceVelocity(0f, Time.fixedDeltaTime, out float velocity) ? velocity : 0f;
    }
}