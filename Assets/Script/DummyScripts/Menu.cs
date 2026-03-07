using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menu : MonoBehaviour //메뉴 별로 공통으로 들어가는 버튼들 구현
{
    //[Header("Audio Sources")]
    public GameObject optionsPanel; //설정창
    public AudioManager audioManager; //오디오 매니저 할당

    [Header("로딩화면")]
    public GameObject loadingScreen; //로딩중 화면 가림판
    public Slider progressBar; //로딩 진척도
    //public TextMeshProUGUI loadingText; //로딩 진척도


    public void OpenOptions()
    {
        audioManager.StopBGM();
        optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        //audioManager.Play("BGM_Track1");
        optionsPanel.SetActive(false);
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        loadingScreen.SetActive(true);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            progressBar.value = progress;
            //loadingText.text = $"Loading.. {Mathf.RoundToInt(progress * 100)}";

            if (operation.progress >= 0.9f)
            {
                //loadingText.text = "Load Complete!";
                yield return new WaitForSeconds(1f);
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    public void CursorSwitch(bool isLock) //커서 나타낼지 말지
    {
        if (isLock == false) //커서 나타내기
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else //커서 가리기
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void QuitGame() //게임 종료
    {
        Debug.Log("Game Quit");
        Application.Quit();
    }
}
