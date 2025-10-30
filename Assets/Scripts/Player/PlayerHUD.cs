using Unity.Netcode;
using UnityEngine;

namespace SneakyGame.Player
{
    /// <summary>
    /// COD BO2 style HUD for health and ammo display
    /// </summary>
    public class PlayerHUD : NetworkBehaviour
    {
        [Header("HUD Settings")]
        [SerializeField] private bool showHUD = true;
        [SerializeField] private Vector2 hudOffset = new Vector2(20, 20);

        private Game.PlayerState playerState;
        private WeaponController weapon;
        private Game.PointsSystem pointsSystem;

        private GUIStyle healthStyle;
        private GUIStyle ammoStyle;
        private GUIStyle crosshairStyle;
        private GUIStyle roundStyle;
        private GUIStyle pointsStyle;

        private void Awake()
        {
            playerState = GetComponent<Game.PlayerState>();
            weapon = GetComponentInChildren<WeaponController>();
            pointsSystem = GetComponent<Game.PointsSystem>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            // Initialize GUI styles
            InitializeStyles();
        }

        private void InitializeStyles()
        {
            // Health style
            healthStyle = new GUIStyle();
            healthStyle.fontSize = 24;
            healthStyle.fontStyle = FontStyle.Bold;
            healthStyle.normal.textColor = Color.white;

            // Ammo style
            ammoStyle = new GUIStyle();
            ammoStyle.fontSize = 32;
            ammoStyle.fontStyle = FontStyle.Bold;
            ammoStyle.normal.textColor = Color.white;
            ammoStyle.alignment = TextAnchor.MiddleRight;

            // Crosshair style
            crosshairStyle = new GUIStyle();
            crosshairStyle.fontSize = 20;
            crosshairStyle.normal.textColor = Color.white;
            crosshairStyle.alignment = TextAnchor.MiddleCenter;

            // Round style
            roundStyle = new GUIStyle();
            roundStyle.fontSize = 32;
            roundStyle.fontStyle = FontStyle.Bold;
            roundStyle.normal.textColor = Color.yellow;
            roundStyle.alignment = TextAnchor.UpperCenter;

            // Points style
            pointsStyle = new GUIStyle();
            pointsStyle.fontSize = 28;
            pointsStyle.fontStyle = FontStyle.Bold;
            pointsStyle.normal.textColor = Color.green;
            pointsStyle.alignment = TextAnchor.UpperRight;
        }

        private void OnGUI()
        {
            if (!IsOwner || !showHUD) return;

            if (healthStyle == null || ammoStyle == null)
            {
                InitializeStyles();
            }

            DrawHealthBar();
            DrawAmmoCounter();
            DrawCrosshair();
            DrawReloadIndicator();
            DrawRound();
            DrawPoints();
        }

        private void DrawHealthBar()
        {
            if (playerState == null) return;

            float health = playerState.Health.Value;
            float maxHealth = playerState.GetMaxHealth();
            float healthPercent = health / maxHealth;

            // Change color based on health
            Color healthColor = Color.Lerp(Color.red, Color.green, healthPercent);
            healthStyle.normal.textColor = healthColor;

            // Draw health text in bottom left
            Rect healthRect = new Rect(hudOffset.x, Screen.height - hudOffset.y - 40, 200, 40);
            GUI.Label(healthRect, $"HEALTH: {Mathf.CeilToInt(health)}", healthStyle);

            // Draw health bar
            Rect healthBarBg = new Rect(hudOffset.x, Screen.height - hudOffset.y - 10, 200, 10);
            Rect healthBarFill = new Rect(hudOffset.x, Screen.height - hudOffset.y - 10, 200 * healthPercent, 10);

            GUI.DrawTexture(healthBarBg, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, Color.gray, 0, 0);
            GUI.DrawTexture(healthBarFill, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, healthColor, 0, 0);
        }

        private void DrawAmmoCounter()
        {
            if (weapon == null) return;

            int currentAmmo = weapon.GetCurrentAmmo();
            int reserveAmmo = weapon.GetReserveAmmo();

            // Change color if low on ammo
            if (currentAmmo <= 10)
            {
                ammoStyle.normal.textColor = Color.red;
            }
            else if (currentAmmo <= 20)
            {
                ammoStyle.normal.textColor = Color.yellow;
            }
            else
            {
                ammoStyle.normal.textColor = Color.white;
            }

            // Draw ammo in bottom right
            Rect ammoRect = new Rect(Screen.width - 220, Screen.height - hudOffset.y - 60, 200, 60);
            GUI.Label(ammoRect, $"{currentAmmo} / {reserveAmmo}", ammoStyle);

            // Draw magazine icon
            GUIStyle magStyle = new GUIStyle();
            magStyle.fontSize = 14;
            magStyle.normal.textColor = Color.gray;
            magStyle.alignment = TextAnchor.MiddleRight;

            Rect magRect = new Rect(Screen.width - 220, Screen.height - hudOffset.y - 20, 200, 20);
            GUI.Label(magRect, "AMMO", magStyle);
        }

        private void DrawCrosshair()
        {
            // Simple crosshair in center of screen
            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;
            float crosshairSize = 15f;

            // Draw crosshair lines
            DrawLine(new Vector2(centerX - crosshairSize, centerY), new Vector2(centerX + crosshairSize, centerY), Color.white, 2);
            DrawLine(new Vector2(centerX, centerY - crosshairSize), new Vector2(centerX, centerY + crosshairSize), Color.white, 2);

            // Center dot
            Rect dotRect = new Rect(centerX - 2, centerY - 2, 4, 4);
            GUI.DrawTexture(dotRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, Color.white, 0, 0);
        }

        private void DrawReloadIndicator()
        {
            if (weapon == null || !weapon.IsReloading()) return;

            // Draw "RELOADING..." text in center
            GUIStyle reloadStyle = new GUIStyle();
            reloadStyle.fontSize = 28;
            reloadStyle.fontStyle = FontStyle.Bold;
            reloadStyle.normal.textColor = Color.yellow;
            reloadStyle.alignment = TextAnchor.MiddleCenter;

            Rect reloadRect = new Rect(Screen.width / 2f - 150, Screen.height / 2f + 50, 300, 40);
            GUI.Label(reloadRect, "RELOADING...", reloadStyle);
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 dir = (end - start).normalized;
            float distance = Vector2.Distance(start, end);

            Rect lineRect = new Rect(start.x, start.y - thickness / 2, distance, thickness);

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(lineRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, color, 0, 0);
            GUIUtility.RotateAroundPivot(-angle, start);
        }

        private void DrawRound()
        {
            if (Game.RoundManager.Instance == null) return;
            int round = Game.RoundManager.Instance.CurrentRound.Value;
            Rect roundRect = new Rect(Screen.width / 2f - 150, 20, 300, 50);
            GUI.Label(roundRect, $"ROUND {round}", roundStyle);
        }

        private void DrawPoints()
        {
            if (pointsSystem == null) return;
            int points = pointsSystem.Points.Value;
            Rect pointsRect = new Rect(Screen.width - 250, 20, 230, 50);
            GUI.Label(pointsRect, $"{points}", pointsStyle);
        }
    }
}
