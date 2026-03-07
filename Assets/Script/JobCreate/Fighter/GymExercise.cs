using UnityEngine;

// 파일명: GymExercise.cs
// 주의: 클래스 이름과 파일 이름이 100% 똑같아야 유니티가 에셋 생성을 허락해 줍니다!

[CreateAssetMenu(fileName = "NewGymExercise", menuName = "Workbench Data/Gym Exercise")]
public class GymExercise : WorkbenchData
{
    [TextArea]
    public string exerciseDescription; // 운동 설명 (예: "전신 근육을 단련하여 최대 체력을 높입니다.")

    public float exerciseTime;         // 운동 소요 시간 (예: 5초)

    [Header("Costs (소모할 상태 수치)")]
    public float costHunger; // 소모할 배고픔 수치 (예: 20)
    public float costWater;  // 소모할 수분 수치 (예: 30)

    [Header("Rewards (영구 스탯 보상)")]
    public float bonusMaxHP;      // 오르는 최대 체력 (예: 10)
    public float bonusMaxStamina; // 오르는 최대 스테미너 (예: 5)
}