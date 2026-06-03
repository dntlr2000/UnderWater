using System;
using System.IO;
using UnityEngine;


public class InventoryFrame : MonoBehaviour
{
    protected InventoryData inventoryData;
    public ItemUIManager ItemUI;

    protected string inventoryName;
    protected int INVENTORY_SIZE = 25;

    public virtual void GenerateData(int slots = 25, int equipSlots = 0)
    {
        inventoryData = new InventoryData();
        INVENTORY_SIZE = slots;
        inventoryData.GenerateData(slots + equipSlots);
        //GetItem(0, 1);
        Debug.Log("Inventory data generated");
        Debug.Log($"Generated InventorySize = {inventoryData.id.Length}");
    }

    public void GetItem(int id, int quantitiy = 1, float durability = -1)
    {
        Debug.Log($"InventorySIze = {INVENTORY_SIZE}, {inventoryData.id.Length}");
        //같은 아이템이 이미 존재하는지 확인 먼저 한 후 빈 슬롯을 찾기
        for (int i = 0; i < INVENTORY_SIZE; i++)
        {
            if (inventoryData.id[i] == id)
            {
                if (GetSingularity(i) == true) break;

                inventoryData.quantity[i] += quantitiy;
                Debug.Log($"Item added in slot{i}. current quantity = {inventoryData.quantity[i]}");
                ItemUI.SetQuantity(i, inventoryData.quantity[i]);
                return;
            }
        }

        for (int i = 0; i < INVENTORY_SIZE; i++) //비어있는 경우. 임시로 -1로 설정c
        {
            if (inventoryData.id[i] == -1)
            {
                Debug.Log($"Found empty slots. Slot index = {i}");
                inventoryData.quantity[i] = quantitiy;
                inventoryData.id[i] = id;
                if (durability != -1) inventoryData.durability[i] = durability;

                //inventoryData.item.LoadIcons(inventoryData.id[i]);

                Sprite ItemSprite = ItemDatabase.Instance.GetIcons(id);
                if (ItemSprite == null)
                {
                    Debug.LogWarning($"ID가 {id}인 아이템 아이콘을 찾을 수 없습니다.");
                    return;
                }
                if (ItemUI == null)
                {
                    Debug.LogWarning("ItemUI가 할당되지 않았습니다.");
                }

                ItemUI.LoadIcons(i, ItemSprite);
                ItemUI.SetQuantity(i, quantitiy);
                return;
            }
        }
    }

    public void RemoveItem(int index, int amount)
    {
        if (inventoryData.id[index] == -1)
        {
            return;
        }
        else
        {
            inventoryData.quantity[index] -= amount;
            ItemUI.SetQuantity(index, inventoryData.quantity[index]);

            if (inventoryData.quantity[index] <= 0)
            {
                inventoryData.id[index] = -1;
                ItemUI.ResetIcons(index);
            }
        }
    }

    public void RemoveAllItem(int index)
    {
        if (inventoryData.id[index] == -1)
        {
            return;
        }
        else
        {
            inventoryData.id[index] = -1;
            inventoryData.quantity[index] = 0;
            inventoryData.durability[index] = -1;
            Debug.Log($"Removed Item in slot {index}");

            ItemUI.ResetIcons(index);
        }
    }

