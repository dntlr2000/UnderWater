using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RaderScript : MonoBehaviour
{
    public Camera radarCamera;        // 레이더 카메라
    public RectTransform radarUI;    // 레이더 UI 영역
    public GameObject radarDotPrefab; // 레이더 점 프리팹
    public Transform player;         // 플레이어 Transform
    public float radarRange = 50f;   // 레이더 범위

    private List<GameObject> radarDots = new List<GameObject>();
    private List<Transform> trackedObjects = new List<Transform>(); // 추적할 오브젝트들

    void Start()
    {
        // 초기화: 레이더에 표시할 오브젝트를 찾습니다.
        GameObject[] objects = GameObject.FindGameObjectsWithTag("RadarObject");
        foreach (var obj in objects)
        {
            trackedObjects.Add(obj.transform);
            GameObject radarDot = Instantiate(radarDotPrefab, radarUI);
            radarDots.Add(radarDot);
        }
    }

    void Update()
    {
        // 레이더 점 업데이트
        for (int i = 0; i < trackedObjects.Count; i++)
        {
            if (trackedObjects[i] == null) continue;

            Vector3 objectPosition = trackedObjects[i].position;
            Vector3 playerPosition = player.position;

            // 월드 좌표에서 레이더 좌표로 변환
            Vector3 relativePosition = objectPosition - playerPosition;

            // 레이더 범위를 벗어나는지 확인
            if (relativePosition.magnitude > radarRange)
            {
                radarDots[i].SetActive(false);
                continue;
            }

            radarDots[i].SetActive(true);

            // 레이더 UI 내 상대 위치 계산
            float normalizedX = relativePosition.x / radarRange;
            float normalizedZ = relativePosition.z / radarRange;

            // UI 영역 내에서 점 위치 설정
            Vector2 radarPosition = new Vector2(normalizedX, normalizedZ) * (radarUI.rect.width / 2);
            radarDots[i].GetComponent<RectTransform>().anchoredPosition = radarPosition;
        }
    }
}
