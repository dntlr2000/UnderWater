using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ItemDatabase : MonoBehaviour 
{
    public List<ItemData> itemDatas = new List<ItemData>();
    private Dictionary<int, ItemData> itemDictionary;
    private PhotonView photonView;
    private Player player;

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
        LoadAllItems();
    }

    private void LoadAllItems()
    {
        // Resources/Items 폴더에 있는 모든 ItemData 타입의 에셋을 로드합니다.
        ItemData[] loadedItems = Resources.LoadAll<ItemData>("Data/ItemData");
        itemDatas = loadedItems.OrderBy(i => i.itemId).ToList();

        // 빠른 검색을 위해 ID 기반 딕셔너리 생성
        itemDictionary = new Dictionary<int, ItemData>();
        foreach (var item in itemDatas)
        {
            if (!itemDictionary.ContainsKey(item.itemId))
                itemDictionary.Add(item.itemId, item);
        }

        Debug.Log($"{itemDictionary.Count}개의 아이템 로드 완료.");
    }

    //protected abstract void useItem(); //아이템 기능을 이렇게 구현할까 싶기도
    public int UseItem(int itemId, int quantity)
    {
        ItemData data = GetItem(itemId);
        if (data == null) return quantity;

        if (player == null) player = FindAnyObjectByType<Inventory>().player;
        quantity = data.Use(player, quantity); // 각 아이템 클래스에 정의된 기능이 실행됨

        return quantity;
    }

    public ItemData GetItem(int id)
    {
        if (itemDictionary.TryGetValue(id, out ItemData item)) return item;
        return null;
    }

    public string getItemName(int itemId)
    {
        return GetItem(itemId).itemName;
    }

    public int getItemId(string itemName)
    {
        for (int i = 0; i < itemDatas.Count; i++)
        {
            if (itemName == itemDatas[i].itemName)
                return i;
        }
        Debug.LogWarning("아이템이 데이터에 존재하지 않습니다.");
        return -1;

    }

    public bool getInteractable(int itemId)
    {
        if (itemId == -1) return false;
        //return items[itemId].interactable;

        return GetItem(itemId).interactable;
    }

    public bool getSingularity(int itemId)
    {
        if (itemId == -1) return false;
        return GetItem(itemId).sigularity;
    }

    public int getPrice(int itemId)
    {
        if (itemId == -1) Debug.LogError("잘못된 값 입력");
        return GetItem(itemId).price;
    }
    

    public float getWeaponDamage(int itemId)
    {
        if (itemId == -1) return 10f; //기본 공격력
        return GetItem(itemId).damage;
    }

    public float getMaxDurability(int itemId)
    {
        if (itemId == -1) return -1;
        return GetItem(itemId).durability;
    }

    public bool ifEquipable(int itemId)
    {
        if (itemId == -1) return false;
        if (GetItem(itemId).type == "equipable") return true;
        return false;
    }

    public string GetItemType(int itemId)
    {
        if (itemId == -1) return "NULL";
        return GetItem(itemId).type;
    }

    public Sprite GetIcons(int itemId)
    {
        if (itemId == -1 || GetItem(itemId).itemIcon == null) return GetItem(0).itemIcon;
        return GetItem(itemId).itemIcon;
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

[CreateAssetMenu(fileName = "NewItem", menuName = "Items/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName; //아이템 이름
    public int itemId; //ID
    public string type = "item";
    public Sprite itemIcon;

    [Header("Attributes")]
    public int price; //개당 가격
    public float weight; //안쓸 것 같으면 삭제
    public float durability; //내구도. 아직 상세 부분은 미구현
    public bool interactable; //들고 있을 때 필드의 오브젝트와 상호작용할 수 있는 아이템인지 판단 여부 ex: 아이템 사용만 적용하고 싶은 경우 false
    public float damage = 10f;
    public bool sigularity;

    //public string itemFileName;
    //public string fieldItemName;
    public virtual int Use(Player player, int quantity)
    {
        //기본 기능 없음
        quantity--;
        return quantity;
    }
}
