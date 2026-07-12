using Photon.Pun;
using System;
using System.Collections;
using UnityEngine;
using static FieldItem;

public class CylinderHolder : InteractableObject, ISavable
{
    public bool isHolding = false;
    private GameObject holdingCylinder;
    private float currentDuration = 0f;
    private float MaxDuration = 0f;
    public float ChargeSpeed;
    private int ItemID = -1;

    [Serializable]
    public struct HolderSaveStruct
    {
        //public bool isHolding;
        public int itemID;
        public float durability;

        public Vector3 position;
        public Quaternion rotation;
    }

    public override void Interact()
    {
        if (Input.GetMouseButton(1))
        {
            UpdateGuage(true, holdDuration);
        }
        else
        {
            UpdateGuage(false, holdDuration);
        }
    }

    public override void HoldInteract()
    {
        TradeCylinder();
    }

    private void FixedUpdate()
    {
        ChargeOxygen();
    }

    public void ChargeOxygen()
    {
        if (!isHolding) return;
        currentDuration += ChargeSpeed;
        //Debug.Log($"산소통 충전중 : 최대치 - {MaxDuration}, 현재 - {currentDuration}");
        if (currentDuration > MaxDuration) currentDuration = MaxDuration;
    }

    public void TradeCylinder()
    {
        int playerHoldingID = GetItemIDFromPlayer();
        Debug.Log($"플레이어가 현재 들고 있는 아이템 ID = {playerHoldingID}");
        if (isHolding && playerHoldingID != -1) return; //PlayerHoldingID = -1 -> 빈손으로 들고 있어야지 산소통을 주울 수 있게 설정 되어 있음 -> 수정 예정
        else if (!isHolding)
        {
            if (playerHoldingID == -1) return;
            if (ItemDatabase.Instance.GetEquipEffectType(playerHoldingID) != "oxygen") return;

            RequestSetCylinder(playerHoldingID);
        }
        else
        {
            if (ItemID == -1) Debug.LogError("홀더에 아이템이 없으나 있는 것으로 취급하고 있습니다.");
            RequestRemoveCylinder();
        }
    }

    private void SetCylinder(int itemID)
    {
        isHolding = true;

        ItemID = itemID;
        currentDuration = GetDurationFromPlayer();
        MaxDuration = ItemDatabase.Instance.getMaxDurability(itemID);

        SetPrefab(ItemID);
        //inventory.RemoveItem(inventory.index, 1);
    }

    public void SetPrefab(int itemID)
    {
        string prefabPath = ItemDatabase.Instance.GetItem(itemID)?.modelPath;
        if (string.IsNullOrEmpty(prefabPath) || Resources.Load(prefabPath) == null)
        {
            prefabPath = "Structures/OxygenCylinder/Cylinder_Default";
        }
        holdingCylinder = Instantiate(Resources.Load<GameObject>(prefabPath), this.transform);
        if (holdingCylinder == null)
            holdingCylinder.transform.localPosition = new Vector3(0, 0, 0.01f);
    }

    public void RequestSetCylinder(int itemID)
    {
        if (inventory == null) inventory = FindAnyObjectByType<Inventory>();

        if (!usePhoton)
        {
            SetCylinder(itemID);
            inventory.RemoveItem(inventory.index, 1);
        }
        else
        {
            pv.RPC("PunRPC_SetCylinder", RpcTarget.AllBuffered, itemID, GetDurationFromPlayer());
            inventory.RemoveItem(inventory.index, 1);
        }
    }

    [PunRPC]
    public void PunRPC_SetCylinder(int itemID, float duration)
    {
        if (itemID == -1) return;
        SetItemProperties(itemID, duration, true);
        SetPrefab(itemID);
    }

    private void RemoveCylinder()
    {
        isHolding = false;

        Destroy(holdingCylinder);
        holdingCylinder = null;
        ItemID = -1;
    }

    private void RequestRemoveCylinder()
    {
        //호출 순서 때문에 템 복사 버그 우려 있긴 함
        if (inventory == null) inventory = FindAnyObjectByType<Inventory>();
        if (!inventory.CheckInventoryEmpty()) return;

        inventory.GetItem(ItemID, 1, currentDuration);

        if (!usePhoton)
        {
            RemoveCylinder();
        }
        else
        {
            //if (!PhotonNetwork.IsMasterClient) return;
            pv.RPC("PunRPC_RemoveCylinder", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    public void PunRPC_RemoveCylinder()
    {
        RemoveCylinder();
    }


    public void SetItemProperties(int id, float durability, bool isHolding)
    {
        this.ItemID = id;
        this.currentDuration = durability;
        this.MaxDuration = ItemDatabase.Instance.getMaxDurability(id);
        this.isHolding = isHolding;
    }

    public string PrefabPath => "SceneObject_CylinderHolder";

    public string GetSaveDataJson()
    {
        HolderSaveStruct data = new HolderSaveStruct
        {
            itemID = this.ItemID,
            durability = this.currentDuration,
            position = this.transform.position,
            rotation = this.transform.rotation
        };
        return JsonUtility.ToJson(data);
    }

    public void RestoreSaveData(string json)
    {
        HolderSaveStruct data = JsonUtility.FromJson<HolderSaveStruct>(json);
        // 마스터 클라이언트가 복구하면서 다른 클라이언트에게도 동기화
        if (pv != null && PhotonNetwork.IsMasterClient)
        {
            pv.RPC(nameof(PunRPC_SetCylinder), RpcTarget.All, data.itemID,  data.durability);
            pv.RPC(nameof(PunRPC_SetTransform), RpcTarget.All, data.position, data.rotation);
        }
    }
}
