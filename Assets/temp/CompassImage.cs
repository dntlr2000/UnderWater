using UnityEngine;

public class CompassImage : MonoBehaviour
{
    public RectTransform compassBar;  // 방위표 이미지의 RectTransform
    public RectTransform compassBarCopy; // 복제된 방위표 이미지 RectTransform
    public Transform player;          // 플레이어의 Transform
    public float compassWidth = 200f; // 방위표 이미지 하나의 가로 크기 (픽셀)
    public float visibleAngle = 200f; // 화면에 보이는 각도 범위

    public GameObject pingPrefab;    // 핑 UI Prefab (작은 아이콘)
    public Transform[] targets;     // 핑으로 표시할 오브젝트 리스트

    private RectTransform[] pings;   // 생성된 핑 UI 배열

    private void Start()
    {
        pings = new RectTransform[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            GameObject ping = Instantiate(pingPrefab, compassBar); // CompassBar에 핑 추가
            pings[i] = ping.GetComponent<RectTransform>();
        }

    }
    void Update()
    {
        // 플레이어의 방향 (Y축 기준)
        float angle = player.eulerAngles.y;

        // 방위표 위치 계산 (이미지 두 개를 연결)
        float offset = (angle / 360f) * compassWidth;
        float position = -offset % compassWidth + 10;

        // 두 이미지의 위치 업데이트
        compassBar.localPosition = new Vector3(position, compassBar.localPosition.y, compassBar.localPosition.z);
        compassBarCopy.localPosition = new Vector3(position - compassWidth, compassBarCopy.localPosition.y, compassBarCopy.localPosition.z);

        // 이미지가 끊어지지 않도록 무한 반복
        if (position < 0 && position > - compassWidth + 10)
        {
            compassBarCopy.localPosition += new Vector3(compassWidth * 2, 0, 0);
        }

        for (int i = 0; i < targets.Length; i++)
        {
            UpdatePingPosition(targets[i], pings[i], angle, position);
        }
    }

    void UpdatePingPosition(Transform target, RectTransform ping, float playerAngle, float c_position)
    {
        
        Vector3 direction = target.position - player.position;
        //Vector3 targetAngle = new Vector3(direction.x, 0, direction.z).normalized;
        float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        float relativeAngle = (targetAngle - playerAngle + 360f) % 360f; //음수를 나누는 일이 없도록 + 360을 한듯
        float temp = relativeAngle / 360f * compassWidth - c_position;
        //이걸로 방향은 구했다. -> 핑의 위치를 계산해야 한다!
        
        //오른쪽 범위를 벗어나면 앞으로 당겨야 함     
        if (relativeAngle > 180) {  //이게 왜 되는거지?
            temp -= compassWidth; //앞으로 당기는 코드
        }
        //ping.anchoredPosition = new Vector2(0, 10);
        ping.localPosition = new Vector2(temp, 10);
        ping.gameObject.SetActive(true);
        

    }

}
