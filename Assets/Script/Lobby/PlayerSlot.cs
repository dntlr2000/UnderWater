using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerSlot : MonoBehaviour
{
    [Header("Portrait")]
    [SerializeField] private Image _portraitImage;
    [SerializeField] private GameObject _selectAgentLabel;

    [Header("Job Tag")]
    [SerializeField] private GameObject _jobTag;
    [SerializeField] private Image _jobIcon;
    [SerializeField] private TMP_Text _jobNameText;

    [Header("Nickname Tag")]
    [SerializeField] private GameObject _nicknameTag;
    [SerializeField] private TMP_Text _nicknameText;

    public void SetEmpty()
    {
        if (_portraitImage != null) _portraitImage.enabled = false;
        if (_selectAgentLabel != null) _selectAgentLabel.SetActive(true);
        if (_jobTag != null) _jobTag.SetActive(false);
        if (_nicknameTag != null) _nicknameTag.SetActive(false);
    }

    public void SetJoinedNoJob(string nickname)
    {
        if (_portraitImage != null) _portraitImage.enabled = false;
        if (_selectAgentLabel != null) _selectAgentLabel.SetActive(true);
        if (_jobTag != null) _jobTag.SetActive(false);

        if (_nicknameTag != null) _nicknameTag.SetActive(true);
        if (_nicknameText != null) _nicknameText.text = nickname;
    }

    public void SetJobSelected(string nickname, string jobName, Sprite jobIcon)
    {
        if (_portraitImage != null)
        {
            if (jobIcon != null) _portraitImage.sprite = jobIcon;
            _portraitImage.enabled = jobIcon != null;
        }
        if (_selectAgentLabel != null) _selectAgentLabel.SetActive(false);

        if (_jobTag != null) _jobTag.SetActive(true);
        if (_jobIcon != null) _jobIcon.sprite = jobIcon;
        if (_jobNameText != null) _jobNameText.text = jobName;

        if (_nicknameTag != null) _nicknameTag.SetActive(true);
        if (_nicknameText != null) _nicknameText.text = nickname;
    }
}