    public virtual void MoveItemSlot(int before, int after)
    {
        if (inventoryData.id[before] == -1) return;
        Sprite ItemSpriteAfter;
        if (inventoryData.id[after] == -1) //옮기는 자리가 빈 자리임
        {
            if (after > INVENTORY_SIZE - 1 && !CheckEquipableItem(inventoryData.id[before])) return; //장비칸에다 착용 불가능한 장비를 착용하려 시도할 때

            inventoryData.id[after] = inventoryData.id[before];
            inventoryData.quantity[after] = inventoryData.quantity[before];
            inventoryData.durability[after] = inventoryData.durability[before];

            ItemSpriteAfter = ItemDatabase.Instance.GetIcons(inventoryData.id[after]);

            ItemUI.LoadIcons(after, ItemSpriteAfter);
            ItemUI.SetQuantity(after, inventoryData.quantity[after]);

            RemoveAllItem(before);
        }
        //각 위치에 아이템이 존재할 때
        else
        {
            if (after > INVENTORY_SIZE - 1 && !CheckEquipableItem(inventoryData.id[before])) return;
            if (before > INVENTORY_SIZE - 1 && !CheckEquipableItem(inventoryData.id[after])) return;

            int tempID = inventoryData.id[after];
            int tempQuantity = inventoryData.quantity[after];
            float tempDurability = inventoryData.durability[after];

            inventoryData.id[after] = inventoryData.id[before];
            inventoryData.quantity[after] = inventoryData.quantity[before];
            inventoryData.durability[after] = inventoryData.durability[before];

            inventoryData.id[before] = tempID;
            inventoryData.quantity[before] = tempQuantity;
            inventoryData.durability[before] = tempDurability;

            ItemSpriteAfter = ItemDatabase.Instance.GetIcons(inventoryData.id[after]);
            Sprite ItemSpriteBefore = ItemDatabase.Instance.GetIcons(inventoryData.id[before]);
            ItemUI.LoadIcons(after, ItemSpriteAfter);
            ItemUI.SetQuantity(after, inventoryData.quantity[after]);

            ItemUI.LoadIcons(before, ItemSpriteBefore);
            ItemUI.SetQuantity(before, inventoryData.quantity[before]);
        }

        Debug.Log($"Switched Items Slot {before} <-> Slot {after}");

    }

    public void GetMoney(int value)
    {
        if (inventoryData.money < 0 && value <= 0) return;
        inventoryData.money += value;
        if (inventoryData.money < 0) inventoryData.money = 0;
        ItemUI.UpdateMoney(inventoryData.money);
    }

    public int GetMoneyData()
    {
        return inventoryData.money;
    }

    public int GetItemID(int index)
    {
        //Debug.Log($"GetItemID가 호출되었습니다 : {inventoryData.id[index]}");
        return inventoryData.id[index];
    }

    public int GetQuantity(int index)
    {
        return inventoryData.quantity[index];
    }

    public string GetName(int index)
    {
        return inventoryData.GetItemName(index);
    }

    public Sprite GetIcon(int index)
    {
        return ItemDatabase.Instance.GetIcons(index);
    }

    public bool GetSingularity(int index)
    {
        return ItemDatabase.Instance.getSingularity(inventoryData.id[index]);
    }

    public void SetDurability(int index, float durability)
    {
        inventoryData.durability[index] = durability;
    }

    public float GetDurability(int index)
    {
        return inventoryData.durability[index];
    }

    public bool CheckEquipableItem(int itemId)
    {
        if (itemId == -1) return false;
        if (ItemDatabase.Instance.ifEquipable(itemId)) return true;
        else return false;
    }

    public virtual void LoadData()
    {
        inventoryData.LoadInventory(inventoryName);
    }

    public virtual void SaveData()
    {
        inventoryData.SaveInventory(inventoryName);
    }

    public bool CheckInventoryEmpty()
    {
        for (int i = 0; i < INVENTORY_SIZE; i++)
        {
            if (inventoryData.id[i] == -1)
            {
                return true;
            }
        }
        return false;
    }

    // 작업대 연결하려고 수정함
    public int GetOwnedItemCount(int itemID)
    {
        int count = 0;
        // 장비창(INVENTORY_SIZE 이후)은 제외하고 순수 인벤토리 슬롯만 검사
        if (inventoryData != null && inventoryData.id != null)
        {
            for (int i = 0; i < inventoryData.id.Length; i++)
            {
                if (inventoryData.id[i] == itemID)
                {
                    count += inventoryData.quantity[i];
                }
            }
        }
        return count;
    }

    public void ConsumeItemByID(int itemID, int amount)
    {
        int remainingAmount = amount;

        if (inventoryData == null || inventoryData.id == null) return;

        for (int i = 0; i < inventoryData.id.Length; i++)
        {
            if (inventoryData.id[i] == itemID)
            {
                int currentSlotAmount = inventoryData.quantity[i];

                if (currentSlotAmount >= remainingAmount)
                {
                    // 이 슬롯에서 필요한 만큼 전부 뺄 수 있으면 완료!
                    RemoveItem(i, remainingAmount);
                    return;
                }
                else
                {
                    // 이 슬롯의 개수가 부족하면 있는 거라도 다 빼고 다음 슬롯 계속 검색
                    remainingAmount -= currentSlotAmount;
                    RemoveItem(i, currentSlotAmount);
                }
            }
        }

        if (remainingAmount > 0)
        {
            Debug.LogWarning($"[경고] 아이템(ID: {itemID})이 {remainingAmount}개 부족해서 덜 뺐습니다!");
        }
    }
    // 작업대 연결하려고 수정함
}


