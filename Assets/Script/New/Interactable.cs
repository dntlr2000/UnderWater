using UnityEngine;
public enum InteractionType
{
    Instant,     //상호작용 시 즉시실행(침대, 컴퓨터)
    Gauge,       //게이지가 차는 상호작용(문, 채집)
}

public interface Interactable
{
    string GetCursorType(); //커서 이미지 바꾸기
    string GetInteractionID(); //무슨 오브젝트와 상호작용하는지 체크(개별)
    InteractionType GetInteractionType(); //상호작용 후 작동의 묶음 처리를 위한 타입


    void Interact();
}


