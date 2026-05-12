using UnityEngine;

public class BuoyancyController : MonoBehaviour
{
    [Header("Buoyancy Sensors")]
    public float sensorRadius = 0.05f;
    public float chestHeight = 1.5f;
    public float headHeight = 2.5f;
    public LayerMask waterLayer;
    public LayerMask interiorLayer; // 잠수함 내부라면 부력도 무시

    [Header("Surface Bobbing")]
    public float bobbingFrequency = 2.0f;
    public float bobbingAmplitude = 0.5f;

    private bool isChestInWater = false;
    private bool isHeadInWater = false;

    void Update()
    {
        // 부력 지점(가슴 높이) 계산
        Vector3 chestPos = transform.position + (Vector3.up * chestHeight);
        Vector3 headPos = transform.position + (Vector3.up * headHeight);

        // 잠수함 내부라면 부력 무시
        if (Physics.CheckSphere(chestPos, sensorRadius, interiorLayer))
        {
            isChestInWater = false;
            isHeadInWater = false;
            return;
        }

        // 물속인지 체크
        isChestInWater = Physics.CheckSphere(chestPos, sensorRadius, waterLayer);
        isHeadInWater = Physics.CheckSphere(headPos, sensorRadius, waterLayer);
    }

    // 수영 모드로 진입할지 여부 (가슴이 물에 닿았는가)
    public bool IsInWater() => isChestInWater;

    public bool IsChestInWater() => isChestInWater;

    public bool IsHeadInWater() => isHeadInWater;

    // 수면에 떠있는 상태인가? (가슴은 물속, 머리는 물 밖)
    public bool IsAtSurface() => isChestInWater && !isHeadInWater;

    // 수면 흔들림 속도값 반환 (AddForce 대신 속도값만 건네줌)
    public float GetBobbingVelocity()
    {
        return Mathf.Sin(Time.time * bobbingFrequency) * bobbingAmplitude;
    }
}