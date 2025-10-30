using UnityEngine;

namespace SneakyGame.Game
{
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Weapons/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        public string weaponName;
        public float damage;
        public float fireRate;
        public int magazineSize;
        public int reserveAmmo;
        public float reloadTime;
        public float range;
        public int pelletCount = 1;
        public float spread = 0f;
        public Color weaponColor = Color.white;
    }
}
