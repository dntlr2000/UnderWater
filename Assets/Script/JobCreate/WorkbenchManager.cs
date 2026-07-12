using UnityEngine;
using System.Collections.Generic;

public class WorkbenchManager : InteractableObject
{
    [Header("Base Settings")]
    public string workbenchName = "통합 작업대";
    public JobType ownerJob;

    [Header("Data List")]
    public List<WorkbenchData> dataList = new List<WorkbenchData>();

    [Header("Background Process State")]
    public bool isWorking = false;
    public bool isRewardReady = false;
    public WorkbenchData currentTaskData;
    public float currentTimer = 0f;
    public float maxTime = 0f;

    public override InteractionType GetInteractionType() => InteractionType.Instant;

    private void Update()
    {
        if (isWorking)
        {
            currentTimer += Time.deltaTime;

            if (currentTimer >= maxTime)
            {
                isWorking = false;
                isRewardReady = true;
            }
        }
    }

    public override void Interact()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (Player.localPlayer == null) return;

        Debug.Log($"dataList 개수: {dataList.Count}");

        JobType userJob = Player.localPlayer.CurrentJobType;
        bool isSpecialist = (userJob == ownerJob);
        Debug.Log($"직업:{userJob}, ownerJob:{ownerJob}, isSpecialist:{isSpecialist}");

        List<WorkbenchData> availableData = new List<WorkbenchData>();

        foreach (var data in dataList)
        {
            if (GlobalUnlockManager.Instance != null && !GlobalUnlockManager.Instance.IsUnlocked(data.id))
            {
                Debug.Log($"잠금으로 필터링됨: {data.id}");
                continue;
            }
            if (isSpecialist) availableData.Add(data);
            else if (data.isBasic) availableData.Add(data);
        }

        Debug.Log($"availableData 개수: {availableData.Count}");
        if (WorkbenchUIManager.Instance != null)
        {
            string title = isSpecialist ? $"[전문가] {workbenchName}" : workbenchName;

            WorkbenchUIManager.Instance.OpenUI(title, availableData, this);
        }
    }

    public void StartCraftingProcess(WorkbenchData data)
    {
        Inventory myInventory = FindAnyObjectByType<Inventory>();
        Condition myCondition = Player.localPlayer.condition;

        if (data is CookingRecipe cook)
        {
            foreach (var ing in cook.ingredients) myInventory.ConsumeItemByID(ing.itemID, ing.requiredAmount);
            maxTime = cook.cookTime;
        }
        else if (data is EquipRecipe equip)
        {
            foreach (var ing in equip.ingredients) myInventory.ConsumeItemByID(ing.itemID, ing.requiredAmount);
            maxTime = equip.craftTime;
        }
        else if (data is CollectionData col)
        {
            foreach (var ing in col.requiredItems) myInventory.ConsumeItemByID(ing.itemID, ing.requiredAmount);
            maxTime = col.researchTime;
        }
        else if (data is GymExercise gym)
        {
            if (myCondition != null)
            {
                myCondition.hunger -= gym.costHunger;
                if (myCondition.hunger < 0) myCondition.hunger = 0f;

                myCondition.thirst -= gym.costWater;
                if (myCondition.thirst < 0) myCondition.thirst = 0f;
            }
            maxTime = gym.exerciseTime;
        }

        currentTaskData = data;
        currentTimer = 0f;
        isRewardReady = false;
        isWorking = true;

        WorkbenchUIManager.Instance.RefreshDetail();
    }

    public void ClaimReward()
    {
        Inventory myInventory = FindAnyObjectByType<Inventory>();
        Condition myCondition = Player.localPlayer.condition;
        WorkbenchData data = currentTaskData;

        if (data is CookingRecipe cook)
        {
            myInventory.GetItem(cook.resultItemID, cook.resultAmount);
        }
        else if (data is EquipRecipe equip)
        {
            myInventory.GetItem(equip.resultEquipID, equip.resultAmount);
        }
        else if (data is CollectionData col)
        {
            if (col.rewardStoryItemID > 0) myInventory.GetItem(col.rewardStoryItemID, 1);
            if (GlobalUnlockManager.Instance != null && !string.IsNullOrEmpty(col.id))
                GlobalUnlockManager.Instance.UnlockItem(col.id);
        }
        else if (data is GymExercise gym)
        {
            if (myCondition != null)
            {
                myCondition.ApplyGymExercise(0, 0, gym.bonusMaxHP, gym.bonusMaxStamina);
            }
        }

        currentTaskData = null;
        isRewardReady = false;
        isWorking = false;

        WorkbenchUIManager.Instance.RefreshDetail();
    }
}