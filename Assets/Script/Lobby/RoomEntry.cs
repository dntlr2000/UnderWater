using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomEntry : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _createdAtText;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private Image _lockIcon;
    [SerializeField] private GameObject _highlight;

    public Button Button => _button;

    public void SetEmpty()
    {
        gameObject.SetActive(false);
    }

    public void SetData(string roomName, string createdAt, int current, int max, bool hasPassword)
    {
        gameObject.SetActive(true);

        if (_nameText != null) _nameText.text = roomName;
        if (_createdAtText != null) _createdAtText.text = createdAt;
        if (_countText != null) _countText.text = $"{current} / {max}";
        if (_lockIcon != null) _lockIcon.enabled = hasPassword;
    }

    public void SetSelected(bool selected)
    {
        if (_highlight != null) _highlight.SetActive(selected);
    }
}