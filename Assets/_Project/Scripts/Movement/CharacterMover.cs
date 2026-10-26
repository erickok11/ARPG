using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Moves any character (player or monster) along the NavMesh.
/// Speed comes from the MovementSpeed stat. Has no input of its own:
/// PlayerController and EnemyAI tell it where to go.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class CharacterMover : MonoBehaviour
{
    [Tooltip("Only used if this object has no CharacterStats.")]
    [SerializeField] private float fallbackSpeed = 6f;
    [Tooltip("Turning speed in degrees per second.")]
    [SerializeField] private float turnSpeed = 1080f;
    [Tooltip("How far from a requested point to search for a valid NavMesh position.")]
    [SerializeField] private float navMeshSnapDistance = 2f;

    private NavMeshAgent agent;
    private CharacterStats stats;

    public bool IsMoving =>
        agent.enabled && agent.isOnNavMesh && agent.hasPath && agent.remainingDistance > agent.stoppingDistance;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stats = GetComponent<CharacterStats>();

        agent.acceleration = 100f;    // near-instant starts and stops feel right for ARPGs
        agent.angularSpeed = 0f;
        agent.updateRotation = false;  // rotation is handled manually for snappier turns
        agent.autoBraking = true;
        ApplyMoveSpeed();
    }

    private void OnEnable()
    {
        if (stats != null) stats.StatChanged += OnStatChanged;
    }

    private void OnDisable()
    {
        if (stats != null) stats.StatChanged -= OnStatChanged;
    }

    private void Update()
    {
        FaceMovementDirection();
    }

    /// <summary>Walk to a world position (snapped onto the NavMesh).</summary>
    public bool MoveTo(Vector3 worldPosition)
    {
        if (!agent.enabled || !agent.isOnNavMesh) return false;

        if (NavMesh.SamplePosition(worldPosition, out NavMeshHit navHit, navMeshSnapDistance, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(navHit.position);
            return true;
        }
        return false;
    }

    /// <summary>Stop moving immediately.</summary>
    public void Stop()
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;
        agent.isStopped = true;
        agent.ResetPath();
    }

    /// <summary>Turn instantly to face a point (e.g. when attacking).</summary>
    public void FaceTowards(Vector3 point)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    /// <summary>Permanently stop this character from moving (e.g. on death).</summary>
    public void DisableMovement()
    {
        Stop();
        agent.enabled = false;
    }

    /// <summary>Turn movement back on after DisableMovement (e.g. when respawning), placing the character at a point.</summary>
    public void EnableMovement(Vector3 position)
    {
        if (NavMesh.SamplePosition(position, out NavMeshHit navHit, navMeshSnapDistance, NavMesh.AllAreas))
            position = navHit.position;

        agent.enabled = true;
        agent.Warp(position);
        Stop();
    }

    private void OnStatChanged(StatType type)
    {
        if (type == StatType.MovementSpeed) ApplyMoveSpeed();
    }

    private void ApplyMoveSpeed()
    {
        agent.speed = stats != null ? stats.GetValue(StatType.MovementSpeed) : fallbackSpeed;
    }

    private void FaceMovementDirection()
    {
        if (!agent.enabled) return;

        Vector3 direction = agent.desiredVelocity;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }
}
