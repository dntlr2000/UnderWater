using UnityEngine;

public abstract class Character : MonoBehaviour
{
    [Header("캐릭터 기본 속성")]
    public float moveSpeed; //이동속도
    public float health;    //체력
    public float atkPower;  //공격력

    //공격 메서드
    public virtual void Attack()
    {
        Debug.Log($"{gameObject.name}이 {atkPower}만큼의 피해를 가했습니다.");
    }

    public virtual void TakeDamage(float damage)
    {
        health -= damage;
        Debug.Log($"{gameObject}가 {damage}만큼의 피해를 입었습니다.");

        if(health <= 0)
        {
            Death();
        }
    }
    //사망 메서드
    protected abstract void Death();
}
