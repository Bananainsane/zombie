using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SneakyGame.Player
{
    /// <summary>
    /// First-person camera controller
    /// Provides immersive horror game perspective
    /// </summary>
    public class PlayerCamera : NetworkBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private float cameraHeight = 1.6f;
        [SerializeField] private float sensitivity = 2f;

        [Header("Rotation Limits")]
        [SerializeField] private float minVerticalAngle = -80f;
        [SerializeField] private float maxVerticalAngle = 80f;

        [Header("Head Bob")]
        [SerializeField] private float bobFrequency = 5f;
        [SerializeField] private float bobAmplitude = 0.05f;
        [SerializeField] private float sprintBobMultiplier = 1.5f;
        private float bobTimer = 0f;

        private Transform playerTransform;
        private Camera playerCamera;
        private Vector2 lookInput;
        private float currentYaw;
        private float currentPitch;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Only setup camera for the local player
            if (IsOwner)
            {
                SetupCamera();
            }
        }

        private void SetupCamera()
        {
            playerTransform = transform;

            // Get main camera if it exists
            Camera mainCam = Camera.main;

            if (mainCam != null)
            {
                Debug.Log("[PlayerCamera] Found Main Camera - taking control");
                playerCamera = mainCam;

                // Remove old AudioListener to avoid conflicts
                AudioListener oldListener = playerCamera.GetComponent<AudioListener>();
                if (oldListener != null)
                {
                    Destroy(oldListener);
                }
            }
            else
            {
                Debug.Log("[PlayerCamera] No camera found - creating new one");
                GameObject cameraObj = new GameObject("PlayerCamera");
                cameraObj.tag = "MainCamera";
                playerCamera = cameraObj.AddComponent<Camera>();
            }

            // Add AudioListener to this player's camera
            if (playerCamera.GetComponent<AudioListener>() == null)
            {
                playerCamera.gameObject.AddComponent<AudioListener>();
            }

            // Position camera at eye level and parent to player
            playerCamera.transform.SetParent(playerTransform);
            playerCamera.transform.localPosition = Vector3.up * cameraHeight;
            playerCamera.transform.localRotation = Quaternion.identity;

            // Initialize angles
            currentYaw = playerTransform.eulerAngles.y;
            currentPitch = 0f;

            // Hide player body from first person view
            HidePlayerBodyFromCamera();

            // Ensure cursor is locked
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Debug.Log("[PlayerCamera] Camera setup complete - parented to player");
        }

        private void HidePlayerBodyFromCamera()
        {
            // Set player's visual to a layer that camera doesn't render
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }

        private void LateUpdate()
        {
            if (!IsOwner || playerCamera == null) return;

            // Read mouse input directly
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                lookInput = Mouse.current.delta.ReadValue() * 0.1f;
            }

            // Update rotation based on input
            currentYaw += lookInput.x * sensitivity;
            currentPitch -= lookInput.y * sensitivity;

            // Clamp vertical rotation
            currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);

            // Apply rotation to player body (yaw) and camera (pitch)
            playerTransform.rotation = Quaternion.Euler(0, currentYaw, 0);

            // Head bob effect
            Vector3 bobOffset = CalculateHeadBob();

            // Apply camera rotation and position
            playerCamera.transform.localPosition = Vector3.up * cameraHeight + bobOffset;
            playerCamera.transform.localRotation = Quaternion.Euler(currentPitch, 0, 0);
        }

        private Vector3 CalculateHeadBob()
        {
            // Get player movement component
            var playerMovement = playerTransform.GetComponent<PlayerMovement>();
            if (playerMovement == null) return Vector3.zero;

            // Check if player is moving
            float velocity = playerMovement.GetComponent<CharacterController>().velocity.magnitude;

            if (velocity < 0.1f)
            {
                bobTimer = 0f;
                return Vector3.zero;
            }

            // Calculate bob based on movement speed
            float bobMultiplier = 1f;
            if (playerMovement.GetStamina() > 5f && Keyboard.current.leftShiftKey.isPressed)
            {
                bobMultiplier = sprintBobMultiplier;
            }

            bobTimer += Time.deltaTime * bobFrequency * bobMultiplier;

            float bobX = Mathf.Sin(bobTimer) * bobAmplitude * bobMultiplier;
            float bobY = Mathf.Abs(Mathf.Cos(bobTimer * 2f)) * bobAmplitude * bobMultiplier;

            return new Vector3(bobX, bobY, 0);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (IsOwner && hasFocus)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        // Allow player to unlock cursor with Escape key
        private void Update()
        {
            if (!IsOwner) return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
    }
}
