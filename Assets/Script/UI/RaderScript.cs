using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;

public class RaderScript : MonoBehaviour
{
    public Camera radarCamera;        // 레이더 카메라
    public RectTransform radarUI;    // 레이더 UI 영역
    public GameObject radarDotPrefab; // 레이더 점 프리팹
    public Transform player;         // 플레이어 Transform
    public float radarRange = 50f;   // 레이더 범위

    //오브젝트에 대한 핑
    private List<GameObject> radarDots = new List<GameObject>();
    private List<Transform> trackedObjects = new List<Transform>(); // 추적할 오브젝트들

    //플레이어에 대한 핑
    public GameObject otherPlayerDotPrefab; //다른 플레이어용 프리팹
    public RectTransform PlayerIcon;
    private Dictionary<int, GameObject> otherPlayerDots = new Dictionary<int, GameObject>();

    void Start()
    {

    }

    void Update()
    {
        //자신에 대한 정보 수집
        if (player == null)
        {
            Player playerScript = FindAnyObjectByType<Inventory>().player;
            if (playerScript == null) return;
            player = playerScript.transform;
        }
        RotatePlayerIcon();
        FindPingObjects("Ping");

        UpdateOtherPlayers();
    }

    public void FindPingObjects(string tag)
    {
        var currentPings = new List<Transform>();
        foreach (var go in GameObject.FindGameObjectsWithTag(tag))
            currentPings.Add(go.transform);

        foreach (var t in currentPings)
        {
            if (!trackedObjects.Contains(t))
            {
                trackedObjects.Add(t);
                var dot = Instantiate(radarDotPrefab, radarUI);
                radarDots.Add(dot);
            }
        }

        for (int i = trackedObjects.Count - 1; i >= 0; i--)
        {
            if (trackedObjects[i] == null || !currentPings.Contains(trackedObjects[i]))
            {
                if (radarDots[i] != null) Destroy(radarDots[i]);
                radarDots.RemoveAt(i);
                trackedObjects.RemoveAt(i);
                continue;
            }
        }

        UpdatePingPositions();
    }

    public void RotatePlayerIcon()
    {
        if (player != null && PlayerIcon != null)
        {
            float PlayerRotation = player.eulerAngles.y;
            PlayerIcon.localEulerAngles = new Vector3(0, 0, -PlayerRotation);
        }
    }

    void UpdateOtherPlayers()
    {
        // 최적화를 위해 매 프레임 Find 하는 것보다 PhotonNetwork.PlayerList와 연동하는 것이 더 좋으므로 나중에 수정
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        // 이번 프레임에 감지된 플레이어들의 ID 목록
        HashSet<int> currentFramePlayerIDs = new HashSet<int>();

        foreach (var pObj in players)
        {
            // PhotonView 컴포넌트 가져오기
            PhotonView pv = pObj.GetComponent<PhotonView>();

            // PhotonView가 없거나, 나 자신(IsMine)이라면 건너뜀
            if (pv == null || pv.IsMine) continue;

            if (pv.Owner.CustomProperties.TryGetValue("IsIndoor", out object isIndoor))
            {
                if ((bool)isIndoor)
                {
                    // 실내에 있는 플레이어는 레이더에서 제외 (이미 있다면 제거 로직으로 넘어감)
                    continue;
                }
            }

            int viewID = pv.ViewID;
            currentFramePlayerIDs.Add(viewID);

            // 2. 딕셔너리에 없다면 UI 새로 생성
            if (!otherPlayerDots.ContainsKey(viewID))
            {
                GameObject newDot = Instantiate(otherPlayerDotPrefab, radarUI);
                otherPlayerDots.Add(viewID, newDot);
            }

            //UI에 위치랑 회전 반영
            UpdatePlayerDotPositionAndRotation(pObj.transform, otherPlayerDots[viewID]);
        }

        //사라진 플레이어(연결 끊김 등) UI 제거
        List<int> existingIDs = new List<int>(otherPlayerDots.Keys);
        foreach (int id in existingIDs)
        {
            if (!currentFramePlayerIDs.Contains(id))
            {
                Destroy(otherPlayerDots[id]);
                otherPlayerDots.Remove(id);
            }
        }
    }

    void UpdatePlayerDotPositionAndRotation(Transform target, GameObject dotObj)
    {
        Vector3 relativePosition = target.position - player.position;

        float normalizedX = relativePosition.x / radarRange;
        float normalizedZ = relativePosition.z / radarRange;
        Vector2 radarPosition = new Vector2(normalizedX, normalizedZ) * (radarUI.rect.width / 2);

        Image img = dotObj.GetComponent<Image>();
        Color col = img.color;
        RectTransform rt = dotObj.GetComponent<RectTransform>();

        // 범위 벗어남 처리
        if (relativePosition.magnitude > radarRange)
        {
            rt.anchoredPosition = radarPosition.normalized * (radarUI.rect.width / 2); // 테두리에 고정 (기존 코드의 10 대신 실제 반지름 사용 권장)
            col.a = 0.5f; // 반투명
        }
        else
        {
            rt.anchoredPosition = radarPosition;
            col.a = 1f;
        }
        img.color = col;

        float targetRotationY = target.eulerAngles.y;

        rt.localEulerAngles = new Vector3(0, 0, -targetRotationY);
    }

    void UpdatePingPositions()
    {
        for (int i = 0; i < trackedObjects.Count; i++)
        {
            if (trackedObjects[i] == null) continue;

            Vector3 relativePosition = trackedObjects[i].position - player.position;
            float normalizedX = relativePosition.x / radarRange;
            float normalizedZ = relativePosition.z / radarRange;
            Vector2 radarPosition = new Vector2(normalizedX, normalizedZ) * (radarUI.rect.width / 2);

            Image img = radarDots[i].GetComponent<Image>();
            Color col = img.color;

            if (relativePosition.magnitude > radarRange)
            {
                radarDots[i].GetComponent<RectTransform>().anchoredPosition = radarPosition.normalized * (radarUI.rect.width / 2);
                col.a = 0.3f;
            }
            else
            {
                radarDots[i].GetComponent<RectTransform>().anchoredPosition = radarPosition;
                col.a = 1f;
            }
            img.color = col;
        }
    }

    public void SetCenter(Transform newCenter)
    {
        player = newCenter;
    }
}
