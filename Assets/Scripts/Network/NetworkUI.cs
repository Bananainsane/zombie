using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SneakyGame.Network
{
    /// <summary>
    /// Enhanced Network UI with modern connection status display
    /// Works in conjunction with MultiplayerMenuUI for full lobby system
    /// Can also work standalone for quick testing
    /// </summary>
    public class NetworkUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject connectionPanel;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button clientButton;
        [SerializeField] private Button serverButton;
        [SerializeField] private Button disconnectButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private GameObject connectedPanel;

        [Header("Visual Feedback")]
        [SerializeField] private Image statusIndicator;
        [SerializeField] private Color connectedColor = new Color(0.2f, 0.8f, 0.3f);
        [SerializeField] private Color connectingColor = new Color(1f, 0.6f, 0f);
        [SerializeField] private Color disconnectedColor = new Color(0.8f, 0.2f, 0.2f);

        [Header("Settings")]
        [SerializeField] private bool hideUIOnConnect = true;
        [SerializeField] private bool showPlayerCount = true;

        private NetworkManager networkManager;
        private bool isConnected = false;

        private void Start()
        {
            networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                Debug.LogError("NetworkManager not found! Make sure NetworkManager exists in the scene.");
                UpdateStatusText("Error: NetworkManager missing", disconnectedColor);
                return;
            }

            SetupButtons();
            SubscribeToNetworkEvents();
            UpdateStatusText("Ready to connect", disconnectedColor);
            UpdateUIState();
        }

        private void SetupButtons()
        {
            if (hostButton != null) hostButton.onClick.AddListener(StartHost);
            if (clientButton != null) clientButton.onClick.AddListener(StartClient);
            if (serverButton != null) serverButton.onClick.AddListener(StartServer);
            if (disconnectButton != null) disconnectButton.onClick.AddListener(Disconnect);
        }

        private void SubscribeToNetworkEvents()
        {
            if (networkManager == null) return;

            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            networkManager.OnServerStarted += OnServerStarted;
        }

        private void Update()
        {
            if (showPlayerCount && networkManager != null && networkManager.IsServer)
            {
                UpdatePlayerCount();
            }
        }

        public void StartHost()
        {
            if (networkManager == null) return;
            UpdateStatusText("Starting as Host...", connectingColor);
            bool success = networkManager.StartHost();
            if (success)
            {
                UpdateStatusText("Host started successfully!", connectedColor);
                Debug.Log("Started as Host");
                isConnected = true;
                UpdateUIState();
            }
            else
            {
                UpdateStatusText("Failed to start Host", disconnectedColor);
                Debug.LogError("Failed to start as Host");
            }
        }

        public void StartClient()
        {
            if (networkManager == null) return;
            UpdateStatusText("Connecting as Client...", connectingColor);
            bool success = networkManager.StartClient();
            if (success)
            {
                UpdateStatusText("Connecting...", connectingColor);
                Debug.Log("Started as Client");
            }
            else
            {
                UpdateStatusText("Failed to connect as Client", disconnectedColor);
                Debug.LogError("Failed to start as Client");
            }
        }

        public void StartServer()
        {
            if (networkManager == null) return;
            UpdateStatusText("Starting as Server...", connectingColor);
            bool success = networkManager.StartServer();
            if (success)
            {
                UpdateStatusText("Server started!", connectedColor);
                Debug.Log("Started as Server");
                isConnected = true;
                UpdateUIState();
            }
            else
            {
                UpdateStatusText("Failed to start Server", disconnectedColor);
                Debug.LogError("Failed to start as Server");
            }
        }

        public void Disconnect()
        {
            if (networkManager == null) return;

            if (networkManager.IsServer || networkManager.IsHost)
            {
                networkManager.Shutdown();
                UpdateStatusText("Disconnected from server", disconnectedColor);
            }
            else if (networkManager.IsClient)
            {
                networkManager.Shutdown();
                UpdateStatusText("Disconnected from session", disconnectedColor);
            }

            isConnected = false;
            UpdateUIState();
        }

        private void OnClientConnected(ulong clientId)
        {
            if (networkManager.IsClient && !networkManager.IsHost && clientId == networkManager.LocalClientId)
            {
                UpdateStatusText("Connected!", connectedColor);
                isConnected = true;
                UpdateUIState();
            }
            Debug.Log($"Client {clientId} connected");
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (clientId == networkManager.LocalClientId)
            {
                UpdateStatusText("Disconnected", disconnectedColor);
                isConnected = false;
                UpdateUIState();
            }
            Debug.Log($"Client {clientId} disconnected");
        }

        private void OnServerStarted()
        {
            Debug.Log("Server started callback");
        }

        private void UpdatePlayerCount()
        {
            if (playerCountText != null && networkManager != null)
            {
                int count = networkManager.ConnectedClients.Count;
                playerCountText.text = $"Players: {count}";
            }
        }

        private void UpdateUIState()
        {
            // Show/hide connection panel based on connection state
            if (connectionPanel != null && hideUIOnConnect)
            {
                connectionPanel.SetActive(!isConnected);
            }

            // Show/hide connected panel
            if (connectedPanel != null)
            {
                connectedPanel.SetActive(isConnected);
            }

            // Update button interactability
            if (hostButton != null) hostButton.interactable = !isConnected;
            if (clientButton != null) clientButton.interactable = !isConnected;
            if (serverButton != null) serverButton.interactable = !isConnected;
            if (disconnectButton != null) disconnectButton.interactable = isConnected;
        }

        private void UpdateStatusText(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }

            if (statusIndicator != null)
            {
                statusIndicator.color = color;
            }

            Debug.Log($"NetworkUI: {message}");
        }

        private void UpdateStatusText(string message)
        {
            UpdateStatusText(message, connectingColor);
        }

        private void HideUI()
        {
            if (connectionPanel != null)
                connectionPanel.SetActive(false);
        }

        private void ShowUI()
        {
            if (connectionPanel != null)
                connectionPanel.SetActive(true);
        }

        private void OnGUI()
        {
            if (connectionPanel != null) return;
            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                if (GUILayout.Button("Host")) StartHost();
                if (GUILayout.Button("Client")) StartClient();
                if (GUILayout.Button("Server")) StartServer();
            }
            else
            {
                GUILayout.Label($"Mode: {GetConnectionMode()}");
                if (GUILayout.Button("Disconnect")) Disconnect();
            }
            GUILayout.EndArea();
        }

        private string GetConnectionMode()
        {
            if (networkManager.IsHost) return "Host";
            else if (networkManager.IsServer) return "Server";
            else if (networkManager.IsClient) return "Client";
            else return "Not Connected";
        }

        private void OnDestroy()
        {
            // Unsubscribe from network events
            if (networkManager != null)
            {
                networkManager.OnClientConnectedCallback -= OnClientConnected;
                networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
                networkManager.OnServerStarted -= OnServerStarted;
            }

            // Remove button listeners
            if (hostButton != null) hostButton.onClick.RemoveListener(StartHost);
            if (clientButton != null) clientButton.onClick.RemoveListener(StartClient);
            if (serverButton != null) serverButton.onClick.RemoveListener(StartServer);
            if (disconnectButton != null) disconnectButton.onClick.RemoveListener(Disconnect);
        }
    }
}
