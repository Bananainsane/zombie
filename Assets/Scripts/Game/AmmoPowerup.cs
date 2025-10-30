using Unity.Netcode;
using UnityEngine;

namespace SneakyGame.Game
{
    /// <summary>
    /// Ammo powerup that refills reserve ammo when collected
    /// </summary>
    public class AmmoPowerup : NetworkBehaviour
    {
        [SerializeField] private int ammoAmount = 60;
        [SerializeField] private float rotationSpeed = 100f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.3f;

        private Vector3 startPosition;

        private void Start()
        {
            startPosition = transform.position;
        }

        private void Update()
        {
            // Rotate powerup for visibility
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

            // Bob up and down
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;

            // Only players can collect powerups
            if (other.CompareTag("Player"))
            {
                CollectPowerup(other.gameObject);
            }
        }

        private void CollectPowerup(GameObject player)
        {
            // Find weapon controller on player
            var weaponController = player.GetComponentInChildren<Player.WeaponController>();

            if (weaponController != null)
            {
                weaponController.AddReserveAmmo(ammoAmount);
                Debug.Log($"Ammo powerup collected! Added {ammoAmount} reserve ammo");
            }
            else
            {
                Debug.LogWarning("Player has no WeaponController!");
            }

            // Play collection sound
            PlayCollectionSoundClientRpc();

            // Despawn powerup
            GetComponent<NetworkObject>().Despawn();
            Destroy(gameObject);
        }

        [ClientRpc]
        private void PlayCollectionSoundClientRpc()
        {
            // Simple beep sound for pickup
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.pitch = 1.2f;
            audioSource.volume = 0.6f;

            AudioClip pickupSound = ProceduralAudioGenerator.CreateAmmoPickup();
            audioSource.PlayOneShot(pickupSound);
        }
    }
}
