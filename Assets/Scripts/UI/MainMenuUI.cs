using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SneakyGame.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        private static bool asHost = false;

        private void Start()
        {
            Debug.Log("MainMenuUI Started - Wiring buttons...");

            // Wire up buttons at runtime
            GameObject.Find("PlayButton")?.GetComponent<Button>()?.onClick.AddListener(OnPlayClicked);
            GameObject.Find("ExitButton")?.GetComponent<Button>()?.onClick.AddListener(OnExitClicked);
            GameObject.Find("AddPlayerButton")?.GetComponent<Button>()?.onClick.AddListener(OnAddPlayerClicked);

            Debug.Log("Buttons wired!");
        }

        public void OnPlayClicked()
        {
            Debug.Log("PLAY clicked! Loading GameScene as Host...");
            asHost = true;
            SceneManager.LoadScene("GameScene");
        }

        public void OnAddPlayerClicked()
        {
            Debug.Log("ADD PLAYER clicked! Loading GameScene as Client...");
            asHost = false;
            SceneManager.LoadScene("GameScene");
        }

        public void OnExitClicked()
        {
            Debug.Log("EXIT clicked!");
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        // Called from GameScene to know if we should start as host or client
        public static bool ShouldStartAsHost() => asHost;
    }
}
