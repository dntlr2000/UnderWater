using Photon.Pun;
using System;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour, ISavable
{
    public bool ifUsed = false; //세이브 데이터 반영
    public bool ifUsedTemp = false; //미반영
    public string PrefabPath => "SceneObject_ObjectSpawner";

    //생성되는 모든 오브젝트는 ISavable과 연동되어있어야 함
    [Header("Item Settings")]
    public int[] itemIDs;
    public int[] itemAmounts;
    public Vector3[] itemPositions;

    [Header("MonsterSettings")]
    public string[] monsterNames;
    public int[] monsterSpawnAmounts;
    public Vector3[] monsterSpawnPositions;

    [Header("StructureSettings")]
    public string[] structureNames;
    public Vector3[] structureLocations;
    public Quaternion[] structureRotations;

    protected PhotonView pv;

    [Serializable]
    public struct SpawnerSaveStruct
    {
        //public bool isHolding;
        public bool ifUsed;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (itemIDs.Length != itemAmounts.Length || itemIDs.Length != itemPositions.Length) Debug.LogError("[ObjectSpawner] 아이템 설정이 맞지 않습니다!");
        if (monsterNames.Length != monsterSpawnAmounts.Length || monsterNames.Length != monsterSpawnPositions.Length) Debug.LogError("[ObjectSpawner] 몬스터 설정이 맞지 않습니다!");
        pv = GetComponent<PhotonView>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GenerateObjects()
    {
        if (ifUsed == true) return;
        for (int i = 0; i < itemIDs.Length; i++)
        {
            GenerateItem(itemIDs[i], itemAmounts[i], itemPositions[i]);
        }
        for (int i = 0; i < monsterNames.Length; i++)
        {
            SpawnMonster(monsterNames[i], monsterSpawnAmounts[i], monsterSpawnPositions[i]);
        }
        /*
        for (int i = 0; i < structureLocations.Length; i++)
        {
            GenerateStructure(structureNames[i], structureLocations[i], structureRotations[i]);
        }
        */

        ifUsed = true;
        pv.RPC(nameof(PunRPC_SetSpawnerState), RpcTarget.All, true);
    }


    public void GenerateItem(int itemID, int amount, Vector3 location)
    {
        ItemDatabase.Instance.GenerateItemPhoton(1, 3, location);
    }

    public void SpawnMonster(string name, int amount, Vector3 location)
    {
        MonsterManager.Instance.SpawnMonsterPhoton(name, 1, location);
    }

    public string GetSaveDataJson()
    {
        SpawnerSaveStruct data = new SpawnerSaveStruct
        {
            ifUsed = this.ifUsed
        };
        return JsonUtility.ToJson(data);
    }

    public void RestoreSaveData(string json)
    {
        SpawnerSaveStruct data = JsonUtility.FromJson<SpawnerSaveStruct>(json);
        // 마스터 클라이언트가 복구하면서 다른 클라이언트에게도 동기화
        if (pv != null && PhotonNetwork.IsMasterClient)
        {
            pv.RPC(nameof(PunRPC_SetSpawnerState), RpcTarget.All, data.ifUsed);
        }
    }

    [PunRPC]
    public void PunRPC_SetSpawnerState(bool ifUsed)
    {
        this.ifUsed = ifUsed;
    }

    public void GenerateStructure(string name, Vector3 location, Quaternion rotation)
    {
        if (pv == null)
        {
            pv = GetComponent<PhotonView>();
            if (pv == null) { Debug.LogError("PhotonView가 존재하지 않습니다!"); }
        }


        pv.RPC(nameof(PunRPC_Master_InstantiateStructure), RpcTarget.MasterClient, name, location, rotation);
    }


    [PunRPC]
    public void PunRPC_Master_InstantiateStructure(string name, Vector3 location, Quaternion rotation)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        string prefabPath = $"Structures/{name}";
        if (Resources.Load(prefabPath) == null)
        {
            prefabPath = "Structures/Door";
        }
        GameObject droppedItem = PhotonNetwork.Instantiate(prefabPath, location, rotation);

    }

    private void OnTriggerEnter(Collider other)
    {
        if (!ifUsed)
        {
            if (other.tag == "Player")
            {
                Player player = other.GetComponent<Player>();
                if (player.photonView.IsMine) GenerateObjects();
            }
        }
        if (!ifUsedTemp)
        {
            if (other.tag == "Player")
            {
                Player player = other.GetComponent<Player>();
                if (player.photonView.IsMine)
                {
                    for (int i = 0; i < structureLocations.Length; i++)
                    {
                        GenerateStructure(structureNames[i], structureLocations[i], structureRotations[i]);
                    }
                }
            }
            ifUsedTemp = true;
        }
    }
}
