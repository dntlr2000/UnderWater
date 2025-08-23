using UnityEngine;

public class Portal : InteractableObject
{
    public override InteractionType GetInteractionType() => InteractionType.Gauge;
    public bool Interactable = true;

    public Vector3 coordinate = Vector3.zero;

    public override void Interact() //카메라가 이 오브젝트를 바라볼 때 호출됨
    {

        if (Interactable && Input.GetMouseButton(1))
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

        if (player== null)
        {
            Debug.LogWarning("Player 정보가 입력된 상태가 아닙니다.");
            return;
        }
        else
        {
            player.gameObject.transform.position = coordinate;

        }
    }


    //텔포 포톤 동기화 2안

    /*
    [PunRPC]
    public void RPC_Teleport(Vector3 newPos, Quaternion newRot)
    {
        var cc = GetComponent<CharacterController>();
        if (cc) cc.enabled = false;             // 충돌 이슈 회피용
        transform.SetPositionAndRotation(newPos, newRot);
        if (cc) cc.enabled = true;

        var rb = GetComponent<Rigidbody>();
        if (rb) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
    }

    public override void HoldInteract()
{
    if (player == null) { Debug.LogWarning("Player 정보가 없습니다."); return; }

    var pv = player.GetComponent<Photon.Pun.PhotonView>();
    if (pv == null) { Debug.LogWarning("Player에 PhotonView가 없습니다."); return; }

    if (!pv.IsMine) return; // 자신의 플레이어만 처리

    // 모든 클라에서 즉시 스냅
    pv.RPC("RPC_Teleport", RpcTarget.All, coordinate, player.transform.rotation);
}
    */
}
