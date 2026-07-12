using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WatchUIController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject _watchRoot;
    [SerializeField] private KeyCode _toggleKey = KeyCode.Backspace;

    [Header("Game Integration")]
    [SerializeField] private UIController _uiController;

    [Header("Screens")]
    [SerializeField] private GameObject _homeScreen;
    [SerializeField] private GameObject _appGridScreen;

    [Header("Flash")]
    [SerializeField] private Image _flashImage;

    [Header("Panels")]
    [SerializeField] private List<WatchPanelBase> _panels;

    [Header("Buttons - Home")]
    [SerializeField] private Button _homeToGridButton;    // Screen_Home 자체 혹은 별도 버튼
    [SerializeField] private Button _centerHomeButton;    // Btn_Home_Center

    [Header("Buttons - Apps")]
    [SerializeField] private Button _btnQuest;


    [Header("Buttons - Back")]
    [SerializeField] private Button _backQuest;


    [Header("App Icon Colors")]
    [SerializeField] private Color _questColor = new Color(1f, 0.42f, 0.21f);
    [SerializeField] private Color _marketColor = new Color(0f, 0.78f, 0.59f);
    [SerializeField] private Color _journalColor = new Color(0.75f, 0.51f, 0.99f);
    [SerializeField] private Color _codexColor = new Color(0.98f, 0.75f, 0.14f);
    [SerializeField] private Color _statusColor = new Color(0.22f, 0.74f, 0.98f);

    private WatchUIAnimator _animator;
    private WatchPanelBase _currentPanel;
    private bool _isOpen;
    public bool IsOpen => _isOpen;

    private readonly Dictionary<WatchPanelType, Color> _panelColors
        = new Dictionary<WatchPanelType, Color>();

    private void Awake()
    {
        _animator = GetComponent<WatchUIAnimator>();
        if (_animator == null)
            _animator = gameObject.AddComponent<WatchUIAnimator>();

        BuildPanelColorMap();
        BindAllButtons();
        CloseAllPanelsImmediate();
        _watchRoot.SetActive(false);
    }

    private void BuildPanelColorMap()
    {
        _panelColors[WatchPanelType.Quest] = _questColor;
        _panelColors[WatchPanelType.Market] = _marketColor;
        _panelColors[WatchPanelType.Journal] = _journalColor;
        _panelColors[WatchPanelType.Codex] = _codexColor;
        _panelColors[WatchPanelType.Status] = _statusColor;
    }

    private void BindAllButtons()
    {
        // 홈 화면 → 앱 그리드
        _homeToGridButton?.onClick.AddListener(ShowAppGrid);

        // 앱 그리드 → 홈
        _centerHomeButton?.onClick.AddListener(ShowHome);

        // 앱 버튼
        _btnQuest?.onClick.AddListener(() => OpenPanel(WatchPanelType.Quest));


        // 뒤로 버튼
        _backQuest?.onClick.AddListener(CloseCurrentPanel);

    }

    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
        {
            // 워치가 이미 열려있으면 항상 닫기 허용
            if (_isOpen)
            {
                ToggleWatch();
                return;
            }

            // 닫혀있을 때는 다른 UI가 없을 때만 열기
            if (CanOpenWatch())
                ToggleWatch();
        }
    }
    private bool CanOpenWatch()
    {
        if (_uiController == null) return true;
        return _uiController.IsAllUIClosed();
    }

    public void ToggleWatch()
    {
        if (_isOpen) CloseWatch();
        else OpenWatch();
    }

    public void OpenWatch()
    {
        _isOpen = true;
        _watchRoot.SetActive(true);

        if (_uiController != null)
        {
            _uiController.LockCursor(false);      // 커서 표시
            _uiController.SetPlayerControl(false); // 플레이어 조작 잠금
        }

        ShowHome();
    }


    public void CloseWatch()
    {
        _isOpen = false;

        if (_currentPanel != null)
        {
            _currentPanel.OnClose();
            _currentPanel = null;
        }

        _watchRoot.SetActive(false);

        if (_uiController != null)
        {
            _uiController.LockCursor(true);       // 커서 숨김
            _uiController.SetPlayerControl(true);  // 플레이어 조작 해제
        }
    }

    public void ShowHome()
    {
        CloseAllPanelsImmediate();

        if (_appGridScreen.activeSelf)
        {
            _animator.PlayClose(_appGridScreen.GetComponent<RectTransform>(), () =>
            {
                _homeScreen.SetActive(true);
            });
        }
        else
        {
            _homeScreen.SetActive(true);
        }
    }

    public void ShowAppGrid()
    {
        _homeScreen.SetActive(false);
        _animator.PlayOpen(_appGridScreen.GetComponent<RectTransform>());
    }

    public void OpenPanel(WatchPanelType type)
    {
        if (_currentPanel != null)
        {
            _animator.PlayClose(_currentPanel.GetComponent<RectTransform>(), null);
            _currentPanel.OnClose();
            _currentPanel = null;
        }

        if (_panels == null || _panels.Count == 0)
        {
            Debug.LogError("[WatchUI] _panels 리스트가 비어있음");
            return;
        }

        var panel = _panels.Find(p => p.PanelType == type);

        _appGridScreen.SetActive(false);

        Color flashColor = _panelColors.TryGetValue(type, out var c) ? c : Color.white;

        void OpenPanelCore()
        {
            _currentPanel = panel;

            var rt = _currentPanel.GetComponent<RectTransform>();
            _currentPanel.OnOpen();
            _currentPanel.RefreshData();
            _animator.PlayOpen(_currentPanel.GetComponent<RectTransform>());

            if (_animator != null && rt != null)
                _animator.PlayOpen(rt);
            else
                _currentPanel.gameObject.SetActive(true);
        }

        if (_flashImage != null)
            _animator.PlayFlash(_flashImage, flashColor, OpenPanelCore);
        else
            OpenPanelCore();
    }

    public void CloseCurrentPanel()
    {
        if (_currentPanel == null) return;

        _animator.PlayClose(_currentPanel.GetComponent<RectTransform>(), () =>
        {
            _currentPanel.OnClose();
            _currentPanel = null;
            _appGridScreen.SetActive(true);
        });
    }

    private void CloseAllPanelsImmediate()
    {
        foreach (var panel in _panels)
        {
            if (panel == null) continue;
            panel.gameObject.SetActive(false);
        }
    }
}