using Photon.Realtime;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatTest : MonoBehaviourPunCallbacks
{
    public List<string> chatList = new List<string>();
    public Button sendBtn;
    public Text chatLog;
    Text chattingList;
    public InputField input;
    string chatters;
    ScrollRect scroll_rect;


    // Start is called before the first frame update
    void Start()
    {
        PhotonNetwork.IsMessageQueueRunning = true;
        scroll_rect = GameObject.FindObjectOfType<ScrollRect>();
    }

    public void SendButtonOnClicked()
    {
        if (input.text.Equals(""))
        {
            Debug.Log("Empty");
            return;
        }


        string msg = string.Format("[{0}]{1}", PhotonNetwork.LocalPlayer.NickName, input.text);
        photonView.RPC("ReceiveMsg", RpcTarget.OthersBuffered, msg);
        ReceiveMsg(msg);
    }


    // Update is called once per frame
    void Update()
    {
        //chatterUpdate();
        if (Input.GetKeyDown(KeyCode.Return) && !input.isFocused)
            SendButtonOnClicked();
    }

    void chatterUpdate()
    {
        chatters = "Player List\n";
        foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerList)
        {
            chatters += p.NickName + "\n";
        }
        chattingList.text = chatters;
    }


    [PunRPC]
    public void ReceiveMsg(string msg)
    {
        chatLog.text += msg + "\n";
        scroll_rect.verticalNormalizedPosition = 0.0f;
    }



}
