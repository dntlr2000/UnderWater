using System.Collections;
using UnityEngine;

public abstract class Character : InteractableObject
{
    [Header("캐릭터 기본 속성")]
    public float moveSpeed; //이동속도
    public float health;    //체력
    public float atkPower;  //공격력

    protected bool invincibleState = false;

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
}
