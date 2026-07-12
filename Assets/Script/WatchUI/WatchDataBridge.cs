using UnityEngine;

public class WatchDataBridge : MonoBehaviour
{
    [SerializeField] private WatchUIController _watchUI;

    // 게임 데이터 매니저 참조 (실제 클래스로 교체)
    // [SerializeField] private QuestManager _questManager;
    // [SerializeField] private InventoryManager _inventoryManager;
    // [SerializeField] private PlayerStatusManager _playerStatus;

    public void BindToPanel(WatchPanelBase panel)
    {
        // 패널 타입별로 데이터 소스를 연결
        // switch (panel.PanelType)
        // {
        //     case WatchPanelType.Quest:
        //         ((QuestPanel)panel).SetDataSource(_questManager.GetActiveQuests());
        //         break;
        //     case WatchPanelType.Status:
        //         ((StatusPanel)panel).SetDataSource(_playerStatus.GetSnapshot());
        //         break;
        // }
    }
}