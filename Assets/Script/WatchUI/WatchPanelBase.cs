using UnityEngine;

public abstract class WatchPanelBase : MonoBehaviour
{
    [SerializeField] private WatchPanelType _panelType;
    public WatchPanelType PanelType => _panelType;

    public virtual void OnOpen() { }
    public virtual void OnClose() { }
    public virtual void RefreshData() { }

}
