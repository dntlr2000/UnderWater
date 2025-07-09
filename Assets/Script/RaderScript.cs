using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using static UnityEngine.GraphicsBuffer;

public class RaderScript : MonoBehaviour
{
    public Camera radarCamera;        // 레이더 카메라
    public RectTransform radarUI;    // 레이더 UI 영역
    public GameObject radarDotPrefab; // 레이더 점 프리팹
    public Transform player;         // 플레이어 Transform
    public float radarRange = 50f;   // 레이더 범위

    private List<GameObject> radarDots = new List<GameObject>();
    private List<Transform> trackedObjects = new List<Transform>(); // 추적할 오브젝트들

    public RectTransform PlayerIcon;

    void Start()
    {
        //if (player == null) player = FindAnyObjectByType<Player>().transform;
        //player = FindAnyObjectByType<Player>().transform;
    }

    void Update()
    {
        if (player == null) player = FindAnyObjectByType<Player>().transform;
        RotatePlayerIcon();
        FindPingObjects("Ping");
        // 레이더 점 업데이트
        for (int i = 0; i < trackedObjects.Count; i++)
        {
            if (trackedObjects[i] == null) continue;

            Vector3 objectPosition = trackedObjects[i].position;
            Vector3 playerPosition = player.position;

            // 월드 좌표에서 레이더 좌표로 변환
            Vector3 relativePosition = objectPosition - playerPosition;

            // 레이더 범위를 벗어나는지 확인
            /*
            if (relativePosition.magnitude > radarRange)
            {
                radarDots[i].SetActive(false);

                continue;
            }
            */

            //radarDots[i].SetActive(true);

            // 레이더 UI 내 상대 위치 계산
            float normalizedX = relativePosition.x / radarRange;
            float normalizedZ = relativePosition.z / radarRange;

            // UI 영역 내에서 점 위치 설정
            Vector2 radarPosition = new Vector2(normalizedX, normalizedZ) * (radarUI.rect.width / 2);
            Image img = radarDots[i].GetComponent<Image>();
            Color col = img.color;
            if (relativePosition.magnitude > radarRange)
            {
                radarDots[i].GetComponent<RectTransform>().anchoredPosition = radarPosition.normalized * 10; //10 : 범위
                col.a = 0.3f;
                img.color = col;
            }
            else
            {
                radarDots[i].GetComponent<RectTransform>().anchoredPosition = radarPosition;
                col.a = 1f;
            }
            img.color = col;
        }
    }

    public void FindPingObjects(string tag)
    {
        var currentPings = new List<Transform>();
        foreach (var go in GameObject.FindGameObjectsWithTag(tag))
            currentPings.Add(go.transform);

        //핑 추가
        foreach (var t in currentPings)
        {
            if (!trackedObjects.Contains(t))
            {
                trackedObjects.Add(t);
                var dot = Instantiate(radarDotPrefab, radarUI);
                radarDots.Add(dot);
            }
        }

        //핑 삭제
        for (int i = trackedObjects.Count - 1; i >= 0; i--)
        {
            if (!currentPings.Contains(trackedObjects[i]))
            {
                Destroy(radarDots[i]);
                radarDots.RemoveAt(i);
                trackedObjects.RemoveAt(i);
            }
        }
    }

    public void RotatePlayerIcon()
    {
        if (player != null && PlayerIcon != null)
        {
            float PlayerRotation = player.eulerAngles.y;
            PlayerIcon.localEulerAngles = new Vector3(0, 0, -PlayerRotation);
        }
    }


}
