using Photon.Pun;
using System.Collections;
using UnityEngine;

public class CylinderHolder : InteractableObject
{
    public bool isHolding = false;
    private GameObject holdingCylinder;
    private float currentDuration = 0f;
    private float MaxDuration = 0f;
    public float ChargeSpeed;
    private int ItemID;


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
        if (isHolding && playerHoldingID != -1) return;
        else if (!isHolding)
        {
            if (playerHoldingID == -1) return;
            if (!ItemDatabase.Instance.ifEquipable(playerHoldingID)) return;

            RequestSetSylinder(playerHoldingID);
        }
        else
        {
            if (ItemID == -1) Debug.LogError("홀더에 아이템이 없으나 있는 것으로 취급하고 있습니다.");
            RequestRemoveSylinder();
        }
    }

    private void SetSylinder(int itemID)
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
        string prefabPath = $"Structures/OxygenCylinder/Cylinder{itemID}";
        if (Resources.Load(prefabPath) == null)
        {
            prefabPath = $"Structures/OxygenCylinder/Cylinder{5}"; // Fallback prefab
        }
        holdingCylinder = Instantiate(Resources.Load<GameObject>(prefabPath), this.transform);
        if (holdingCylinder == null)
            holdingCylinder.transform.localPosition = new Vector3(0, 0, 0.01f);
    }

    public void RequestSetSylinder(int itemID)
    {
        if (inventory == null) inventory = FindAnyObjectByType<Inventory>();

        if (!usePhoton)
        {
            SetSylinder(itemID);
            inventory.RemoveItem(inventory.index, 1);
        }
        else
        {
            pv.RPC("PunRPC_SetSylinder", RpcTarget.AllBuffered, itemID, GetDurationFromPlayer());
            inventory.RemoveItem(inventory.index, 1);
        }
    }

    [PunRPC]
    public void PunRPC_SetSylinder(int itemID, float duration)
    {
        SetItemProperties(itemID, duration, true);
        SetPrefab(itemID);
    }

    private void RemoveSylinder()
    {
        isHolding = false;

        Destroy(holdingCylinder);
        holdingCylinder = null;
        ItemID = -1;
    }

    private void RequestRemoveSylinder()
    {
        //호출 순서 때문에 템 복사 버그 우려 있긴 함
        if (inventory == null) inventory = FindAnyObjectByType<Inventory>();
        if (!inventory.CheckInventoryEmpty()) return;

        inventory.GetItem(ItemID, 1, currentDuration);

        if (!usePhoton)
        {
            RemoveSylinder();
        }
        else
        {
            //if (!PhotonNetwork.IsMasterClient) return;
            pv.RPC("PunRPC_RemoveSylinder", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    public void PunRPC_RemoveSylinder()
    {
        RemoveSylinder();
    }


    public void SetItemProperties(int id, float durability, bool isHolding)
    {
        this.ItemID = id;
        this.currentDuration = durability;
        this.MaxDuration = ItemDatabase.Instance.getMaxDurability(id);
        this.isHolding = isHolding;
        //Debug.Log($"[PROPERTY SET] Item properties received via RPC. ID set to {this.ItemID}, Amount to {this.currentDuration}");
    }
    
}
