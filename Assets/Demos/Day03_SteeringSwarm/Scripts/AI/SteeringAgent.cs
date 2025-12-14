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
        [Tooltip("When speed drops below this near target, stop completely")]
        public float stoppingSpeed = 0.1f;
        [Tooltip("Distance at which agent is consered 'arrived' and will stop")]
        public float arriveThresholdDistance = 0.5f;

        [Header("Separation")]
        public float separationRadius = 1.5f;
        public float separationStrength = 5f; // How strongly we want to separate from nearby agents

        // Our Obstacle cubes should have BoxCollider by default
        // We need to create an appropriate Layer for obstacles and assign it in the inspector
        // 1. Select an obstacle object in the hierarchy
        // 2. In the Inspector, click on the "Layer" dropdown at the top right
        // 3. Choose "Add Layer..." and create a new layer named "Obstacle"
        // 4. Assign the "Obstacle" layer to your obstacle objects
        // 5. If prompted to "Change children to Obstacle layer?", choose "Yes"
        // 6. Repeat for all obstacle objects in the scene, or use Prefabs for consistency
        [Header("Obstacle Avoidance")]
        public float obstacleAvoidanceRadius = 2f;
        public float obstacleAvoidanceStrength = 10f;
        [Tooltip("How far ahead to look for obstacles")]
        public float lookAheadDistance = 3f;
        [Tooltip("Layer mask for obstacles")]
        public LayerMask obstacleLayerMask = ~0; // Default to everything

        // Ground also needs a layer set up for ground following
        // Follow similar steps as above to create and assign a "Ground" layer
        [Header("Ground Following")]
        public float groundCheckDistance = 10f;
        public float hoverHeight = 0.5f;
        public LayerMask groundLayer = ~0;

        [Header("Weights")]
        public float arriveWeight = 1f;
        public float separationWeight = 1f;
        public float obstacleAvoidanceWeight = 2f; // Obstacle avoidance is often more important

        [Header("Debug")]
        public bool drawDebug = true;

        private Vector3 velocity = Vector3.zero; // Current velocity of the 
        // Optional target for Seek / Arrive behaviors
        public Transform target;
        // Static list to keep track of all agents in the scene
        public static List<SteeringAgent> allAgents = new List<SteeringAgent>();

        private const float MIN_DISTANCE = 0.01f;

        private bool hasArrived = false;

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
            Vector3 totalSteering = Vector3.zero; // Initialize steering force
            // 0. Obstacle avoidance
            Vector3 avoidance = ObstacleAvoidance();
            totalSteering += avoidance * obstacleAvoidanceWeight;

            // 1. Calculate steering force
            // Steering is an acceleration/force that changes velocity

            if (target != null)
            {
                // Use Seek or Arrive towards the target
                totalSteering += Arrive(target.position, slowingRadius) * arriveWeight;

                // Check if we've arrived
                float distanceToTarget = FlatDistance(transform.position, target.position);
                if (distanceToTarget < arriveThresholdDistance && velocity.magnitude < stoppingSpeed)
                {
                    hasArrived = true;
                }
                else
                {
                    hasArrived = false;
                }
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

            // Settling behaviour when arrived
            if (hasArrived)
            {
                velocity *= 0.9f; // Lose 10% of speed each frame when arrived

                // If moving very slowly, stop completely
                if (velocity.magnitude < stoppingSpeed)
                {
                    velocity = Vector3.zero;
                }
            }
            else
            {
                velocity *= 0.99f; // Natural damping to prevent perpetual motion
            }

            // 5. Move agent based on velocity
            transform.position += velocity * Time.deltaTime; // Need deltaTime again for frame rate independence

            FollowGround();


            // 6. Face movement direction
            if (velocity.sqrMagnitude > 0.0001f) // If we're moving significantly, face that direction
            {
                transform.forward = velocity.normalized;
            }
        }

        private void FollowGround()
        {
            Vector3 rayOrigin = transform.position + Vector3.up * 5f; // Start ray above agent

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer))
            {
                Vector3 pos = transform.position;
                pos.y = hit.point.y + hoverHeight; // Keep on ground plane
                transform.position = pos;
            }
        }

        public Vector3 ObstacleAvoidance()
        {
            // Don't avoid if not moving
            if (velocity.sqrMagnitude < 0.001f)
                return Vector3.zero;

            Vector3 ahead = velocity.normalized;
            Vector3 avoidanceForce = Vector3.zero;

            // Cast rays in a fan pattern ahead of the agent
            float[] angles = { 0f, -30f, 30f, -60f, 60f }; // Straight, left, right, wider left, wider right

            foreach (float angle in angles)
            {
                Vector3 direction = Quaternion.Euler(0, angle, 0) * ahead;
                Ray ray = new Ray(transform.position + Vector3.up * 0.5f, direction);

                if (Physics.Raycast(ray, out RaycastHit hit, lookAheadDistance, obstacleLayerMask))
                {
                    // Found an obstacle -> steer away from it
                    // The closer the obstacle, the stronger the force
                    float proximity = 1f - (hit.distance / lookAheadDistance);

                    // Steer perpendicular to the hit normal (slide along the wall)
                    Vector3 avoidDir = Vector3.Cross(Vector3.up, hit.normal).normalized;

                    // Choose the direction that's more aligned with our current velocity
                    if (Vector3.Dot(avoidDir, velocity) < 0)
                        avoidDir = -avoidDir;

                    avoidanceForce += avoidDir * proximity * obstacleAvoidanceStrength;

                    if (drawDebug)
                    {
                        Debug.DrawRay(ray.origin, direction * hit.distance, Color.red);
                    }
                }
                else if (drawDebug)
                {
                    Debug.DrawRay(ray.origin, direction * lookAheadDistance, Color.green);
                }
            }

            avoidanceForce.y = 0f; // Keep in XZ plane
            return avoidanceForce;
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

            Vector3 toTarget = targetPos - myPos;
            float distance = toTarget.magnitude;

            if (distance < arriveThresholdDistance) // Already at target
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

            // Use flattened position
            Vector3 myPos = new Vector3(transform.position.x, 0f, transform.position.z);

            foreach (SteeringAgent other in allAgents)
            {
                if (other == this || other == null) continue; // Skip self

                // Use flattened position for other agent too
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

        private float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz); // Pythagorean theorem in XZ plane
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebug) return;

            // Velocity
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + velocity);

            // Separation radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, separationRadius);

            // Obstacle avoidance radius
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, obstacleAvoidanceRadius);

            // Arrival threshold
            if (target != null)
            {
                Gizmos.color = hasArrived ? Color.green : Color.cyan;
                Gizmos.DrawWireSphere(target.position, arriveThresholdDistance);
            }
        }
    }
}
