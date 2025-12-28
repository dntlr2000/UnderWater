using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;


public class StructureBuilder : MonoBehaviour
{
    private PhotonView photonView;
    public GameObject[] Structures;

    private static StructureBuilder _instance;
    public static StructureBuilder Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<StructureBuilder>();
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

    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void BuildObject(int index, Vector3 position, Quaternion rotation)
    {
        if (index >= Structures.Length) { return; }
        Instantiate(Structures[index], position, rotation);
        string prefabPath = $"Structures/{Structures[index]}";
        PhotonNetwork.Instantiate(prefabPath, position, rotation);
    }

    
}
