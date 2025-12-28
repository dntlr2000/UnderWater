using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class ItemDatabase : MonoBehaviour //아이템 목록을 인터페이스나 abstract로 구현?
{
    public ItemData[] items = new ItemData[30];
    private Sprite[] ItemIcons = new Sprite[30];
    private PhotonView photonView;

    private static ItemDatabase _instance;
    public static ItemDatabase Instance
    {
        get
        {
            // 만약 인스턴스가 아직 없다면 씬에서 찾아봅니다.
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<ItemDatabase>();
                if (_instance == null)
                {
                    Debug.LogError("씬에 ItemDatabase 오브젝트가 존재하지 않습니다!");
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;          
        }
        GenerateData();
    }

    public void GenerateData()
    {
        for (int i = 0; i < items.Length; i++)
        {
            items[i] = new ItemData();
            items[i].itemId = i;
        }

        SetItemData(0, "Null Item", 0, 99, true, 0, 0f);
        SetItemData(1, "Item Box", 0, 99, false, 100, 10f);
        SetItemData(2, "Dring Can", 0, 99, false, 200, 10f);
        SetItemData(3, "Weapon", 0, 99, false, 300, 50f, true) ;
        SetItemData(4, "Key", 0, 99, false, 100, 10f, true);
        SetItemData(5, "OxygenCylinder_old", 0, 200, false, 0, 10f, true, "equipable");
        SetItemData(6, "OxygenSaver", 0, 100, false, 0, 10f, true, "equipable");

        LoadIcons(); //아이템 아이콘들을 먼저 전부 로딩

        //DebugListAllSprites("Item");
        Debug.Log("ItemDatabase data generated");

    }

    public void SetItemData(int index, string itemName, float weight = 0, float durability = 99, bool interactable = false, int price = 0, float damage = 10f, bool singularity = false, string type = "item")
    {
        //아이템 데이터 생성 양식
        items[index].itemName = itemName;
        items[index].weight = weight;
        items[index].durability = durability;
        items[index].interactable = interactable;
        items[index].price = price;
        items[index].damage = damage;
        items[index].sigularity = singularity;
        items[index].type = type;
    }

    public void LoadIcons()
    {
        //ItemIcons = new Sprite[30];
        //아이템 아이콘은 Resources/Item 폴더에 넣어두기
        //sprites = Resources.LoadAll<Sprite>("ItemIcons"); 전부 불러오기
        string path = "Item";

        for (int i = 0; i < items.Length;i++)
        {
            ItemIcons[i] = Resources.Load<Sprite>($"{path}/Item" + i);
            if (ItemIcons[i] == null)
            {
                ItemIcons[i] = Resources.Load<Sprite>($"{path}/Item0");
            }
            //if (ItemIcons[i] != null) Debug.Log($"Item ID = {i} Icon Load Complete");
        }
        //.png 확장자 생략
        //이후 db에 매칭되는 아이템 아이콘을 이렇게 불러오면 될듯
    }
    
    public Sprite LoadIcons(int index)
    {
        ItemIcons[index] = Resources.Load<Sprite>("Item/Item"+ index);

        if (ItemIcons[index] == null)
        {
            ItemIcons[index] = Resources.Load<Sprite>("Item/Item0");
        }

        if (ItemIcons[index] != null) Debug.Log($"Item ID = {index} Icon Load Complete");

        return ItemIcons[index];
    }

    void DebugListAllSprites(string folder) //경로에 파일들 확인용
    {
        var all = Resources.LoadAll<Sprite>(folder);
        Debug.Log($"[{folder}]에서 찾은 스프라이트 개수: {all.Length}");
        foreach (var s in all)
            Debug.Log($" → {s.name}");
    }


    public Sprite GetIcons(int index)
    {
        return ItemIcons[index];
    }

    //protected abstract void useItem(); //아이템 기능을 이렇게 구현할까 싶기도
    public int useItem(int itemId, int quantity)
    {
        //abstract로 아이템 별로 상속 받아서 기능을 구현하는 방법도 생각 해보았으나 스크립트 파일이 너무 많아질 것을 우려해서 이 방법을 선택함.
        //클라이언트에 가해지는 부담은 더 커지겠지만 유지보수면에선 더 수월할 것으로 판단.
        //아이템 별로 기능을 if 문으로 돌리면서 구현하는 것 외에 더 좋은 아이디어가 생각나면 수정할 예정..

        //내구도가 있는 아이템의 경우 내구도가 전부 소모되면 quantitiy 값을 0으로 반환
        if (itemId == -1) return 0;

        Debug.Log($"#Item {items[itemId].itemName} used.");

        Inventory inventory = FindAnyObjectByType<Inventory>(); //GetItem() 사용하기 위해
        Player player = inventory.player; //상태 변경 반영하기 위해
        if (inventory == null) { Debug.LogWarning($"ItemDatabase에서 인벤토리 스크립트를 찾을 수 없습니다."); return -1; }

        if (itemId == 0)
        {
            //임시용 아이템. 기능 없음.
            quantity -= 1;
        }

        else if (itemId == 1) //임시 아이템 박스
        {   
            inventory.GetItem(0, 3);
            quantity -= 1;
        }

        else if (itemId == 2) //임시 음료수
        {
            quantity-= 1;
            player.condition.getFood(10, 10);
        }

        else if (itemId == 3) //임시 칼
        {
            //아무 기능 없음. 내구도 현재 존재 안함
        }

        else if (itemId == 5) //산소통(구)
        {
            //일단 임시로 소모형 아이템으로 구현함
            //player.condition.chargeOxygen(inventory.GetDurability(inventory.index));
            //quantity -= 1;
        }

        else if (itemId == 6) //산소통
        {
            //일단 임시로 소모형 아이템으로 구현함
            //player.condition.chargeOxygen(inventory.GetDurability(inventory.index));
            //quantity -= 1;
        }

        return quantity; //소모형 아이템인 경우 quantity의 값을 소모된 만큼 빼고 반환받도록 함
        //장비 장착 효과에 대해서는 Condition.cs의 EquipEffect()에서 구현됨
    }

    public string getItemName(int itemId)
    {
        return items[itemId].itemName;
    }

    public int getItemId(string itemName)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (itemName == items[i].itemName)
                return i;
        }
        Debug.LogWarning("아이템이 데이터에 존재하지 않습니다.");
        return -1;

    }

    public bool getInteractable(int itemId)
    {
        if (itemId == -1) return false;
        return items[itemId].interactable;
    }

    public bool getSingularity(int itemId)
    {
        if (itemId == -1) return false;
        return items[itemId].sigularity;
    }

    public int getPrice(int itemId)
    {
        if (itemId == -1) Debug.LogError("잘못된 값 입력");
        return items[itemId].price;
    }
    /*
    public GameObject generateFieldItem(int itemId, Vector3 Location, int quantity = 1, bool ifPool = false)
    {
        string resourcesPath = "FieldItem/Object" + itemId;
        GameObject prefab = Resources.Load<GameObject>(resourcesPath);
        if (prefab == null)
        {
            Debug.LogWarning($"경로에 아이템이 존재하지 않습니다: {resourcesPath}");
            //기본값
            resourcesPath = "FieldItem/Object" + 1;
            prefab = Resources.Load<GameObject>(resourcesPath);
            if (prefab == null ) {
                Debug.LogError("기본값으로 아이템을 불러오려고 했으나 실패했습니다!");
            }
        }

        //생성
        GameObject go = Instantiate(prefab, Location, Quaternion.identity);
        //go.name = $"Item + {itemId}";

        //속성 지정
        FieldItem fieldScript = go.GetComponent<FieldItem>();
        if (fieldScript != null)
        {
            fieldScript.itemID = itemId;
            fieldScript.amount = quantity;
            fieldScript.ifPool = ifPool;
        }

        return go;
    }

    public GameObject generateFieldItem(GameObject prefab, Vector3 Location, int quantity, bool ifPool = false)
    {
        GameObject go = Instantiate(prefab, Location, Quaternion.identity);
        //go.name = $"Item + {itemId}";

        //속성 지정
        FieldItem fieldScript = go.GetComponent<FieldItem>();
        if (fieldScript != null)
        {
            //fieldScript.itemID = itemId;
            fieldScript.amount = quantity;
            fieldScript.ifPool = ifPool;
        }

        return go;
    }
    */ //GenerateItem(포톤 아닌거)

    public float getWeaponDamage(int itemId)
    {
        if (itemId == -1) return 10f; //기본 공격력
        return items[itemId].damage;
    }

    public float getMaxDurability(int itemId)
    {
        if (itemId == -1) return -1;
        return items[itemId].durability;
    }

    public bool ifEquipable(int itemId)
    {
        if (itemId == -1) return false;
        if (items[itemId].type == "equipable") return true;
        return false;
    }

    public string GetItemType(int itemId)
    {
        if (itemId == -1) return "NULL";
        return items[itemId].type;
    }

    public void GenerateItemPhoton(int itemID, int amount, Vector3 Location, float durability = -1f)
    {
        if (photonView == null) { 
            photonView = GetComponent<PhotonView>(); 
            if (photonView == null) { Debug.LogError("PhotonView가 존재하지 않습니다!"); }
        }
        

        int itemIDToDrop = itemID;
        int quantityToDrop = amount;

        Vector3 dropLocation = Location;

        photonView.RPC("PunRPC_Master_InstantiateDroppedItem", RpcTarget.MasterClient, itemIDToDrop, quantityToDrop, durability, dropLocation);
    }


    [PunRPC]
    public void PunRPC_Master_InstantiateDroppedItem(int itemID, int amount, float durability, Vector3 location)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        string prefabPath = $"FieldItem/Object{itemID}";
        if (Resources.Load(prefabPath) == null)
        {
            prefabPath = "FieldItem/Object1";
        }
        GameObject droppedItem = PhotonNetwork.Instantiate(prefabPath, location, Quaternion.identity);

        if (droppedItem != null)
        {
            PhotonView itemView = droppedItem.GetComponent<PhotonView>();
            if (itemView != null)
            {
                itemView.RPC("PunRPC_SetItemProperties", RpcTarget.All, itemID, amount, durability);
            }
            else
            {
                Debug.LogError($"Dropped item prefab '{prefabPath}' is missing a PhotonView component.");
            }
        }
    }
}

[Serializable]
public struct ItemData
{
    public string itemName; //아이템 이름
    public int itemId; //ID
    //public int quantity; //소지 개수 //인벤토리 매니저를 통해 관리하는게 더 나을듯?
    public int price; //개당 가격
    public float weight; //안쓸 것 같으면 삭제
    public float durability; //내구도. 아직 상세 부분은 미구현
    public bool interactable; //들고 있을 때 필드의 오브젝트와 상호작용할 수 있는 아이템인지 판단 여부 ex: 소모형 아이템
    public float damage;
    public bool sigularity;
    public string type;

    //public string itemFileName;
    //public string fieldItemName;
}
