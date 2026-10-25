using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// PoE/Diablo-style movement: click (or hold) the left mouse button on the ground
/// and the character walks there along the NavMesh.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class ClickToMove : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Layers that count as walkable ground for clicks. Set this to your Ground layer.")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float maxRayDistance = 500f;
    [Tooltip("How far from the clicked point to search for a valid NavMesh position.")]
    [SerializeField] private float navMeshSnapDistance = 2f;

    [Header("Movement")]
    [Tooltip("Only used if this object has no CharacterStats. Otherwise speed comes from the MovementSpeed stat.")]
    [SerializeField] private float moveSpeed = 6f;
    [Tooltip("Turning speed in degrees per second.")]
    [SerializeField] private float turnSpeed = 1080f;
    [Tooltip("While the button is held, how often (seconds) to update the destination.")]
    [SerializeField] private float holdRepathInterval = 0.05f;

    private NavMeshAgent agent;
    private CharacterStats stats;
    private Camera cam;
    private float repathTimer;

    public bool IsMoving => agent.hasPath && agent.remainingDistance > agent.stoppingDistance;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stats = GetComponent<CharacterStats>();

        agent.acceleration = 100f;   // near-instant starts and stops feel right for ARPGs
        agent.angularSpeed = 0f;
        agent.updateRotation = false; // rotation is handled manually below for snappier turns
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

    private void OnStatChanged(StatType type)
    {
        if (type == StatType.MovementSpeed) ApplyMoveSpeed();
    }

    private void ApplyMoveSpeed()
    {
        agent.speed = stats != null ? stats.GetValue(StatType.MovementSpeed) : moveSpeed;
    }

    private void Start()
    {
        cam = Camera.main;
        if (cam == null)
            Debug.LogError("ClickToMove: no camera tagged MainCamera in the scene.");
    }

    private void Update()
    {
        HandleMouseInput();
        FaceMovementDirection();
    }

    // ---------------------------------------------------------------
    // Public API (used later by attacks and skills)
    // ---------------------------------------------------------------

    /// <summary>Walk to a world position (snapped onto the NavMesh).</summary>
    public bool MoveTo(Vector3 worldPosition)
    {
        if (NavMesh.SamplePosition(worldPosition, out NavMeshHit navHit, navMeshSnapDistance, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(navHit.position);
            return true;
        }
        return false;
    }

    /// <summary>Stop moving immediately (e.g. when starting an attack).</summary>
    public void Stop()
    {
        agent.isStopped = true;
        agent.ResetPath();
    }

    // ---------------------------------------------------------------
    // Internals
    // ---------------------------------------------------------------

    private void HandleMouseInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || cam == null) return;

        repathTimer -= Time.deltaTime;

        if (!mouse.leftButton.isPressed) return;

        bool pressedThisFrame = mouse.leftButton.wasPressedThisFrame;
        if (pressedThisFrame && IsPointerOverUI()) return;
        if (!pressedThisFrame && repathTimer > 0f) return;
        repathTimer = holdRepathInterval;

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, groundMask))
        {
            MoveTo(hit.point);
        }
    }

    private void FaceMovementDirection()
    {
        Vector3 direction = agent.desiredVelocity;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
