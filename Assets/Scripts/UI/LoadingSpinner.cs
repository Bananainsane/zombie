using UnityEngine;
using UnityEngine.UI;

namespace SneakyGame.UI
{
    /// <summary>
    /// Simple loading spinner animation for connection status
    /// Rotates continuously when active
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class LoadingSpinner : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 360f; // Degrees per second
        [SerializeField] private bool rotateClockwise = false;

        [Header("Optional Pulsing Effect")]
        [SerializeField] private bool enablePulse = true;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseMin = 0.8f;
        [SerializeField] private float pulseMax = 1.2f;

        private RectTransform rectTransform;
        private Image image;
        private float pulseTime;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            image = GetComponent<Image>();
        }

        private void Update()
        {
            // Rotate
            float rotation = rotationSpeed * Time.deltaTime;
            if (rotateClockwise)
                rotation = -rotation;

            rectTransform.Rotate(0, 0, rotation);

            // Pulse
            if (enablePulse && image != null)
            {
                pulseTime += Time.deltaTime * pulseSpeed;
                float scale = Mathf.Lerp(pulseMin, pulseMax, (Mathf.Sin(pulseTime) + 1f) / 2f);
                rectTransform.localScale = Vector3.one * scale;
            }
        }

        private void OnEnable()
        {
            pulseTime = 0f;
            if (rectTransform != null)
                rectTransform.localScale = Vector3.one;
        }
    }
}
