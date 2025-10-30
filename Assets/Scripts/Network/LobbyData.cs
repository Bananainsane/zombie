using System;
using Unity.Netcode;
using UnityEngine;

namespace SneakyGame.Network
{
    /// <summary>
    /// Data structure representing a multiplayer lobby/game session
    /// </summary>
    [Serializable]
    public class LobbyData
    {
        public string lobbyId;
        public string hostName;
        public int currentPlayers;
        public int maxPlayers;
        public LobbyStatus status;
        public string ipAddress;
        public ushort port;
        public float lastUpdateTime;

        public LobbyData(string hostName, int maxPlayers = 12)
        {
            this.lobbyId = Guid.NewGuid().ToString();
            this.hostName = hostName;
            this.currentPlayers = 1; // Host is always first player
            this.maxPlayers = maxPlayers;
            this.status = LobbyStatus.Waiting;
            this.port = 7777;
            this.lastUpdateTime = Time.realtimeSinceStartup;
        }

        public bool IsFull => currentPlayers >= maxPlayers;
        public bool CanJoin => !IsFull && status == LobbyStatus.Waiting;
        public string PlayerCountText => $"{currentPlayers}/{maxPlayers}";
    }

    public enum LobbyStatus
    {
        Waiting,
        InProgress,
        Full
    }
}
