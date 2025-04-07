using UnityEngine;

public class ItemTest : MonoBehaviour
{
    //아이템 풀링 구현까진 된 것 같은데 정작 아이템 습득 방식이 줍는게 아니라 자동으로 인벤토리에 들어오는 방식이면?
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            Debug.Log("Get Item.");
            gameObject.SetActive(false);
        }
        
    }

    private void OnDisable()
    {
        Debug.Log("Disabled Item.");
        PoolManager.Instance.ReturnToPool("Item", gameObject);

    }
}
