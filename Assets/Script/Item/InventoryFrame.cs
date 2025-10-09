using System;
using System.IO;
using UnityEngine;

public class InventoryFrame : MonoBehaviour
{
    protected InventoryData inventoryData;
    public ItemUIManager ItemUI;

    protected string inventoryName;

    public virtual void GenerateData()
    {
        inventoryData = new InventoryData();
        inventoryData.GenerateData();

        //GetItem(0, 1);
        Debug.Log("Inventory data generated");
    }

    public void GetItem(int id, int quantitiy = 1, int durability = -1)
    {
        //같은 아이템이 이미 존재하는지 확인 먼저 한 후 빈 슬롯을 찾기
        for (int i = 0; i < inventoryData.id.Length; i++)
        {
            if (inventoryData.id[i] == id)
            {
                inventoryData.quantity[i] += quantitiy;
                Debug.Log($"Item added in slot{i}. current quantity = {inventoryData.quantity[i]}");
                ItemUI.SetQuantity(i, inventoryData.quantity[i]);
                return;
            }
        }

        for (int i = 0; i < inventoryData.id.Length; i++) //비어있는 경우. 임시로 -1로 설정c
        {
            if (inventoryData.id[i] == -1)
            {
                Debug.Log($"Found empty slots. Slot index = {i}");
                inventoryData.quantity[i] = quantitiy;
                inventoryData.id[i] = id;

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
            Debug.Log($"Removed Item in slot {index}");

            ItemUI.ResetIcons(index);
        }
    }

    public void MoveItemSlot(int before, int after)
    {
        if (inventoryData.id[before] == -1) return;
        Sprite ItemSpriteAfter;
        if (inventoryData.id[after] == -1) //옮기는 자리가 빈 자리임
        {
            inventoryData.id[after] = inventoryData.id[before];
            inventoryData.quantity[after] = inventoryData.quantity[before];
            ItemSpriteAfter = ItemDatabase.Instance.GetIcons(inventoryData.id[after]);

            ItemUI.LoadIcons(after, ItemSpriteAfter);
            ItemUI.SetQuantity(after, inventoryData.quantity[after]);

            RemoveAllItem(before);
        }
        //각 위치에 아이템이 존재할 때
        else
        {
            int tempID = inventoryData.id[after];
            int tempQuantity = inventoryData.quantity[after];

            inventoryData.id[after] = inventoryData.id[before];
            inventoryData.quantity[after] = inventoryData.quantity[before];

            inventoryData.id[before] = tempID;
            inventoryData.quantity[before] = tempQuantity;
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

    public virtual void LoadData()
    {
        inventoryData.LoadInventory(inventoryName);
    }

    public virtual void SaveData()
    {
        inventoryData.SaveInventory(inventoryName);
    }
}


[Serializable]
public class InventoryData
{
    public int[] quantity;
    public int[] id;
    //[NonSerialized] // 명시적으로 직렬화에서 제외
    //public ItemDatabase item; //Serializable이 아니므로 이 값이 json으로 저장되진 않음

    public int money;

    public void GenerateData()
    {
        quantity = new int[25];
        id = new int[25];
        //item = new ItemDatabase();
        //item.GenerateData();

        for (int i = 0; i < quantity.Length; i++)
        {
            quantity[i] = 0;
            id[i] = -1;
        }

        money = 0;

        //Debug.Log("InventoryData data generated");

    }

    public void GetItem(int slot, int id, int quantity = 1)
    {
        this.id[slot] = id;
        this.quantity[slot] = quantity;
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

        quantity = data.quantity;
        money = data.money;
        id = data.id;

    }

    public void useItem(int index)
    {
        quantity[index] = ItemDatabase.Instance.useItem(id[index], quantity[index]);
    }

    public void InitializeItemDatabase()
    {
        if (ItemDatabase.Instance == null)
        {
            ItemDatabase.Instance.GenerateData();
        }
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

}