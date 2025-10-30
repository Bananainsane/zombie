using SneakyGame.Network;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SneakyGame.UI
{
    /// <summary>
    /// Simple multiplayer menu with Create Game and Quick Join
    /// </summary>
    public class MultiplayerMenuUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Button createGameButton;
        [SerializeField] private Button quickJoinButton;
        [SerializeField] private GameObject statusPanel;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Settings")]
        [SerializeField] private int maxPlayers = 12;
        [SerializeField] private string gameSceneName = "GameScene";

        private bool isConnecting = false;

        private void Start()
        {
            SetupButtons();
            HideStatus();
        }

        private void SetupButtons()
        {
            if (createGameButton != null)
                createGameButton.onClick.AddListener(OnCreateGame);

            if (quickJoinButton != null)
                quickJoinButton.onClick.AddListener(OnQuickJoin);
        }

        private void OnCreateGame()
        {
            var nm = Unity.Netcode.NetworkManager.Singleton;

            if (nm == null)
            {
                ShowStatus("NetworkManager missing!");
                return;
            }

            // If already running, shut down first
            if (nm.IsClient || nm.IsServer)
            {
                Debug.Log("[CREATE GAME] Shutting down previous session...");
                nm.Shutdown();
                ShowStatus("Cleaning up...");
                Invoke(nameof(StartHostDelayed), 1.0f); // Increased delay for proper cleanup
                return;
            }

            StartHostDelayed();
        }

        private void StartHostDelayed()
        {
            var nm = Unity.Netcode.NetworkManager.Singleton;
            if (nm == null)
            {
                ShowStatus("NetworkManager missing!");
                return;
            }

            ShowStatus("Creating game...");
            isConnecting = true;

            // Ensure clean state
            if (nm.IsClient || nm.IsServer)
            {
                Debug.Log("[CREATE GAME] Double-check shutdown...");
                nm.Shutdown();
            }

            // Delay to ensure port is fully released by OS
            Invoke(nameof(ActuallyStartHost), 0.5f);
        }

        private void ActuallyStartHost()
        {
            var nm = Unity.Netcode.NetworkManager.Singleton;
            if (nm == null)
            {
                ShowStatus("NetworkManager missing!");
                isConnecting = false;
                return;
            }

            bool success = nm.StartHost();

            if (success)
            {
                Debug.Log("[CREATE GAME] Host started successfully! Loading GameScene...");
                ShowStatus("Game created! Loading...");

                // Load GameScene through NetworkManager for proper player spawning
                nm.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
            {
                ShowStatus("FAILED! Close Unity and restart if this persists");
                isConnecting = false;
                Debug.LogError("[CREATE GAME] Failed to start host - port 7777 in use!\n" +
                              "SOLUTION 1: Wait 5 seconds and click CREATE GAME again\n" +
                              "SOLUTION 2: Stop play mode and start again\n" +
                              "SOLUTION 3: Close Unity completely and reopen");
            }
        }

        private void OnQuickJoin()
        {
            var nm = Unity.Netcode.NetworkManager.Singleton;

            if (nm == null)
            {
                ShowStatus("NetworkManager missing!");
                return;
            }

            // If already running, shut down first
            if (nm.IsClient || nm.IsServer)
            {
                Debug.Log("[QUICK JOIN] Shutting down previous session...");
                nm.Shutdown();
                ShowStatus("Cleaning up...");
                Invoke(nameof(StartClientDelayed), 1.0f); // Increased delay
                return;
            }

            StartClientDelayed();
        }

        private void StartClientDelayed()
        {
            var nm = Unity.Netcode.NetworkManager.Singleton;
            if (nm == null)
            {
                ShowStatus("NetworkManager missing!");
                return;
            }

            ShowStatus("Joining game...");
            isConnecting = true;

            // Ensure clean state
            if (nm.IsClient || nm.IsServer)
            {
                Debug.Log("[QUICK JOIN] Double-check shutdown...");
                nm.Shutdown();
            }

            // Set to localhost
            var transport = nm.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport != null)
            {
                transport.ConnectionData.Address = "127.0.0.1";
                transport.ConnectionData.Port = 7777; // Connect to host on port 7777
            }

            // Delay to ensure clean state
            Invoke(nameof(ActuallyStartClient), 0.5f);
        }

        private void ActuallyStartClient()
        {
            var nm = Unity.Netcode.NetworkManager.Singleton;
            if (nm == null)
            {
                ShowStatus("NetworkManager missing!");
                isConnecting = false;
                return;
            }

            bool success = nm.StartClient();

            if (success)
            {
                Debug.Log("[QUICK JOIN] Client started - host will load GameScene automatically");
                ShowStatus("Connecting to host...");
                // NetworkManager.SceneManager will handle scene loading for clients
            }
            else
            {
                ShowStatus("Failed - is host running?");
                isConnecting = false;
                Debug.LogError("[QUICK JOIN] Failed to start client!\n" +
                              "Make sure someone clicked CREATE GAME first!");
            }
        }

        private void ShowStatus(string message)
        {
            if (statusPanel != null)
                statusPanel.SetActive(true);

            if (statusText != null)
                statusText.text = message;

            Debug.Log($"[Lobby] {message}");
        }

        private void HideStatus()
        {
            if (statusPanel != null)
                statusPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (createGameButton != null) createGameButton.onClick.RemoveAllListeners();
            if (quickJoinButton != null) quickJoinButton.onClick.RemoveAllListeners();

            // Cancel any pending invokes
            CancelInvoke();
        }

        private void OnApplicationQuit()
        {
            // Clean shutdown on quit
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                if (Unity.Netcode.NetworkManager.Singleton.IsServer || Unity.Netcode.NetworkManager.Singleton.IsClient)
                {
                    Unity.Netcode.NetworkManager.Singleton.Shutdown();
                }
            }
        }
    }
}
