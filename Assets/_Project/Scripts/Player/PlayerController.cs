using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Player mouse input:
///   Click ground        -> walk there (hold to keep walking toward the cursor)
///   Click an enemy      -> walk into range and keep attacking until it dies or you click elsewhere
///   Click an item label -> walk to the item and pick it up
/// Clicks inside open windows (inventory, crafting) are ignored here.
/// </summary>
[RequireComponent(typeof(CharacterMover), typeof(MeleeAttack), typeof(Health))]
public class PlayerController : MonoBehaviour
{
    [Header("Clicking")]
    [Tooltip("Set to your Ground layer.")]
    [SerializeField] private LayerMask groundMask;
    [Tooltip("Set to your Enemy layer.")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private float maxRayDistance = 500f;

    [Header("Items")]
    [Tooltip("How close you need to be to pick up an item.")]
    [SerializeField] private float pickupRange = 1.5f;

    [Header("Timing")]
    [Tooltip("While holding the button on the ground, how often to update the destination.")]
    [SerializeField] private float holdRepathInterval = 0.05f;
    [Tooltip("While chasing an enemy or item, how often to update the path.")]
    [SerializeField] private float chaseRepathInterval = 0.1f;

    private CharacterMover mover;
    private MeleeAttack attack;
    private Health health;
    private Inventory inventory;
    private Camera cam;

    private Health attackTarget;
    private ItemPickup pickupTarget;
    private float repathTimer;
    private bool pressStartedOverUI;

    public Health CurrentTarget => attackTarget;

    private void Awake()
    {
        mover = GetComponent<CharacterMover>();
        attack = GetComponent<MeleeAttack>();
        health = GetComponent<Health>();
        inventory = GetComponent<Inventory>();
    }

    private void OnEnable() => health.Died += OnDied;
    private void OnDisable() => health.Died -= OnDied;

    private void Start()
    {
        cam = Camera.main;
        if (cam == null)
            Debug.LogError("PlayerController: no camera tagged MainCamera in the scene.");
    }

    private void Update()
    {
        if (health.IsDead) return;

        repathTimer -= Time.deltaTime;
        HandleMouse();
        UpdateAttack();
        UpdatePickup();
    }

    private void HandleMouse()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || cam == null || !mouse.leftButton.isPressed) return;

        Vector2 screenPosition = mouse.position.ReadValue();
        Vector2 guiPosition = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
        Ray ray = cam.ScreenPointToRay(screenPosition);

        if (mouse.leftButton.wasPressedThisFrame)
        {
            pressStartedOverUI = IsPointerOverUI() || ClickBlocker.IsPointerOver(guiPosition);
            if (pressStartedOverUI) return;

            // Item labels take priority, like in PoE.
            if (ItemPickup.TryGetLabelAt(guiPosition, out ItemPickup clickedItem))
            {
                pickupTarget = clickedItem;
                attackTarget = null;
                repathTimer = 0f;
                return;
            }

            if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, groundMask | enemyMask)) return;

            Health clickedEnemy = IsOnLayer(hit.collider.gameObject, enemyMask)
                ? hit.collider.GetComponentInParent<Health>()
                : null;

            pickupTarget = null;
            if (clickedEnemy != null && !clickedEnemy.IsDead)
            {
                attackTarget = clickedEnemy;
                repathTimer = 0f;
            }
            else
            {
                attackTarget = null;
                mover.MoveTo(hit.point);
                repathTimer = holdRepathInterval;
            }
            return;
        }

        // Button held: keep walking toward the cursor (unless busy attacking or picking up).
        if (pressStartedOverUI || attackTarget != null || pickupTarget != null || repathTimer > 0f) return;
        repathTimer = holdRepathInterval;

        if (Physics.Raycast(ray, out RaycastHit groundHit, maxRayDistance, groundMask))
            mover.MoveTo(groundHit.point);
    }

    private void UpdateAttack()
    {
        if (attackTarget == null) return;

        if (attackTarget.IsDead)
        {
            attackTarget = null;
            mover.Stop();
            return;
        }

        if (attack.InRange(attackTarget.transform))
        {
            mover.Stop();
            mover.FaceTowards(attackTarget.transform.position);
            attack.TryAttack(attackTarget);
        }
        else if (repathTimer <= 0f)
        {
            repathTimer = chaseRepathInterval;
            mover.MoveTo(attackTarget.transform.position);
        }
    }

    private void UpdatePickup()
    {
        if (pickupTarget == null) return;

        Vector3 offset = pickupTarget.transform.position - transform.position;
        offset.y = 0f;

        if (offset.sqrMagnitude <= pickupRange * pickupRange)
        {
            mover.Stop();
            if (inventory != null && inventory.Add(pickupTarget.Item))
                Destroy(pickupTarget.gameObject);
            else
                Debug.Log(inventory == null ? "Player has no Inventory component." : "Inventory is full.");
            pickupTarget = null;
        }
        else if (repathTimer <= 0f)
        {
            repathTimer = chaseRepathInterval;
            mover.MoveTo(pickupTarget.transform.position);
        }
    }

    private void OnDied()
    {
        attackTarget = null;
        pickupTarget = null;
        mover.DisableMovement();
        Debug.Log("You died. Stop and press Play to try again.");
    }

    private static bool IsOnLayer(GameObject go, LayerMask mask)
    {
        return (mask.value & (1 << go.layer)) != 0;
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
