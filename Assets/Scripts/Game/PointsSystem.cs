using Unity.Netcode;
using UnityEngine;

namespace SneakyGame.Game
{
    public class PointsSystem : NetworkBehaviour
    {
        public NetworkVariable<int> Points = new NetworkVariable<int>(0);

        [SerializeField] private int killPoints = 60;
        [SerializeField] private int headshotPoints = 100;

        [ServerRpc(RequireOwnership = false)]
        public void AddKillPointsServerRpc(bool wasHeadshot)
        {
            int reward = wasHeadshot ? headshotPoints : killPoints;
            Points.Value += reward;
            Debug.Log($"<color=green>+{reward} points! Total: {Points.Value}</color>");
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpendPointsServerRpc(int amount)
        {
            if (Points.Value >= amount)
            {
                Points.Value -= amount;
                Debug.Log($"Spent {amount} points. Remaining: {Points.Value}");
            }
        }

        public bool HasEnoughPoints(int amount) => Points.Value >= amount;
    }
}
