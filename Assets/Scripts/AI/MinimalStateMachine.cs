using Unity.Netcode;
using UnityEngine;

namespace SneakyGame.AI
{
    [RequireComponent(typeof(MinimalFlocking))]
    public class MinimalStateMachine : NetworkBehaviour
    {
        public enum State { Roaming, Chase }

        public float detectionRadius = 10f;
        public float loseRadius = 15f;
        public Color roamingColor = Color.green;
        public Color chaseColor = Color.red;

        private NetworkVariable<State> state = new NetworkVariable<State>(State.Roaming);
        private MinimalFlocking flocking;
        private Transform target;
        private Material mat;

        void Awake() => flocking = GetComponent<MinimalFlocking>();

        public override void OnNetworkSpawn()
        {
            mat = GetComponent<Renderer>()?.material;
            state.OnValueChanged += (o, n) => UpdateColor(n);
            UpdateColor(state.Value);
        }

        public override void OnNetworkDespawn() => state.OnValueChanged -= (o, n) => UpdateColor(n);

        void Update()
        {
            if (!IsServer) return;
            if (state.Value == State.Roaming) CheckForPlayers();
            else ChasePlayer();
        }

        void CheckForPlayers()
        {
            Transform nearest = null;
            float nearestDist = float.MaxValue;
            foreach (var p in GameObject.FindGameObjectsWithTag("Player"))
            {
                float d = Vector3.Distance(transform.position, p.transform.position);
                if (d < detectionRadius && d < nearestDist) { nearest = p.transform; nearestDist = d; }
            }
            if (nearest != null) { target = nearest; state.Value = State.Chase; }
        }

        void ChasePlayer()
        {
            if (target == null) { state.Value = State.Roaming; return; }
            float d = Vector3.Distance(transform.position, target.position);
            if (d > loseRadius) { target = null; state.Value = State.Roaming; return; }
            flocking.SetVelocity((target.position - transform.position).normalized * flocking.maxSpeed * 1.5f);
        }

        void UpdateColor(State s) { if (mat != null) mat.color = s == State.Roaming ? roamingColor : chaseColor; }
    }
}
