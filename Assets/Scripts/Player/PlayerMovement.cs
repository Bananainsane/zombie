using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
namespace SneakyGame.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(NetworkObject))]
    public class PlayerMovement : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float sprintSpeed = 5f;
        [SerializeField] private float crouchSpeed = 1.5f;
        [SerializeField] private float infectedSpeed = 9f;
        [SerializeField] private float rotationSpeed = 10f;
        [Header("Stamina Settings")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float sprintStaminaCost = 20f;
        [SerializeField] private float staminaRegenRate = 15f;
        [Header("Noise Settings")]
        [SerializeField] private float walkNoiseLevel = 0.3f;
        [SerializeField] private float sprintNoiseLevel = 1f;
        [SerializeField] private float crouchNoiseLevel = 0.1f;
        [Header("References")]
        [SerializeField] private Transform cameraTransform;
        private bool isInfected;
        private NetworkVariable<float> stamina = new NetworkVariable<float>();
        private NetworkVariable<float> currentNoiseLevel = new NetworkVariable<float>();
        private CharacterController characterController;
        private Vector2 moveInput;
        private bool isCrouching;
        private bool isSprinting;
        private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>();
        private void Awake() => characterController = GetComponent<CharacterController>();
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Position player at spawn point (server only)
            if (IsServer)
            {
                stamina.Value = maxStamina;
                PositionAtSpawnPoint();
            }

            if (cameraTransform == null && IsOwner) cameraTransform = Camera.main?.transform;
            if (!IsOwner)
            {
                networkPosition.OnValueChanged += OnPositionChanged;
                networkRotation.OnValueChanged += OnRotationChanged;
            }
        }

        private void PositionAtSpawnPoint()
        {
            // Find all spawn points in scene
            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");

            if (spawnPoints.Length == 0)
            {
                Debug.LogWarning("[PlayerMovement] No spawn points found! Spawning at elevated position");
                SetSpawnPosition(new Vector3(0, 2, 0));
                return;
            }

            // Pick a random spawn point
            int randomIndex = UnityEngine.Random.Range(0, spawnPoints.Length);
            Vector3 spawnPos = spawnPoints[randomIndex].transform.position;

            // Elevate spawn position above ground
            spawnPos.y = 2f; // Spawn 2 units above ground, let physics settle

            SetSpawnPosition(spawnPos);

            Debug.Log($"[PlayerMovement] Positioned player at spawn point {randomIndex}: {spawnPos}");
        }

        private void SetSpawnPosition(Vector3 position)
        {
            // CRITICAL: Disable CharacterController when setting position
            // Otherwise it can cause physics glitches
            characterController.enabled = false;
            transform.position = position;
            characterController.enabled = true;

            Debug.Log($"[PlayerMovement] Player spawned at {position}");
            Debug.Log($"[PlayerMovement] CharacterController: height={characterController.height}, center={characterController.center}, radius={characterController.radius}");
            Debug.Log($"[PlayerMovement] Ground should be at y=0, player bottom should be around y=0 after falling");
        }
        public override void OnNetworkDespawn()
        {
            if (!IsOwner)
            {
                networkPosition.OnValueChanged -= OnPositionChanged;
                networkRotation.OnValueChanged -= OnRotationChanged;
            }
            base.OnNetworkDespawn();
        }
        private void Update()
        {
            if (!IsOwner) return;
            moveInput = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) moveInput.y += 1;
            if (Keyboard.current.sKey.isPressed) moveInput.y -= 1;
            if (Keyboard.current.aKey.isPressed) moveInput.x -= 1;
            if (Keyboard.current.dKey.isPressed) moveInput.x += 1;
            bool wantsToSprint = Keyboard.current.leftShiftKey.isPressed && moveInput.magnitude > 0.1f;
            isCrouching = Keyboard.current.leftCtrlKey.isPressed;
            if (wantsToSprint && stamina.Value > 5f)
            {
                isSprinting = true;
                UpdateStaminaServerRpc(-sprintStaminaCost * Time.deltaTime);
            }
            else
            {
                isSprinting = false;
                if (stamina.Value < maxStamina) UpdateStaminaServerRpc(staminaRegenRate * Time.deltaTime);
            }
            HandleMovement();
            SyncTransformServerRpc(transform.position, transform.rotation);
        }
        private void HandleMovement()
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();
            Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
            float currentSpeed = isInfected ? infectedSpeed : moveSpeed;
            float noiseLevel = walkNoiseLevel;
            if (isCrouching)
            {
                currentSpeed = crouchSpeed;
                noiseLevel = crouchNoiseLevel;
            }
            else if (isSprinting && !isInfected)
            {
                currentSpeed = sprintSpeed;
                noiseLevel = sprintNoiseLevel;
            }
            UpdateNoiseLevelServerRpc(moveInput.magnitude > 0.1f ? noiseLevel : 0f);
            Vector3 movement = moveDirection * currentSpeed * Time.deltaTime;
            movement.y = -9.81f * Time.deltaTime;
            characterController.Move(movement);
        }
        [ServerRpc]
        private void UpdateStaminaServerRpc(float change) => stamina.Value = Mathf.Clamp(stamina.Value + change, 0f, maxStamina);
        [ServerRpc]
        private void UpdateNoiseLevelServerRpc(float noise) => currentNoiseLevel.Value = noise;
        [ServerRpc]
        private void SyncTransformServerRpc(Vector3 position, Quaternion rotation)
        {
            networkPosition.Value = position;
            networkRotation.Value = rotation;
        }
        private void OnPositionChanged(Vector3 oldValue, Vector3 newValue) => transform.position = newValue;
        private void OnRotationChanged(Quaternion oldValue, Quaternion newValue) => transform.rotation = newValue;
        public Vector3 GetPosition() => transform.position;
        public float GetNoiseLevel() => currentNoiseLevel.Value;
        public float GetStamina() => stamina.Value;
        public float GetMaxStamina() => maxStamina;
        public void SetInfectedSpeed(bool infected) => isInfected = infected;
    }
}
