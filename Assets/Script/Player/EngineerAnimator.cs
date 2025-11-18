using UnityEngine;
using Photon.Pun;
using Photon.Realtime;


public class EngineerAnimator : MonoBehaviour
{
    Rigidbody rb;
    public Animator animator;
    PhotonView pv;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        pv = GetComponent<PhotonView>();
    }

    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    #region 클라이언트 모션
    public void ResetTrigger()
    {
        if (animator == null) { Debug.LogError("애니메이터가 할당되지 않았습니다"); }

        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Jump");
        animator.ResetTrigger("Open");
        animator.SetFloat("yVelocity", 0);
    }

    public void SetOnWater(bool state)
    {
        if (animator == null) { Debug.LogError("애니메이터가 할당되지 않았습니다"); }
        animator.SetBool("onWater", state);
    }

    public void SetAirState(bool state)
    {
        animator.SetBool("onAir", state);
    }

    public void SetMove(bool move, bool isRunning)
    {
        animator.SetBool("Move", move);
        animator.SetBool("Run", isRunning);
    }

    public void SetSit(bool sit)
    {
        animator.SetBool("Sit", sit);
    }

    public void SetJump(bool state)
    {
        animator.SetTrigger("Jump");
        animator.SetBool("onAir", state);
    }

    public void SetAttack(int attackType)
    {
        animator.SetFloat("AttackType", attackType);
        animator.SetTrigger("Attack");
    }
    
    public void SetDown(bool state)
    {
        animator.SetBool("isDown", state);
    }

    public void ApplyVelocityY()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        animator.SetFloat("yVelocity", rb.linearVelocity.y);
    }
    public void ApplyVelocityY(float yVel)
    {
        animator.SetFloat("yVelocity", yVel);
    }
    #endregion

    #region 포톤 요청 및 응답

    public void RequestResetTrigger()
    {
        //PhotonView playerPhotonView = gameObject.GetComponent<PhotonView>();

        pv.RPC("PunRPC_ResetTrigger", RpcTarget.Others);

    }
    [PunRPC]
    private void PunRPC_ResetTrigger(PhotonMessageInfo info)
    {
        //if (!PhotonNetwork.IsMasterClient) return;

        if (gameObject == null) return;


        ResetTrigger();
    }

    public void RequestSetWaterState(bool state)
    {
        //PhotonView playerPhotonView = gameObject.GetComponent<PhotonView>();

        pv.RPC("PunRPC_SetWaterState", RpcTarget.Others, state);

    }
    [PunRPC]
    private void PunRPC_SetWaterState(bool state, PhotonMessageInfo info)
    {
        //if (!PhotonNetwork.IsMasterClient) return;

        if (gameObject == null) return;

        SetOnWater(state);
    }

    public void RequestSetAirState(bool state)
    {
        //PhotonView playerPhotonView = gameObject.GetComponent<PhotonView>();

        pv.RPC("PunRPC_SetAirState", RpcTarget.Others, state);

    }
    [PunRPC]
    private void PunRPC_SetAirState(bool state, PhotonMessageInfo info)
    {
        //if (!PhotonNetwork.IsMasterClient) return;

        if (gameObject == null) return;


        SetAirState(state);
    }

    public void RequestSetMoveState(bool move, bool isRunning)
    {
        //PhotonView playerPhotonView = gameObject.GetComponent<PhotonView>();

        pv.RPC("PunRPC_SetMoveState", RpcTarget.Others, move, isRunning);

    }
    [PunRPC]
    private void PunRPC_SetMoveState(bool move, bool isRunning, PhotonMessageInfo info)
    {
        //if (!PhotonNetwork.IsMasterClient) return;

        if (gameObject == null) return;


        SetMove(move, isRunning);
    }

    public void RequestSetSitState(bool state)
    {
        //PhotonView playerPhotonView = gameObject.GetComponent<PhotonView>();

        pv.RPC("PunRPC_SetSitState", RpcTarget.Others, state);

    }
    [PunRPC]
    private void PunRPC_SetSitState(bool state, PhotonMessageInfo info)
    {
        //if (!PhotonNetwork.IsMasterClient) return;

        if (gameObject == null) return;


        SetSit(state);
    }

    public void RequestSetJumpState(bool state)
    {
        //PhotonView playerPhotonView = gameObject.GetComponent<PhotonView>();

        pv.RPC("PunRPC_SetJumpState", RpcTarget.Others, state);

    }
    [PunRPC]
    private void PunRPC_SetJumpState(bool state, PhotonMessageInfo info)
    {
        //if (!PhotonNetwork.IsMasterClient) return;

        if (gameObject == null) return;


        SetJump(state);
    }

    public void RequestSetAttackState(int type)
    {
        //PhotonView playerPhotonView = gameObject.GetComponent<PhotonView>();

        pv.RPC("PunRPC_SetAttackState", RpcTarget.Others, type);

    }
    [PunRPC]
    private void PunRPC_SetAttackState(int type, PhotonMessageInfo info)
    {
        //if (!PhotonNetwork.IsMasterClient) return;

        if (gameObject == null) return;


        SetAttack(type);
    }

    public void RequestSetDownState(bool state)
    {
        //PhotonView playerPhotonView = gameObject.GetComponent<PhotonView>();

        pv.RPC("PunRPC_SetDownState", RpcTarget.Others, state);

    }
    [PunRPC]
    private void PunRPC_SetDownState(bool state, PhotonMessageInfo info)
    {
        //if (!PhotonNetwork.IsMasterClient) return;

        if (gameObject == null) return;


        SetDown(state);
    }

    public void RequestApplyVelocityY()
    {
        // --- 수정: 로컬 속도 값을 읽어서 RPC로 전송 ---
        if (rb == null) rb = GetComponent<Rigidbody>();
        float yVel = rb.linearVelocity.y;
        pv.RPC("PunRPC_ApplyVelocityY", RpcTarget.Others, yVel); // yVel 값을 함께 전송
        // --------------------------------------------
    }
    [PunRPC]
    private void PunRPC_ApplyVelocityY(float yVel, PhotonMessageInfo info) // yVel 값을 받음
    {
        if (gameObject == null) return;

        ApplyVelocityY(yVel); // Rigidbody를 읽는 대신 받은 값으로 애니메이션 적용
    }
    #endregion

}
