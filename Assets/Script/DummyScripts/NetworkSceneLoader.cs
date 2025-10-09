using UnityEngine;

public class NetworkSceneLoader : MonoBehaviour
{
    void Awake()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "LobbyScene")
        {
            gameObject.AddComponent<NetworkManager>();
        }
        else if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MYJ")
        {
            gameObject.AddComponent<InGameNetworkManager>();
        }
    }
}
