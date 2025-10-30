using UnityEngine;
using TMPro;

namespace SneakyGame.UI
{
    public class RoundEndUI : MonoBehaviour
    {
        public static RoundEndUI Instance;

        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI winnerText;

        private void Awake() => Instance = this;

        public void ShowWinner(string message)
        {
            panel.SetActive(true);
            winnerText.text = message;
            Invoke(nameof(Hide), 4f);
        }

        private void Hide() => panel.SetActive(false);
    }
}
