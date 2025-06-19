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


        if (Input.GetKeyDown(KeyCode.N))
        {
            GetItem(0, 1);
        }

        if (Input.GetKeyDown(KeyCode.B)) {
            GetItem(1, 3);
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            RemoveAllItem(index);
        }
        /*
        if (Input.GetKeyDown(KeyCode.WheelUp)) //이거 휠 굴리는 명령어가 아니었음;
        {
            index += 1;
            if (index > 4) index = 4;
            IndexSetter();
        }
        if (Input.GetKeyDown(KeyCode.WheelUp))
        {
            index -= 1;
            if (index < 0) index = 0;
            IndexSetter();
        }
        */

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

    }

    private void FixedUpdate()
    {
        
    }

    private void IndexSetter()
    {
        IndexLine.localPosition = new Vector2(-140 + 70 * index, 0);
    }

    public void GetItem(int id, int quantitiy = 1)
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

        for (int i = 0; i < inventoryData.id.Length; i++) //비어있는 경우. 임시로 -1로 설정
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

}

[Serializable]
public class InventoryData
{
    public int[] quantity;
    public int[] id;

    public ItemDatabase item; //Serializable이 아니므로 이 값이 json으로 저장되진 않음

    public void GenerateData()
    {
        quantity = new int[20];
        id = new int[20];
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
}