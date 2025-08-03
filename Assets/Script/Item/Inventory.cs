using System;
using System.IO;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public int index; //현재 들고 있는 아이템
    private InventoryData inventoryData;
    public ItemUIManager ItemUI;
    public Transform IndexLine;

    //private bool showInventory = false; //ItemUI에서 일단 가져와봄 Update 부하를 줄이기 위해 ItemUI의 메서드를 여기로 옮길 수 있음

    void Start()
    {
        GenerateData();

        //GetItem(0, 1);
    }

    public void GenerateData()
    {
        inventoryData = new InventoryData();
        inventoryData.GenerateData();


        //GetItem(0, 1);
        Debug.Log("Inventory data generated");
    }

    // Update is called once per frame

    void Update()
    {
        /*
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (showInventory)
            {
                showInventory = false;
            }
            else
            {
                showInventory = true;
            }
        }
        */

        if (Input.GetKeyDown(KeyCode.Q))
        {
            //RemoveAllItem(index);
            RemoveItem(index, 1);
        }

        //들고 있는 아이템 변경하기
        Vector2 delta = Input.mouseScrollDelta;
        if (delta.y > 0f)
        {
            index += 1;
            if (index > 4) index = 4;
            IndexSetter();
        }
        else if (delta.y < 0f)
        {
            index -= 1;
            if (index < 0) index = 0;
            IndexSetter();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            index = 0;
            IndexSetter();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            index = 1;
            IndexSetter();
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            index = 2;
            IndexSetter();
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            index = 3;
            IndexSetter();
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            index = 4;
            IndexSetter();
        }

        //아이템 사용하기
        if (Input.GetMouseButtonDown(1))
        {
            if (inventoryData.id[index] < 0) return;

            inventoryData.useItem(index);
            ItemUI.SetQuantity(index, inventoryData.quantity[index]);
            if (inventoryData.quantity[index] <= 0)
            {
                inventoryData.id[index] = -1;
                ItemUI.ResetIcons(index);
            }

            
        }
    }

    private void FixedUpdate()
    {
        
    }

    private void IndexSetter()
    {
        IndexLine.localPosition = new Vector2(-140 + 70 * index, 0);
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

                Sprite ItemSprite = inventoryData.item.GetIcons(id);
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

    public void MoveItemSlot(int before, int after)
    {
        if (inventoryData.id[before] == -1) return;
        Sprite ItemSpriteAfter;
        if (inventoryData.id[after] == -1) //옮기는 자리가 빈 자리임
        {
            inventoryData.id[after] = inventoryData.id[before];
            inventoryData.quantity[after] = inventoryData.quantity[before];
            ItemSpriteAfter = inventoryData.item.GetIcons(inventoryData.id[after]);

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
            ItemSpriteAfter = inventoryData.item.GetIcons(inventoryData.id[after]);
            Sprite ItemSpriteBefore = inventoryData.item.GetIcons(inventoryData.id[before]);
            ItemUI.LoadIcons(after, ItemSpriteAfter);
            ItemUI.SetQuantity(after, inventoryData.quantity[after]);

            ItemUI.LoadIcons(before, ItemSpriteBefore);
            ItemUI.SetQuantity(before, inventoryData.quantity[before]);
        }

        Debug.Log($"Switched Items Slot {before} <-> Slot {after}");

    }

    public bool HoldingInteractableItem() //들고 있을 때 상호작용 가능한 아이템인지 확인 =>InteractableObject와 연계
    {
        if (inventoryData.id[index] == -1) return true;
        return inventoryData.item.getInteractable(inventoryData.id[index]);
    }
}

[Serializable]
public class InventoryData
{
    public int[] quantity;
    public int[] id;

    public ItemDatabase item; //Serializable이 아니므로 이 값이 json으로 저장되진 않음

    public void GenerateData()
    {
        quantity = new int[25];
        id = new int[25];
        item = new ItemDatabase();
        item.GenerateData();

        for (int i = 0; i < quantity.Length;i++)
        {
            quantity[i] = 0;
            id[i] = -1;
        }

        Debug.Log("InventoryData data generated");

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
            id[0] = -1;
        }
    }

    public string GetItemName(int slot)
    {
        int itemId = id[slot];
        return item.getItemName(itemId);
    }

    //id는 그냥 id[slot]의 값

    //public int UseItem() //이 부분은 다른 스크립트에서 구현해야 할 내용인듯
    public void SaveInventory() //추후 멀티플레이어를 고려하여 경로를 고쳐야할듯
    {
        string path = Application.persistentDataPath + "/inventory.json";
        InventoryData data = this;

        string json = JsonUtility.ToJson(data);
        File.WriteAllText(path, json);

        Debug.Log($"인벤토리 데이터 저장 경로: {path}");
    }

    public void LoadInventory()
    {
        string path = Application.persistentDataPath + "/inventory.json";
        if (!File.Exists(path))
        {
            Debug.LogWarning("저장된 인벤토리 정보가 없습니다.");
            return;
        }
        string json = File.ReadAllText(path);
        InventoryData data = JsonUtility.FromJson<InventoryData>(json);

        quantity = data.quantity;
        id = data.id;

    }

    public void useItem(int index)
    {
        quantity[index] = item.useItem(id[index], quantity[index]);
    }
}