using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SneakyGame.AI
{
    public class MinimalFlocking : NetworkBehaviour
    {
        public float separationRadius = 2f;
        public float alignmentRadius = 5f;
        public float cohesionRadius = 5f;
        public float maxSpeed = 3f;
        public float maxForce = 2f;

        private static List<MinimalFlocking> all = new List<MinimalFlocking>();
        private Vector3 velocity;
        private Rigidbody rb;

        void Awake() => rb = GetComponent<Rigidbody>();

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                all.Add(this);
                velocity = Random.insideUnitSphere * maxSpeed;
                velocity.y = 0;
            }
        }

        public override void OnNetworkDespawn() { if (IsServer) all.Remove(this); }

        void FixedUpdate()
        {
            if (!IsServer) return;

            Vector3 force = Separation() + Alignment() + Cohesion();
            velocity += Vector3.ClampMagnitude(force, maxForce) * Time.fixedDeltaTime;
            velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

            rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
            if (velocity.magnitude > 0.1f)
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(velocity), Time.fixedDeltaTime * 5f));
        }

        Vector3 Separation()
        {
            Vector3 steer = Vector3.zero;
            int count = 0;
            foreach (var other in all)
            {
                if (other == this) continue;
                float d = Vector3.Distance(transform.position, other.transform.position);
                if (d < separationRadius && d > 0)
                {
                    steer += (transform.position - other.transform.position).normalized / d;
                    count++;
                }
            }
            if (count > 0) steer = Vector3.ClampMagnitude((steer / count).normalized * maxSpeed - velocity, maxForce);
            return steer;
        }

        Vector3 Alignment()
        {
            Vector3 avg = Vector3.zero;
            int count = 0;
            foreach (var other in all)
            {
                if (other == this) continue;
                if (Vector3.Distance(transform.position, other.transform.position) < alignmentRadius)
                {
                    avg += other.velocity;
                    count++;
                }
            }
            if (count > 0) return Vector3.ClampMagnitude(avg.normalized * maxSpeed - velocity, maxForce);
            return Vector3.zero;
        }

        Vector3 Cohesion()
        {
            Vector3 center = Vector3.zero;
            int count = 0;
            foreach (var other in all)
            {
                if (other == this) continue;
                if (Vector3.Distance(transform.position, other.transform.position) < cohesionRadius)
                {
                    center += other.transform.position;
                    count++;
                }
            }
            if (count > 0) return Vector3.ClampMagnitude((center / count - transform.position).normalized * maxSpeed - velocity, maxForce);
            return Vector3.zero;
        }

        public void SetVelocity(Vector3 v) { if (IsServer) velocity = v; }
        public Vector3 GetVelocity() => velocity;
    }
}
