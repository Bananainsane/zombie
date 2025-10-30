using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
namespace SneakyGame.Player
{
    public class WeaponController : NetworkBehaviour
    {
        [Header("Weapon Stats")]
        [SerializeField] private Game.WeaponData currentWeaponData;
        private float damage = 25f;
        private float fireRate = 0.1f;
        private float range = 100f;
        private int maxAmmo = 30;
        private int reserveAmmo = 120;
        private float reloadTime = 2f;
        private int pelletCount = 1;
        private float spread = 0f;
        [Header("Recoil")]
        [SerializeField] private float recoilAmount = 0.5f;
        [SerializeField] private float recoilRecovery = 5f;
        [Header("Effects")]
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private GameObject bulletHolePrefab;
        [SerializeField] private Transform firePoint;
        private NetworkVariable<int> currentAmmo = new NetworkVariable<int>();
        private NetworkVariable<int> currentReserve = new NetworkVariable<int>();
        private float nextFireTime = 0f;
        private bool isReloading = false;
        private Camera playerCamera;
        private void Awake()
        {
            if (firePoint == null)
            {
                GameObject firePointObj = new GameObject("FirePoint");
                firePointObj.transform.SetParent(transform);
                firePointObj.transform.localPosition = new Vector3(0, 0, 0.5f);
                firePoint = firePointObj.transform;
            }
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (currentWeaponData != null) LoadWeaponStats(currentWeaponData);
            if (IsServer)
            {
                currentAmmo.Value = maxAmmo;
                currentReserve.Value = reserveAmmo;
            }
            if (IsOwner) playerCamera = Camera.main;
        }
        public void EquipWeapon(Game.WeaponData weaponData)
        {
            if (!IsServer) return;
            LoadWeaponStats(weaponData);
            currentAmmo.Value = maxAmmo;
            currentReserve.Value = reserveAmmo;
        }
        private void LoadWeaponStats(Game.WeaponData weaponData)
        {
            currentWeaponData = weaponData;
            damage = weaponData.damage;
            fireRate = weaponData.fireRate;
            range = weaponData.range;
            maxAmmo = weaponData.magazineSize;
            reserveAmmo = weaponData.reserveAmmo;
            reloadTime = weaponData.reloadTime;
            pelletCount = weaponData.pelletCount;
            spread = weaponData.spread;
        }
        private void Update()
        {
            if (!IsOwner) return;
            if (Mouse.current != null && Mouse.current.leftButton.isPressed && !isReloading && Time.time >= nextFireTime)
            {
                if (currentAmmo.Value > 0)
                {
                    Shoot();
                    nextFireTime = Time.time + fireRate;
                }
                else PlayEmptyClickClientRpc();
            }
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && !isReloading && currentAmmo.Value < maxAmmo && currentReserve.Value > 0) StartReload();
        }
        private void Shoot() { ShootServerRpc(); }
        [ServerRpc]
        private void ShootServerRpc()
        {
            if (currentAmmo.Value <= 0 || isReloading) return;
            currentAmmo.Value--;
            Transform shooterTransform = transform.parent != null ? transform.parent : transform;
            Camera cam = Camera.main;
            for (int i = 0; i < pelletCount; i++)
            {
                Vector3 direction;
                Vector3 origin;
                if (cam != null)
                {
                    origin = cam.transform.position;
                    direction = cam.transform.forward;
                }
                else
                {
                    origin = shooterTransform.position + Vector3.up * 1.5f;
                    direction = shooterTransform.forward;
                }
                if (spread > 0f)
                {
                    direction += new Vector3(Random.Range(-spread, spread), Random.Range(-spread, spread), Random.Range(-spread, spread));
                    direction.Normalize();
                }
                Ray ray = new Ray(origin, direction);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, range))
                {
                    GameObject hitObject = hit.collider.gameObject;
                    AI.ZombieHealth zombieHealth = hitObject.GetComponentInParent<AI.ZombieHealth>();
                    if (zombieHealth == null) zombieHealth = hitObject.GetComponent<AI.ZombieHealth>();
                    if (zombieHealth != null)
                    {
                        bool isHeadshot = hit.point.y > (hitObject.transform.position.y + 1.5f);
                        float oldHealth = zombieHealth.GetHealth();
                        zombieHealth.TakeDamage(damage, hit.point);
                        if (zombieHealth.GetHealth() <= 0 && oldHealth > 0)
                        {
                            var pointsSystem = GetComponentInParent<Game.PointsSystem>();
                            if (pointsSystem != null) pointsSystem.AddKillPointsServerRpc(isHeadshot);
                        }
                    }
                    SpawnBulletHoleClientRpc(hit.point, hit.normal);
                }
            }
            PlayShootEffectsClientRpc();
        }
        [ClientRpc]
        private void PlayShootEffectsClientRpc()
        {
            if (muzzleFlashPrefab != null && firePoint != null)
            {
                GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
                Destroy(flash, 0.1f);
            }
            if (IsOwner && playerCamera != null) playerCamera.transform.localRotation *= Quaternion.Euler(-recoilAmount, Random.Range(-recoilAmount * 0.5f, recoilAmount * 0.5f), 0);
            PlayGunshotSound();
        }
        private void PlayGunshotSound()
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f;
                audioSource.volume = 0.5f;
            }
            AudioClip gunshotClip = Resources.Load<AudioClip>("Audio/Weapons/gunshot");
            if (gunshotClip == null) gunshotClip = Game.ProceduralAudioGenerator.CreateGunshot();
            audioSource.PlayOneShot(gunshotClip);
        }
        [ClientRpc]
        private void SpawnBulletHoleClientRpc(Vector3 position, Vector3 normal)
        {
            if (bulletHolePrefab != null)
            {
                Quaternion rotation = Quaternion.LookRotation(normal);
                GameObject hole = Instantiate(bulletHolePrefab, position, rotation);
                Destroy(hole, 10f);
            }
        }
        [ClientRpc]
        private void PlayEmptyClickClientRpc()
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                AudioClip clickClip = Resources.Load<AudioClip>("Audio/Weapons/empty_click");
                if (clickClip != null) audioSource.PlayOneShot(clickClip, 0.3f);
            }
        }
        private void StartReload() { ReloadServerRpc(); }
        [ServerRpc]
        private void ReloadServerRpc()
        {
            if (isReloading || currentReserve.Value <= 0 || currentAmmo.Value >= maxAmmo) return;
            isReloading = true;
            ReloadClientRpc();
            Invoke(nameof(FinishReload), reloadTime);
        }
        [ClientRpc]
        private void ReloadClientRpc()
        {
            Debug.Log("Reloading...");
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                AudioClip reloadClip = Resources.Load<AudioClip>("Audio/Weapons/reload");
                if (reloadClip != null) audioSource.PlayOneShot(reloadClip, 0.4f);
            }
        }
        private void FinishReload()
        {
            if (!IsServer) return;
            int ammoNeeded = maxAmmo - currentAmmo.Value;
            int ammoToReload = Mathf.Min(ammoNeeded, currentReserve.Value);
            currentAmmo.Value += ammoToReload;
            currentReserve.Value -= ammoToReload;
            isReloading = false;
            Debug.Log($"Reload complete: {currentAmmo.Value}/{maxAmmo} (Reserve: {currentReserve.Value})");
        }
        public int GetCurrentAmmo() => currentAmmo.Value;
        public int GetReserveAmmo() => currentReserve.Value;
        public int GetMaxAmmo() => maxAmmo;
        public bool IsReloading() => isReloading;

        /// <summary>
        /// Add reserve ammo (called by ammo powerup)
        /// </summary>
        public void AddReserveAmmo(int amount)
        {
            if (!IsServer) return;

            currentReserve.Value += amount;
            Debug.Log($"[WeaponController] Added {amount} reserve ammo. New total: {currentReserve.Value}");
        }
    }
}
