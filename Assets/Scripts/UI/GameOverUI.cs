using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace SneakyGame.UI
{
    public class GameOverUI : MonoBehaviour
    {
        public static GameOverUI Instance;

        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI gameOverText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private Button returnToMenuButton;

        private void Awake()
        {
            Instance = this;

            // Ensure panel is hidden at start
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            // Setup button listener
            if (returnToMenuButton != null)
            {
                returnToMenuButton.onClick.AddListener(ReturnToMainMenu);
            }
        }

        public void ShowGameOver(int roundReached)
        {
            Debug.Log("<color=yellow>========== GAME OVER SCREEN SHOWING ==========</color>");

            if (gameOverPanel == null)
            {
                Debug.LogError("GameOverUI: gameOverPanel is not assigned!");
                return;
            }

            gameOverPanel.SetActive(true);
            Debug.Log($"Game Over Panel activated: {gameOverPanel.activeSelf}");

            if (gameOverText != null)
            {
                gameOverText.text = "GAME OVER";
            }

            if (statsText != null)
            {
                statsText.text = $"Your team survived until Round {roundReached}";
            }

            // Unlock and show cursor so player can click the button
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log($"Cursor unlocked. LockState: {Cursor.lockState}, Visible: {Cursor.visible}");

            // Pause the game (optional - uncomment if desired)
            // Time.timeScale = 0f;
        }

        private void ReturnToMainMenu()
        {
            Debug.Log("<color=cyan>========== RETURN TO MAIN MENU BUTTON CLICKED ==========</color>");

            // Unpause game before loading
            Time.timeScale = 1f;

            // Reset cursor to default state
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Shutdown multiplayer connection
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Debug.Log("Shutting down NetworkManager...");
                Unity.Netcode.NetworkManager.Singleton.Shutdown();
            }
            else
            {
                Debug.LogWarning("NetworkManager.Singleton is null!");
            }

            // Load main menu scene
            Debug.Log("Loading MainMenu scene...");
            SceneManager.LoadScene("MainMenu");
        }

        private void OnDestroy()
        {
            // Cleanup button listener
            if (returnToMenuButton != null)
            {
                returnToMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            }
        }
    }
}
