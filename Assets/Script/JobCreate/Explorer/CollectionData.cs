using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCollection", menuName = "Workbench Data/Collection")]
public class CollectionData : WorkbenchData
{
    [TextArea]
    public string collectionDescription; // 수집품 설명 (예: "고대 유적에서 발견된 석판의 파편들입니다.")
    public float researchTime;           // 연구/조합 소요 시간

    [Header("Requirements (필요 수집품/재료)")]
    // 요리사 때 만들었던 RecipeIngredient 구조체를 그대로 재활용합니다! 아주 편하죠!
    public List<RecipeIngredient> requiredItems = new List<RecipeIngredient>();

    [Header("Rewards (보상)")]
    public int rewardStoryItemID; // 스토리 단서 아이템 ID (0이면 없음)
    public float rewardBonusStat; // 연구 완료 시 얻는 보너스 스탯 (예: 시야 증가, 이동속도 증가 등)
}