[Serializable]
public class InventoryData
{
    public int[] quantity;
    public int[] id;
    public float[] durability;
    //[NonSerialized] // 명시적으로 직렬화에서 제외
    //public ItemDatabase item; //Serializable이 아니므로 이 값이 json으로 저장되진 않음

    public int money;

    public void GenerateData(int slots = 25)
    {
        quantity = new int[slots];
        id = new int[slots];
        durability = new float[slots];
        //item = new ItemDatabase();
        //item.GenerateData();

        for (int i = 0; i < quantity.Length; i++)
        {
            quantity[i] = 0;
            id[i] = -1;
            durability[i] = -1;
        }

        money = 0;

        //Debug.Log("InventoryData data generated");

    }

    public void GetItem(int slot, int id, int quantity = -1, float durability = -1)
    {
        this.id[slot] = id;
        this.quantity[slot] = quantity;
        this.durability[slot] = durability;
    }

    public void AddItem(int slot, int amount)
    {
        quantity[slot] += amount;
    }

    public void RemoveItem(int slot, int amount)
    {
        quantity[slot] -= amount;
        if (quantity[slot] <= 0) //아이템이 비워짐
        {
            quantity[slot] = 0;
            id[slot] = -1;
        }
    }

    public string GetItemName(int slot)
    {
        int itemId = id[slot];
        return ItemDatabase.Instance.getItemName(itemId);
    }

    //id는 그냥 id[slot]의 값

    //public int UseItem() //이 부분은 다른 스크립트에서 구현해야 할 내용인듯
    public void SaveInventory(string dataName = "inventory") //추후 멀티플레이어를 고려하여 경로를 고쳐야할듯
    {
        string path = Application.persistentDataPath + $"/{dataName}.json";
        InventoryData data = this;

        string json = JsonUtility.ToJson(data);
        File.WriteAllText(path, json);

        Debug.Log($"인벤토리 데이터 저장 경로: {path}");
    }
    
    public void LoadInventory(string dataName)
    {
        string path = Application.persistentDataPath + $"/{dataName}.json";
        if (!File.Exists(path))
        {
            Debug.LogWarning("저장된 인벤토리 정보가 없습니다.");
            return;
        }
        string json = File.ReadAllText(path);
        InventoryData data = JsonUtility.FromJson<InventoryData>(json);

        if (data.quantity != null) quantity = data.quantity;
        money = data.money;
        id = data.id;
        if (data.durability != null) durability= data.durability;
    }

    public void useItem(int index)
    {
        quantity[index] = ItemDatabase.Instance.UseItem(id[index], quantity[index]);
    }


    /* 버그의 원인이라 기존의 저장 시스템으로 롤백
    public void SaveToFile(string dataName)
    {
        string path = Application.persistentDataPath + $"/{dataName}.json";
        string json = JsonUtility.ToJson(this, true); // true를 넣어주면 보기 좋게 포맷팅됩니다.
        File.WriteAllText(path, json);
        Debug.Log($"인벤토리 데이터 저장 완료: {path}");
    }

    public static InventoryData LoadFromFile(string dataName)
    {
        string path = Application.persistentDataPath + $"/{dataName}.json";
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            InventoryData data = JsonUtility.FromJson<InventoryData>(json);
            Debug.Log($"인벤토리 데이터 로드 완료: {path}");
            return data;
        }
        return null;
    }
    */

    public bool CheckEquipableItem(int itemId)
    {
        return true;
    }

    public bool CheckInventoryEmpty()
    {
        for (int i = 0; i < id.Length; i++)
        {
            if (id[i] != -1)
            {
                return false;
            }
        }
        return true;
    }

}