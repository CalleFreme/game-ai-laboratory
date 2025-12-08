using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
  public enum State { Idle, Patrol, Chase }
  public State currentState;

  [Header("References")]
  public NavMeshAgent agent;
  public Transform player;

  [Header("Idle")]
  public float idleTime = 2f;
  private float idleTimer;

  [Header("Patrol")]
  public Transform[] patrolPoints;
  private int patrolIndex = 0;

  [Header("Chase")]
  public float chaseRange = 10f;

  void Start()
  {
    if (agent == null) agent = GetComponent<NavMeshAgent>();
    ChangeState(State.Idle);
  }

  void Update()
  {
    switch (currentState)
    {
      case State.Idle:
        UpdateIdle();
        break;
      case State.Patrol:
        UpdatePatrol();
        break;
      case State.Chase:
        UpdateChase();
        break;
    }
  }

  void UpdateIdle()
  {
    idleTimer += Time.deltaTime;
    if (idleTimer >= idleTime)
    {
      ChangeState(State.Patrol);
    }

    if (IsPlayerInRange())
    {
      ChangeState(State.Chase);
    }
  }

  void UpdatePatrol()
  {
    agent.isStopped = false;
    agent.SetDestination(patrolPoints[patrolIndex].position);

    float distanceToPoint = Vector3.Distance(transform.position, patrolPoints[patrolIndex].position);

    if (distanceToPoint < 3f)
    {
      patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
      ChangeState(State.Idle);
    }

    if (IsPlayerInRange())
    {
      ChangeState(State.Chase);
    }
  }

  void UpdateChase()
  {
    agent.isStopped = false;
    agent.SetDestination(player.position);

    if (!IsPlayerInRange())
    {
      ChangeState(State.Patrol);
    }
  }

  bool IsPlayerInRange()
  {
    return Vector3.Distance(transform.position, player.position) <= chaseRange;
  }

  void ChangeState(State newState)
  {
    currentState = newState;

    switch (newState)
    {
      case State.Idle:
        agent.isStopped = true;
        idleTimer = 0f;
        break;

      case State.Patrol:
        agent.isStopped = false;
        break;

      case State.Chase:
        agent.isStopped = false;
        break;
    }
  }
}