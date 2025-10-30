using Unity.Netcode;
using UnityEngine;

namespace SneakyGame.AI
{
    /// <summary>
    /// Fixes T-pose by rotating arms down when no animations are available
    /// </summary>
    public class ZombieArmFix : NetworkBehaviour
    {
        [Header("Arm Rotation Fix")]
        [SerializeField] private bool autoFixTPose = true;
        [SerializeField] private float armDownAngle = 30f; // How far to rotate arms down

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (autoFixTPose)
            {
                FixTPose();
            }
        }

        private void FixTPose()
        {
            // Find arm bones in the humanoid rig
            Animator animator = GetComponentInChildren<Animator>();

            if (animator == null || !animator.isHuman)
            {
                Debug.LogWarning($"{name}: No humanoid animator found for T-pose fix");
                return;
            }

            // Get arm bones
            Transform leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            Transform leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            Transform rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);

            // Rotate arms down to look more natural
            if (leftUpperArm != null)
            {
                leftUpperArm.localRotation = Quaternion.Euler(0, 0, armDownAngle);
            }

            if (rightUpperArm != null)
            {
                rightUpperArm.localRotation = Quaternion.Euler(0, 0, -armDownAngle);
            }

            // Bend elbows slightly
            if (leftLowerArm != null)
            {
                leftLowerArm.localRotation = Quaternion.Euler(0, 0, 20f);
            }

            if (rightLowerArm != null)
            {
                rightLowerArm.localRotation = Quaternion.Euler(0, 0, -20f);
            }

            Debug.Log($"{name}: T-pose fixed - arms rotated down");
        }
    }
}
