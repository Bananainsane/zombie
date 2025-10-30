using Unity.Netcode;
using UnityEngine;

namespace SneakyGame.Game
{
    public class MysteryBox : NetworkBehaviour
    {
        [SerializeField] private WeaponData[] availableWeapons;
        [SerializeField] private int cost = 950;
        [SerializeField] private float activationRadius = 3f;
        [SerializeField] private Material glowMaterial;

        private Renderer boxRenderer;

        private void Awake()
        {
            boxRenderer = GetComponent<Renderer>();
            if (boxRenderer && glowMaterial) boxRenderer.material = glowMaterial;
        }

        private void Update()
        {
            if (!IsServer) return;

            var players = FindObjectsOfType<PointsSystem>();
            foreach (var player in players)
            {
                if (Vector3.Distance(transform.position, player.transform.position) < activationRadius)
                {
                    if (Input.GetKeyDown(KeyCode.F) && player.HasEnoughPoints(cost))
                    {
                        SpinBox(player);
                    }
                }
            }
        }

        private void SpinBox(PointsSystem player)
        {
            player.SpendPointsServerRpc(cost);
            WeaponData randomWeapon = availableWeapons[Random.Range(0, availableWeapons.Length)];

            var weaponController = player.GetComponentInChildren<Player.WeaponController>();
            if (weaponController)
            {
                weaponController.EquipWeapon(randomWeapon);
            }

            ShowWeaponClientRpc(randomWeapon.weaponName);
        }

        [ClientRpc]
        private void ShowWeaponClientRpc(string weaponName)
        {
            Debug.Log($"<color=yellow>MYSTERY BOX: Got {weaponName}!</color>");
        }
    }
}
