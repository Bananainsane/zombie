using Unity.Netcode;
using UnityEngine;

namespace SneakyGame.Network
{
    /// <summary>
    /// Validates NetworkManager is running when GameScene loads
    /// Players spawn automatically via Unity Netcode when scene loads
    /// </summary>
    public class GameSceneStarter : MonoBehaviour
    {
        private void Start()
        {
            var nm = NetworkManager.Singleton;

            if (nm == null)
            {
                Debug.LogError("[GameScene] NetworkManager not found! Must start from MainMenu!");
                return;
            }

            // Just log status - NetworkManager already started from MainMenu
            if (nm.IsHost)
            {
                Debug.Log("[GameScene] Loaded as HOST - players will spawn automatically");
            }
            else if (nm.IsClient)
            {
                Debug.Log("[GameScene] Loaded as CLIENT - waiting for spawn...");
            }
            else
            {
                Debug.LogError("[GameScene] NetworkManager not started! Go back to MainMenu!");
            }
        }
    }
}
