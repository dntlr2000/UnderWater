using Photon.Pun;
using System;
using System.Collections;
using UnityEngine;
using static Character;
using static CylinderHolder;

public abstract class Character : InteractableObject, ISavable
{
    [Header("캐릭터 기본 속성")]
    public float moveSpeed; //이동속도
    public float health;    //체력
    public float atkPower;  //공격력

    protected bool invincibleState = false;
    //protected bool attackable = true; //Monster.cs에 lastattacktime이 이미 비슷한 기능을 수행함
    public string prefabPath = "";

    [Serializable]
    public struct CharacterSaveStruct
    {
        public float health;
        public Vector3 position;
        //public Quaternion rotation;
    }

    //공격 메서드
    public virtual void Attack()
    {
        //Debug.Log($"{gameObject.name}이 {atkPower}만큼의 피해를 가했습니다.");
    }

    public virtual void TakeDamage(float damage)
    {
        health -= damage;
        //Debug.Log($"{gameObject}가 {damage}만큼의 피해를 입었습니다.");

        if (health <= 0)
        {
            Death();
        }
    }
    //사망 메서드
    protected abstract void Death();


    public override InteractionType GetInteractionType() => InteractionType.Instant;

    public override void Interact() //상호작용
    {
        if (GetInteractionType() == InteractionType.Instant)
        {
            if (Input.GetMouseButtonDown(0)) //좌클
            {
                TakeDamage(10f); // 정상작동 될 경우 들고 있는 무기에 따라 데미지가 달라도록 할 예정
                //RPC_Deactivate();
                Debug.Log($"{objectName}을(를) 때렸습니다, 데미지: {10f}");
            }
        }

    }

    protected IEnumerator HitCoolTime(float time)
    {
        invincibleState = true;
        yield return new WaitForSeconds(time);
        invincibleState = false;
    }

    public void SetInvincible(float time)
    {
        if (time == -1) invincibleState = true;
        else if (time == 0) invincibleState = false;
        else
        {
            StartCoroutine(HitCoolTime(time));
        }
    }

    public virtual string PrefabPath {
        get
        {
            string path = prefabPath;
            if (Resources.Load(path) == null)
            {
                Debug.LogWarning("해당 경로에 캐릭터가 존재하지 않습니다. 기본 경로로 대체합니다.");
                return "Fish/_SharkV1";
            }
            return path;
        }
    }

    public string GetSaveDataJson()
    {
        CharacterSaveStruct data = new CharacterSaveStruct
        {
            health = this.health,
            position = transform.position,
            //rotation = transform.rotation
        };
        return JsonUtility.ToJson(data);
    }

    public void RestoreSaveData(string json)
    {
        CharacterSaveStruct data = JsonUtility.FromJson<CharacterSaveStruct>(json);
        // 마스터 클라이언트가 복구하면서 다른 클라이언트에게도 동기화
        if (pv != null && PhotonNetwork.IsMasterClient)
        {
            pv.RPC(nameof(PunRPC_SetCharacterProperties), RpcTarget.All, data.health, data.position);
        }
    }

    [PunRPC]
    public void PunRPC_SetCharacterProperties(float health, Vector3 position)
    {
        this.health = health;
        transform.position = position;
        //transform.rotation = rotation;
    }
}
