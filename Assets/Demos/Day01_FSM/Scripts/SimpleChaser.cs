using UnityEngine;
using UnityEngine.AI;

namespace GameAI.Day01
{
    public enum EnemyState
    {
        Idle,
        Chasing
    }

    [RequireComponent(typeof(NavMeshAgent))]
    public class SimpleChaser : MonoBehaviour
    {

        [Header("References")]
        public Transform target; // Probably the player

        [Header("Chase settings")]
        public float chaseRange = 10f;

        private NavMeshAgent _agent;
        private EnemyState _state = EnemyState.Idle;

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            if (target == null)
            {
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, target.position);

            switch(_state)
            {
                case EnemyState.Idle:
                    UpdateIdle(distanceToTarget);
                    break;
                case EnemyState.Chasing:
                    UpdateChasing(distanceToTarget);
                    break;
            }
        }

        void UpdateIdle(float distanceToTarget)
        {
            // Transition: Idle -> Chasing
            if (distanceToTarget <= chaseRange)
            {
                _state = EnemyState.Chasing;
            }
        }

        void UpdateChasing(float distanceToTarget)
        {
            // While chasing, keep updating destination
            _agent.SetDestination(target.position);
            // Transition: Chasing -> Idle
            if (distanceToTarget > chaseRange)
            {
                _state = EnemyState.Idle;
                _agent.ResetPath();
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
        }
    }
}
