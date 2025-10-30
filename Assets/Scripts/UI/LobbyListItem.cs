using SneakyGame.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SneakyGame.UI
{
    /// <summary>
    /// UI component for displaying a single lobby entry in the browser
    /// </summary>
    public class LobbyListItem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI hostNameText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button joinButton;
        [SerializeField] private Image statusIndicator;

        [Header("Status Colors")]
        [SerializeField] private Color waitingColor = new Color(0.2f, 0.8f, 0.3f); // Green
        [SerializeField] private Color inProgressColor = new Color(1f, 0.6f, 0f); // Orange
        [SerializeField] private Color fullColor = new Color(0.8f, 0.2f, 0.2f); // Red

        private LobbyData lobbyData;
        private System.Action<LobbyData> onJoinCallback;

        public void Setup(LobbyData lobby, System.Action<LobbyData> onJoin)
        {
            lobbyData = lobby;
            onJoinCallback = onJoin;

            UpdateUI();

            if (joinButton != null)
            {
                joinButton.onClick.RemoveAllListeners();
                joinButton.onClick.AddListener(OnJoinClicked);
            }
        }

        private void UpdateUI()
        {
            if (lobbyData == null) return;

            // Update host name
            if (hostNameText != null)
            {
                hostNameText.text = lobbyData.hostName;
            }

            // Update player count
            if (playerCountText != null)
            {
                playerCountText.text = lobbyData.PlayerCountText;
            }

            // Update status
            if (statusText != null)
            {
                statusText.text = GetStatusString();
            }

            // Update status indicator color
            if (statusIndicator != null)
            {
                statusIndicator.color = GetStatusColor();
            }

            // Enable/disable join button
            if (joinButton != null)
            {
                joinButton.interactable = lobbyData.CanJoin;
            }
        }

        private string GetStatusString()
        {
            return lobbyData.status switch
            {
                LobbyStatus.Waiting => "Waiting",
                LobbyStatus.InProgress => "In Progress",
                LobbyStatus.Full => "Full",
                _ => "Unknown"
            };
        }

        private Color GetStatusColor()
        {
            return lobbyData.status switch
            {
                LobbyStatus.Waiting => waitingColor,
                LobbyStatus.InProgress => inProgressColor,
                LobbyStatus.Full => fullColor,
                _ => Color.gray
            };
        }

        private void OnJoinClicked()
        {
            onJoinCallback?.Invoke(lobbyData);
        }

        private void OnDestroy()
        {
            if (joinButton != null)
            {
                joinButton.onClick.RemoveAllListeners();
            }
        }
    }
}
