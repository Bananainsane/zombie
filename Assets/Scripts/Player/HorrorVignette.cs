using Unity.Netcode;
using UnityEngine;

namespace SneakyGame.Player
{
    /// <summary>
    /// Creates horror vignette effect that darkens edges of screen when in danger
    /// Simulates limited vision/tunnel vision when scared
    /// </summary>
    public class HorrorVignette : NetworkBehaviour
    {
        [Header("Vignette Settings")]
        [SerializeField] private float normalVignetteStrength = 0.2f;
        [SerializeField] private float dangerVignetteStrength = 0.7f;
        [SerializeField] private float transitionSpeed = 2f;

        private PlayerMovement playerMovement;
        private Material vignetteMaterial;
        private float currentVignetteStrength;

        private void Awake()
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner) return;

            // Create vignette material
            Shader vignetteShader = Shader.Find("Hidden/Internal-Colored");
            if (vignetteShader != null)
            {
                vignetteMaterial = new Material(vignetteShader);
            }

            currentVignetteStrength = normalVignetteStrength;
        }

        private void Update()
        {
            if (!IsOwner) return;

            // Check if player is in danger
            float stamina = playerMovement != null ? playerMovement.GetStamina() : 100f;
            float maxStamina = playerMovement != null ? playerMovement.GetMaxStamina() : 100f;
            float staminaPercent = stamina / maxStamina;

            // Check for nearby zombies
            var zombies = FindObjectsOfType<AI.ZombieAI>();
            float closestZombieDistance = float.MaxValue;

            foreach (var zombie in zombies)
            {
                float distance = Vector3.Distance(transform.position, zombie.transform.position);
                if (distance < closestZombieDistance)
                {
                    closestZombieDistance = distance;
                }
            }

            // Determine danger level
            bool inDanger = staminaPercent < 0.3f || closestZombieDistance < 12f;

            float targetVignette = normalVignetteStrength;

            if (inDanger)
            {
                float dangerLevel = 1f - (closestZombieDistance / 12f);
                if (staminaPercent < 0.3f)
                {
                    dangerLevel = Mathf.Max(dangerLevel, 1f - staminaPercent);
                }

                targetVignette = Mathf.Lerp(normalVignetteStrength, dangerVignetteStrength, dangerLevel);
            }

            // Smooth transition
            currentVignetteStrength = Mathf.Lerp(currentVignetteStrength, targetVignette, Time.deltaTime * transitionSpeed);
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!IsOwner || vignetteMaterial == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            // Simple vignette using Graphics.DrawTexture overlay
            RenderTexture.active = destination;
            GL.Clear(true, true, Color.black);

            // Draw source
            Graphics.Blit(source, destination);

            // Draw vignette overlay
            GL.PushMatrix();
            GL.LoadPixelMatrix();

            vignetteMaterial.SetPass(0);

            // Draw dark overlay from edges
            GL.Begin(GL.QUADS);

            Color vignetteColor = new Color(0, 0, 0, currentVignetteStrength);
            float width = Screen.width;
            float height = Screen.height;
            float edgeDistance = Mathf.Min(width, height) * 0.3f;

            // Top edge
            GL.Color(vignetteColor);
            GL.Vertex3(0, height, 0);
            GL.Vertex3(width, height, 0);
            GL.Color(new Color(0, 0, 0, 0));
            GL.Vertex3(width, height - edgeDistance, 0);
            GL.Vertex3(0, height - edgeDistance, 0);

            // Bottom edge
            GL.Color(vignetteColor);
            GL.Vertex3(0, 0, 0);
            GL.Color(new Color(0, 0, 0, 0));
            GL.Vertex3(0, edgeDistance, 0);
            GL.Vertex3(width, edgeDistance, 0);
            GL.Color(vignetteColor);
            GL.Vertex3(width, 0, 0);

            // Left edge
            GL.Color(vignetteColor);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(0, height, 0);
            GL.Color(new Color(0, 0, 0, 0));
            GL.Vertex3(edgeDistance, height, 0);
            GL.Vertex3(edgeDistance, 0, 0);

            // Right edge
            GL.Color(vignetteColor);
            GL.Vertex3(width, 0, 0);
            GL.Color(new Color(0, 0, 0, 0));
            GL.Vertex3(width - edgeDistance, 0, 0);
            GL.Vertex3(width - edgeDistance, height, 0);
            GL.Color(vignetteColor);
            GL.Vertex3(width, height, 0);

            GL.End();
            GL.PopMatrix();

            RenderTexture.active = null;
        }
    }
}
