using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Events;

namespace SneakyGame.Network
{
    /// <summary>
    /// Manages lobby discovery and browsing for local network games
    /// Handles creating, finding, and joining game lobbies
    /// </summary>
    public class LobbyBrowser : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float refreshInterval = 2f;
        [SerializeField] private float lobbyTimeout = 10f;
        [SerializeField] private int defaultMaxPlayers = 12;

        [Header("Events")]
        public UnityEvent<List<LobbyData>> OnLobbiesUpdated = new UnityEvent<List<LobbyData>>();
        public UnityEvent<string> OnConnectionStatusChanged = new UnityEvent<string>();

        private Dictionary<string, LobbyData> discoveredLobbies = new Dictionary<string, LobbyData>();
        private LobbyData currentHostedLobby;
        private float nextRefreshTime;
        private bool isRefreshing = false;

        public List<LobbyData> AvailableLobbies => discoveredLobbies.Values.ToList();
        public bool IsHosting => currentHostedLobby != null;
        public LobbyData CurrentLobby => currentHostedLobby;

        private void Update()
        {
            // Auto-refresh lobby list
            if (isRefreshing && Time.realtimeSinceStartup >= nextRefreshTime)
            {
                RefreshLobbyList();
                nextRefreshTime = Time.realtimeSinceStartup + refreshInterval;
            }

            // Clean up stale lobbies
            CleanupStaleLobbies();

            // Update current lobby player count if hosting
            UpdateHostedLobby();
        }

        public void StartRefreshing()
        {
            isRefreshing = true;
            nextRefreshTime = Time.realtimeSinceStartup;
            OnConnectionStatusChanged?.Invoke("Searching for games...");
        }

        public void StopRefreshing()
        {
            isRefreshing = false;
            OnConnectionStatusChanged?.Invoke("Stopped searching");
        }

        public void RefreshLobbyList()
        {
            // In a real implementation, this would use Unity Transport's network discovery
            // For now, we simulate discovery by checking known addresses
            // This is a simplified version - production would use proper network discovery

            // Check if we're hosting
            if (IsHosting && currentHostedLobby != null)
            {
                currentHostedLobby.lastUpdateTime = Time.realtimeSinceStartup;

                if (!discoveredLobbies.ContainsKey(currentHostedLobby.lobbyId))
                {
                    discoveredLobbies[currentHostedLobby.lobbyId] = currentHostedLobby;
                }
            }

            OnLobbiesUpdated?.Invoke(AvailableLobbies);
        }

