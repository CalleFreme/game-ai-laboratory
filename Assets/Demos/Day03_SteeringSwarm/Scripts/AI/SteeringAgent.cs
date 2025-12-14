using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Demos.Day03_SteeringSwarm.Scripts.AI
{
    public class SteeringAgent : MonoBehaviour
    {
        [Header("Movement")]
        public float maxSpeed = 5f;
        public float maxForce = 10f; // Limit how "fast" we change direction (turning radius)

        [Header("Arrive")]
        public float slowingRadius = 3f;

        [Header("Separation")]
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

        private const float MIN_DISTANCE = 0.01f;

        private void OnEnable()
        {
            if (!allAgents.Contains(this))
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
            Vector3 totalSteering = Vector3.zero; // Initialize steering force

            if (target != null)
            {
                // Use Seek or Arrive towards the target
                totalSteering += Arrive(target.position, slowingRadius) * arriveWeight;
            }

            // Add separation if there are neighbours
            if (allAgents.Count > 1)
            {
                totalSteering += Separation(separationRadius, separationStrength) * separationWeight;
            }

            // 2. Limit steering force to maxForce
            // Prevents agents from turning instantly
            totalSteering = Vector3.ClampMagnitude(totalSteering, maxForce);

            // 3. Apply steering force to velocity (Integration)
            // Acceleration = Force / Mass (assuming mass = 1)
            // Velocity Change = Acceleration * DeltaTime
            velocity += totalSteering * Time.deltaTime;

            // 4. Limit velocity to maxSpeed
            velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

            velocity.y = 0f; // Keep movement in the XZ plane

            // 5. Move agent based on velocity
            transform.position += velocity * Time.deltaTime; // Need deltaTime again for frame rate independence

            Vector3 pos = transform.position;
            pos.y = 1f; // Keep on ground plane
            transform.position = pos;

            // 6. Face movement direction
            if (velocity.sqrMagnitude > 0.0001f) // If we're moving significantly, face that direction
            {
                transform.forward = velocity.normalized;
            }
        }

        public Vector3 Seek(Vector3 targetPosition)
        {
            // Goal: Move straight towards the target at max speed.
            // Reynolds' formula:
            // Steering Force = Desired Velocity - Current Velocity
            // Desired Velocity: a vector pointing from seeker to target, normalized and scaled to maxSpeed
            // Steering: The difference between where you want to go (desired) and where you are currently going

            // Flatten to XZ plane
            Vector3 myPos = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 targetPos = new Vector3(targetPosition.x, 0f, targetPosition.z);

            Vector3 toTarget = targetPos - myPos;

            // If already at target, no steering needed
            if (toTarget.sqrMagnitude < 0.0001f) // Why sqrMagnitude? More efficient than magnitude
                return Vector3.zero;

            // Desired velocity: Full speed towards target
            Vector3 desiredVelocity = toTarget.normalized * maxSpeed;

            // Reynolds' steering formula
            return desiredVelocity - velocity;
        }

        public Vector3 Arrive(Vector3 targetPosition, float slowingRadius)
        {
            // Goal: Move towards the target but slow down when close and stop at the target.
            // Arrive is similar to Seek but adjusts speed based on distance to target.
            // 1. Compute distance to target
            // 2. If within slowing radius, scale desired speed proportionally to distance

            // Flatten to XZ plane
            Vector3 myPos = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 targetPos = new Vector3(targetPosition.x, 0f, targetPosition.z);

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
            Vector3 separationForce = Vector3.zero;
            int neighborCount = 0;

            // *** FIX: Use flattened position ***
            Vector3 myPos = new Vector3(transform.position.x, 0f, transform.position.z);

            foreach (SteeringAgent other in allAgents)
            {
                if (other == this) continue; // Skip self
                if (other == null) continue; // Skip null references

                // Use flattened position for other agent too ***
                Vector3 otherPos = new Vector3(other.transform.position.x, 0f, other.transform.position.z);

                Vector3 toMe = myPos - otherPos;
                float distance = toMe.magnitude;

                if (distance < MIN_DISTANCE)
                {
                    // Agents are overlapping, apply a random small force to separate them
                    float randomAngle = Random.Range(0f, Mathf.PI * 2f);
                    toMe = new Vector3(Mathf.Cos(randomAngle), 0f, Mathf.Sin(randomAngle));
                    distance = MIN_DISTANCE; // Prevent division by zero
                }

                // If they are within my personal space
                if (distance > 0f && distance < separationRadius)
                {
                    // Weight: 1/dist means closer neigbours push MUCH harder
                    float weight = Mathf.Clamp(1f / distance, 0f, 10f); // Clamp to avoid extreme forces
                    separationForce += toMe.normalized * weight;
                    neighborCount++;
                }
            }

            if (neighborCount > 0)
            // Need to average the force based on number of neighbors, so we don't get excessively large forces
            {
                separationForce /= neighborCount; // Average direction

                if (separationForce.sqrMagnitude > 0.0001f)
                {
                    // Convert "move away" direction into a steering force
                    Vector3 desiredVelocity = separationForce.normalized * maxSpeed;
                    Vector3 steering = desiredVelocity - velocity;

                    steering *= separationStrength; // Scale by strength

                    steering.y = 0f; // Keep in XZ plane

                    return steering;
                }
            }

            return Vector3.zero;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebug) return;

            // Draw velocity
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + velocity);

            // Draw separation radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, separationRadius);
        }
    }
}
