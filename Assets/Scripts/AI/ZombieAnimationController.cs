using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace SneakyGame.AI
{
    /// <summary>
    /// Controls zombie animations based on NavMeshAgent movement speed
    /// Syncs walking, running, attacking animations with AI behavior
    /// </summary>
    public class ZombieAnimationController : NetworkBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float walkSpeedThreshold = 0.1f;
        [SerializeField] private float runSpeedThreshold = 3f;

        private Animator animator;
        private NavMeshAgent agent;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            agent = GetComponent<NavMeshAgent>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (animator == null)
            {
                Debug.LogWarning($"{name}: No Animator found for zombie animations");
                enabled = false;
                return;
            }

            if (agent == null)
            {
                Debug.LogWarning($"{name}: No NavMeshAgent found");
                enabled = false;
                return;
            }

            // Log animator parameters for debugging
            Debug.Log($"{name}: Zombie animation controller initialized with {animator.runtimeAnimatorController?.name}");
            if (animator.parameters.Length > 0)
            {
                Debug.Log($"{name}: Available animator parameters:");
                foreach (var param in animator.parameters)
                {
                    Debug.Log($"  - {param.name} ({param.type})");
                }
            }
            else
            {
                Debug.LogWarning($"{name}: Animator has NO parameters! Animations might not transition.");
            }

            // Ensure animator is enabled
            animator.enabled = true;
        }

        private void Update()
        {
            if (!IsServer || animator == null || agent == null || !agent.enabled)
                return;

            float speed = agent.velocity.magnitude;

            if (animator.parameters.Length > 0)
            {
                SetAnimatorFloat("Speed", speed);
                SetAnimatorFloat("speed", speed);
                SetAnimatorFloat("velocity", speed);
                SetAnimatorFloat("Velocity", speed);

                SetAnimatorBool("isWalking", speed > walkSpeedThreshold);
                SetAnimatorBool("isRunning", speed > runSpeedThreshold);
                SetAnimatorBool("IsWalking", speed > walkSpeedThreshold);
                SetAnimatorBool("IsRunning", speed > runSpeedThreshold);
                SetAnimatorBool("walk", speed > walkSpeedThreshold);
                SetAnimatorBool("run", speed > runSpeedThreshold);
            }
            else
            {
                if (speed > runSpeedThreshold)
                {
                    animator.Play("run", 0);
                }
                else if (speed > walkSpeedThreshold)
                {
                    animator.Play("walk", 0);
                }
                else
                {
                    animator.Play("idle1", 0);
                }
            }
        }

        private void SetAnimatorFloat(string paramName, float value)
        {
            if (HasParameter(paramName, AnimatorControllerParameterType.Float))
            {
                animator.SetFloat(paramName, value);
            }
        }

        private void SetAnimatorBool(string paramName, bool value)
        {
            if (HasParameter(paramName, AnimatorControllerParameterType.Bool))
            {
                animator.SetBool(paramName, value);
            }
        }

        private bool HasParameter(string paramName, AnimatorControllerParameterType type)
        {
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == paramName && param.type == type)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Play attack animation (call from ZombieAI when attacking)
        /// </summary>
        public void PlayAttack()
        {
            if (animator == null) return;

            if (HasParameter("Attack", AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger("Attack");
            }
            else if (HasParameter("attack", AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger("attack");
            }
            else
            {
                int randomAttack = Random.Range(1, 5);
                string attackAnim = $"atack{randomAttack}";
                animator.Play(attackAnim, 0);
            }
        }

        /// <summary>
        /// Play roar animation (call when zombie spots player)
        /// </summary>
        public void PlayRoar()
        {
            if (animator == null) return;

            if (HasParameter("Roar", AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger("Roar");
            }
            else if (HasParameter("roar", AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger("roar");
            }
        }
    }
}