        public void CreateLobby(string hostName, int maxPlayers)
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("NetworkManager not found!");
                OnConnectionStatusChanged?.Invoke("Error: NetworkManager missing");
                return;
            }

            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
            {
                Debug.LogWarning("Already connected to a network session");
                OnConnectionStatusChanged?.Invoke("Error: Already connected");
                return;
            }

            // Create lobby data
            currentHostedLobby = new LobbyData(hostName, maxPlayers);
            currentHostedLobby.ipAddress = "127.0.0.1"; // localhost for now

            OnConnectionStatusChanged?.Invoke("Creating game...");

            // Configure transport
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.ConnectionData.Address = "127.0.0.1";
                transport.ConnectionData.Port = currentHostedLobby.port;
                transport.ConnectionData.ServerListenAddress = "0.0.0.0";
            }

            // Start as host
            bool success = NetworkManager.Singleton.StartHost();

            if (success)
            {
                // Subscribe to connection events
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

                discoveredLobbies[currentHostedLobby.lobbyId] = currentHostedLobby;
                OnConnectionStatusChanged?.Invoke($"Game created: {hostName}");
                OnLobbiesUpdated?.Invoke(AvailableLobbies);

                Debug.Log($"Lobby created: {hostName} ({maxPlayers} max players)");
            }
            else
            {
                currentHostedLobby = null;
                OnConnectionStatusChanged?.Invoke("Failed to create game");
                Debug.LogError("Failed to start host");
            }
        }

        public void JoinLobby(LobbyData lobby)
        {
            if (lobby == null || !lobby.CanJoin)
            {
                OnConnectionStatusChanged?.Invoke("Cannot join this game");
                return;
            }

            if (NetworkManager.Singleton == null)
            {
                OnConnectionStatusChanged?.Invoke("Error: NetworkManager missing");
                return;
            }

            OnConnectionStatusChanged?.Invoke($"Joining {lobby.hostName}...");

            // Configure transport
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.ConnectionData.Address = lobby.ipAddress;
                transport.ConnectionData.Port = lobby.port;
            }

            // Start as client
            bool success = NetworkManager.Singleton.StartClient();

            if (success)
            {
                OnConnectionStatusChanged?.Invoke($"Connected to {lobby.hostName}!");
                Debug.Log($"Joined lobby: {lobby.hostName}");
            }
            else
            {
                OnConnectionStatusChanged?.Invoke("Connection failed");
                Debug.LogError("Failed to join lobby");
            }
        }

        public void QuickJoin()
        {
            var availableLobby = AvailableLobbies.FirstOrDefault(l => l.CanJoin);

            if (availableLobby != null)
            {
                JoinLobby(availableLobby);
            }
            else
            {
                OnConnectionStatusChanged?.Invoke("No available games found");
                RefreshLobbyList();
            }
        }

        public void LeaveLobby()
        {
            if (NetworkManager.Singleton != null)
            {
                // Unsubscribe from events
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;

                NetworkManager.Singleton.Shutdown();
            }

            if (currentHostedLobby != null)
            {
                discoveredLobbies.Remove(currentHostedLobby.lobbyId);
                currentHostedLobby = null;
            }

            OnConnectionStatusChanged?.Invoke("Disconnected");
            OnLobbiesUpdated?.Invoke(AvailableLobbies);
        }

        private void OnClientConnected(ulong clientId)
        {
            if (currentHostedLobby != null && NetworkManager.Singleton.IsHost)
            {
                currentHostedLobby.currentPlayers = NetworkManager.Singleton.ConnectedClients.Count;

                if (currentHostedLobby.IsFull)
                {
                    currentHostedLobby.status = LobbyStatus.Full;
                }

                OnLobbiesUpdated?.Invoke(AvailableLobbies);
                Debug.Log($"Player joined. Current players: {currentHostedLobby.currentPlayers}");
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (currentHostedLobby != null && NetworkManager.Singleton.IsHost)
            {
                currentHostedLobby.currentPlayers = NetworkManager.Singleton.ConnectedClients.Count;

                if (currentHostedLobby.status == LobbyStatus.Full)
                {
                    currentHostedLobby.status = LobbyStatus.Waiting;
                }

                OnLobbiesUpdated?.Invoke(AvailableLobbies);
                Debug.Log($"Player left. Current players: {currentHostedLobby.currentPlayers}");
            }
        }

        private void UpdateHostedLobby()
        {
            if (currentHostedLobby != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
            {
                int connectedCount = NetworkManager.Singleton.ConnectedClients.Count;
                if (currentHostedLobby.currentPlayers != connectedCount)
                {
                    currentHostedLobby.currentPlayers = connectedCount;
                    currentHostedLobby.lastUpdateTime = Time.realtimeSinceStartup;
                    OnLobbiesUpdated?.Invoke(AvailableLobbies);
                }
            }
        }

        private void CleanupStaleLobbies()
        {
            var staleLobbies = discoveredLobbies.Values
                .Where(l => l != currentHostedLobby &&
                           Time.realtimeSinceStartup - l.lastUpdateTime > lobbyTimeout)
                .ToList();

            foreach (var staleLobby in staleLobbies)
            {
                discoveredLobbies.Remove(staleLobby.lobbyId);
            }

            if (staleLobbies.Count > 0)
            {
                OnLobbiesUpdated?.Invoke(AvailableLobbies);
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }
    }
}
