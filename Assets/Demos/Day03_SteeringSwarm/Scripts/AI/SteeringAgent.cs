using UnityEngine;
using System.Collections.Generic;

namespace Demos.Day03_SteeringSwarm.Scripts.AI
{
    public class SteeringAgent : MonoBehaviour
    {
        [Header("Movement")]
        public float maxSpeed = 5f;
        public float maxForce = 10f; // Limit how "fast" we change direction (turning radius)

        [Header("Arrive")]
        public float slowingRadius = 3f;

        [Header("Arrive")]
        public float separationRadius = 1.5f;
        public float separationStrength = 5f; // How strongly we want to separate from nearby agents

        [Header("Weights")]
        public float arriveWeight = 1f;
        public float separationWeight = 1f;

        [Header("Debug")]
        public bool drawDebug = true;

        private Vector3 velocity = Vector3.zero; // Current velocity of the agent

        // Optional target for Seek / Arrive behaviors
        public Transform target;

        // Static list to keep track of all agents in the scene
        public static List<SteeringAgent> allAgents = new List<SteeringAgent>();

        private void OnEnable()
        {
            allAgents.Add(this); // Register this agent
        }

        private void OnDisable()
        {
            allAgents.Remove(this); // Unregister this agent
        }

        void Update()
        {
            // 1. Calculate steering force
            // Steering is an acceleration/force that changes velocity
            Vector3 steeringForce = Vector3.zero; // Initialize steering force

            if (target != null)
            {
                steeringForce += Seek(target.position);
            }
            //
            // if (AllAgents.Count > 1)
            // {
            //     steeringForce += Separation(separationRadius, separationStrength) * separationWeight;
            // }

            // 2. Limit steering force to maxForce
            // Prevents agents from turning instantly
            steeringForce = Vector3.ClampMagnitude(steeringForce, maxForce);

            // 3. Apply steering force to velocity (Integration)
            // Acceleration = Force / Mass (assuming mass = 1)
            // Velocity Change = Acceleration * DeltaTime
            velocity += steeringForce * Time.deltaTime;

            // 4. Limit velocity to maxSpeed
            velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

            // 5. Move agent based on velocity
            transform.position += velocity * Time.deltaTime;

            // 6. Face movement direction
            if (velocity.sqrMagnitude > 0.0001f) // If we're moving significantly, face that direction
            {
                transform.forward = velocity.normalized;
            }
        }

        public Vector3 Seek(Vector3 targetPos)
        {
            // Goal: Move straight towards the target at max speed.
            // Reynolds' formula:
            // Steering Force = Desired Velocity - Current Velocity
            // Desired Velocity: a vector pointing from seeker to target, normalized and scaled to maxSpeed
            // Steering: The difference between where you want to go (desired) and where you are currently going
            Vector3 toTarget = targetPos - transform.position;

            // If already at target, no steering needed
            if (toTarget.sqrMagnitude < 0.0001f) // Why sqrMagnitude? More efficient than magnitude
                return Vector3.zero;

            // Desired velocity: Full speed towards target
            Vector3 desiredVelocity = toTarget.normalized * maxSpeed;

            // Reynolds' steering formula
            return desiredVelocity - velocity;
        }

        public Vector3 Arrive(Vector3 targetPos, float slowingRadius)
        {
            // Goal: Move towards the target but slow down when close and stop at the target.
            // Arrive is similar to Seek but adjusts speed based on distance to target.
            // 1. Compute distance to target
            // 2. If within slowing radius, scale desired speed proportionally to distance
            Vector3 toTarget = targetPos - transform.position;
            float distance = toTarget.magnitude;

            if (distance < 0.0001f) // Already at target
                return Vector3.zero;

            float desiredSpeed = maxSpeed;
            if (distance < slowingRadius)
            {
                desiredSpeed = maxSpeed * (distance / slowingRadius); // Scale speed
            }

            Vector3 desiredVelocity = toTarget.normalized * desiredSpeed;
            return desiredVelocity - velocity;
        }

        public Vector3 Separation(float separationRadius, float separationStrength)
        {
            return Vector3.zero; // TODO: Implement Separation behavior
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebug) return;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + velocity); // Draw velocity vector
        }
    }
}
