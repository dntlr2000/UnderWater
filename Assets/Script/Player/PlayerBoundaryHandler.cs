using UnityEngine;
using Photon.Pun;
using System.Collections;
using TMPro;

public class PlayerBoundaryHandler : MonoBehaviourPun
{
    [Header("Dependencies")]
    public FirstViewCamera firstViewCamera; // 카메라 흔들기용
    private void Start()
    {
        if (!photonView.IsMine) return;

        if (firstViewCamera == null)
        {
            firstViewCamera = GetComponentInChildren<FirstViewCamera>();
        }
    }

    // 물리 충돌 감지 (투명 벽에 닿았을 때)
    private void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine) return;

        if (other.CompareTag("MapBoundary"))
        {
            Debug.Log($"<color=yellow>[충돌 테스트] 내가 닿은 오브젝트 이름: {other.gameObject.name} / 태그: {other.tag}</color>");
            TriggerBoundaryWarning();
        }
    }

    private void TriggerBoundaryWarning()
    {
        // 텍스트 없이 화면만 덜덜 흔들리게 합니다.
        if (firstViewCamera != null)
        {
            firstViewCamera.TriggerShake(0.5f, 0.2f);
            Debug.Log("맵 경계선에 닿았습니다! (화면 진동)");
        }
/*
        // 2. 경고 UI 띄우기 (코루틴 겹침 방지)
        if (warningCoroutine != null) StopCoroutine(warningCoroutine);
        warningCoroutine = StartCoroutine(ShowWarningTextRoutine());*/

        // 3. 체력 깎기 로직
        /*
        if (condition != null)
        {
            condition.Damaged(10f); 
            Debug.Log("경계선에 닿아 데미지를 입었습니다!");
        }
        */
    }
/*
    private IEnumerator ShowWarningTextRoutine()
    {
        warningText.enabled = true;
        yield return new WaitForSeconds(3f);
        warningText.enabled = false;
    }*/
}