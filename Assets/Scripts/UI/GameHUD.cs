using UnityEngine;
using TMPro;
using System.Linq;

namespace SneakyGame.UI
{
    public class GameHUD : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI roleText;
        [SerializeField] private TextMeshProUGUI survivorsText;
        [SerializeField] private TextMeshProUGUI staminaText;

        private void Update()
        {
            if (Game.GameManager.Instance == null) return;

            timerText.text = $"Time: {Mathf.CeilToInt(Game.GameManager.Instance.GetTimeLeft())}s";

            var localPlayer = FindObjectsOfType<Game.PlayerState>().FirstOrDefault(p => p.IsOwner && p.CompareTag("Player"));
            if (localPlayer)
            {
                roleText.text = localPlayer.IsInfected.Value ? "INFECTED" : "SURVIVOR";

                // Show stamina
                if (localPlayer.TryGetComponent<Player.PlayerMovement>(out var movement))
                {
                    float stamina = movement.GetStamina();
                    float maxStamina = movement.GetMaxStamina();
                    staminaText.text = $"Stamina: {Mathf.RoundToInt((stamina / maxStamina) * 100)}%";

                    // Change color based on stamina level
                    if (stamina < 20f)
                        staminaText.color = Color.red;
                    else if (stamina < 50f)
                        staminaText.color = Color.yellow;
                    else
                        staminaText.color = Color.green;
                }
            }

            var players = FindObjectsOfType<Game.PlayerState>().Where(p => p.CompareTag("Player"));
            var survivors = players.Count(p => !p.IsInfected.Value);
            survivorsText.text = $"Survivors: {survivors}";
        }
    }
}
