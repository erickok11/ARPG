using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Player input:
///   Left-click ground        -> walk there (hold to keep walking toward the cursor)
///   Left-click an enemy      -> walk into range and basic-attack until it dies or you click elsewhere
///   Left-click an item label -> walk to the item and pick it up
///   Q / W / E / R            -> use the skill on that slot (hold to keep using it)
///       Strike skills target the enemy under (or near) the cursor and walk into range first.
///       Projectile and Slam skills fire toward the cursor right away.
/// Clicks on open windows and the HUD are ignored here.
/// When you die you lose some experience (CharacterLevel) and come back after a few seconds.
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

    [Header("Skills")]
    [Tooltip("If a Strike skill is used with no enemy under the cursor, use the nearest enemy within this distance of the cursor.")]
    [SerializeField] private float strikeTargetAssist = 2.5f;

    [Header("Timing")]
    [Tooltip("While holding the button on the ground, how often to update the destination.")]
    [SerializeField] private float holdRepathInterval = 0.05f;
    [Tooltip("While chasing an enemy or item, how often to update the path.")]
    [SerializeField] private float chaseRepathInterval = 0.1f;

    [Header("Death")]
    [Tooltip("Seconds after dying before you come back.")]
    [SerializeField] private float respawnDelay = 3f;
    [Tooltip("Where you come back. Leave empty to use where you started.")]
    [SerializeField] private Transform respawnPoint;

    private CharacterMover mover;
    private MeleeAttack attack;
    private Health health;
    private Inventory inventory;
    private SkillUser skillUser;
    private Mana mana;
    private CharacterLevel characterLevel;
    private Camera cam;
    private Vector3 startPosition;

    private Health attackTarget;
    private int targetSkillSlot = -1;  // -1 = basic attack; otherwise the skill slot being used on attackTarget
    private bool repeatSkill;          // keep using the skill on the target while its key is held
    private ItemPickup pickupTarget;
    private float repathTimer;
    private bool pressStartedOverUI;

    public Health CurrentTarget => attackTarget;
    /// <summary>Seconds until you respawn while dead, otherwise 0.</summary>
    public float RespawnTimeLeft { get; private set; }

    private void Awake()
    {
        mover = GetComponent<CharacterMover>();
        attack = GetComponent<MeleeAttack>();
        health = GetComponent<Health>();
        inventory = GetComponent<Inventory>();
        skillUser = GetComponent<SkillUser>();
        mana = GetComponent<Mana>();
        characterLevel = GetComponent<CharacterLevel>();
        startPosition = transform.position;
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

        // Mid-swing or mid-cast: you can't do anything else until it finishes.
        if (skillUser != null && skillUser.IsBusy) return;

        HandleSkillKeys();
        HandleMouse();
        UpdateAttack();
        UpdatePickup();
    }

    // -----------------------------------------------------------------
    // Skills
    // -----------------------------------------------------------------

    private void HandleSkillKeys()
    {
        Keyboard kb = Keyboard.current;
        if (skillUser == null || kb == null || cam == null) return;

        // Letting go of the key stops repeating (the current target still gets one use).
        if (targetSkillSlot >= 0 && !SkillKey(kb, targetSkillSlot).isPressed) repeatSkill = false;

        for (int slot = 0; slot < SkillBar.SlotCount; slot++)
        {
            KeyControl key = SkillKey(kb, slot);
            if (!key.isPressed || !skillUser.HasSkill(slot)) continue;
            bool pressedThisFrame = key.wasPressedThisFrame;

            if (skillUser.NeedsTarget(slot))
            {
                // Already walking to / hitting a target with this skill: keep going.
                if (!pressedThisFrame && targetSkillSlot == slot && attackTarget != null) return;

                Health target = FindStrikeTarget();
                if (target == null)
                {
                    if (pressedThisFrame) skillUser.ReportNoTarget();
                    return;
                }

                attackTarget = target;
                targetSkillSlot = slot;
                repeatSkill = true;
                pickupTarget = null;
                repathTimer = 0f;
                return;
            }

            // Projectiles and slams go toward the cursor immediately.
            if (TryGetCursorGround(out Vector3 point))
            {
                ClearAttackTarget();
                pickupTarget = null;
                skillUser.TryUseAt(slot, point);
            }
            return;
        }
    }

    private static KeyControl SkillKey(Keyboard kb, int slot) => slot switch
    {
        0 => kb.qKey,
        1 => kb.wKey,
        2 => kb.eKey,
        _ => kb.rKey,
    };

    private Health FindStrikeTarget()
    {
        Health underCursor = EnemyUnderCursor();
        if (underCursor != null) return underCursor;
        if (!TryGetCursorGround(out Vector3 point)) return null;

        Health nearest = null;
        float best = float.MaxValue;
        foreach (Collider c in Physics.OverlapSphere(point, strikeTargetAssist, enemyMask))
        {
            Health h = c.GetComponentInParent<Health>();
            if (h == null || h.IsDead) continue;
            float d = (h.transform.position - point).sqrMagnitude;
            if (d < best) { best = d; nearest = h; }
        }
        return nearest;
    }

    // -----------------------------------------------------------------
    // Mouse
    // -----------------------------------------------------------------

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
                ClearAttackTarget();
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
                targetSkillSlot = -1;   // left-click = basic attack
                repathTimer = 0f;
            }
            else
            {
                ClearAttackTarget();
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

    // -----------------------------------------------------------------
    // Attacking a target (basic attack or Strike skill)
    // -----------------------------------------------------------------

    private void UpdateAttack()
    {
        if (attackTarget == null) return;

        if (attackTarget.IsDead)
        {
            ClearAttackTarget();
            mover.Stop();
            return;
        }

        bool usingSkill = targetSkillSlot >= 0 && skillUser != null;
        float range = usingSkill ? skillUser.RangeOf(targetSkillSlot) : attack.Range;

        if (FlatDistance(attackTarget.transform.position) <= range)
        {
            mover.Stop();
            mover.FaceTowards(attackTarget.transform.position);

            if (!usingSkill)
            {
                attack.TryAttack(attackTarget);
                return;
            }

            SkillUseResult result = skillUser.TryUseOnTarget(targetSkillSlot, attackTarget);
            if (result == SkillUseResult.Used)
            {
                if (!repeatSkill) ClearAttackTarget();
            }
            else if (result != SkillUseResult.Busy)
            {
                ClearAttackTarget(); // no mana, missing requirements, etc. (the HUD shows why)
            }
        }
        else if (repathTimer <= 0f)
        {
            repathTimer = chaseRepathInterval;
            mover.MoveTo(attackTarget.transform.position);
        }
    }

    private void ClearAttackTarget()
    {
        attackTarget = null;
        targetSkillSlot = -1;
        repeatSkill = false;
    }

    // -----------------------------------------------------------------
    // Items
    // -----------------------------------------------------------------

    private void UpdatePickup()
    {
        if (pickupTarget == null) return;

        if (FlatDistance(pickupTarget.transform.position) <= pickupRange)
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

    // -----------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------

    private bool TryGetCursorGround(out Vector3 point)
    {
        point = default;
        Mouse mouse = Mouse.current;
        if (mouse == null || cam == null) return false;

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, groundMask)) return false;
        point = hit.point;
        return true;
    }

    private Health EnemyUnderCursor()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || cam == null) return null;

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, groundMask | enemyMask)) return null;
        if (!IsOnLayer(hit.collider.gameObject, enemyMask)) return null;

        Health h = hit.collider.GetComponentInParent<Health>();
        return h != null && !h.IsDead ? h : null;
    }

    private float FlatDistance(Vector3 point)
    {
        Vector3 offset = point - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    private void OnDied()
    {
        ClearAttackTarget();
        pickupTarget = null;
        mover.DisableMovement();

        int lost = characterLevel != null ? characterLevel.ApplyDeathPenalty() : 0;
        Debug.Log(lost > 0 ? $"You died and lost {lost} experience." : "You died.");
        StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        RespawnTimeLeft = respawnDelay;
        while (RespawnTimeLeft > 0f)
        {
            RespawnTimeLeft -= Time.deltaTime;
            yield return null;
        }
        RespawnTimeLeft = 0f;

        mover.EnableMovement(respawnPoint != null ? respawnPoint.position : startPosition);
        health.Revive();
        if (mana != null) mana.Restore(mana.Max);
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
