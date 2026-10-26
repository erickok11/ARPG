using System;
using UnityEngine;

/// <summary>
/// A basic melee hit used by both the player and monsters.
/// Attack rate comes from the AttackSpeed stat, damage from DamageCalculator.
/// </summary>
[RequireComponent(typeof(CharacterStats))]
public class MeleeAttack : MonoBehaviour
{
    /// <summary>Fires for every successful hit by anyone (used by damage numbers).</summary>
    public static event Action<MeleeAttack, Health, DamageResult> AnyHit;

    [Tooltip("Max distance (center to center) to land a hit.")]
    [SerializeField] private float range = 2f;

    private CharacterStats stats;
    private float cooldown;

    public float Range => range;
    public bool IsReady => cooldown <= 0f;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
    }

    private void Update()
    {
        if (cooldown > 0f) cooldown -= Time.deltaTime;
    }

    public bool InRange(Transform target)
    {
        Vector3 offset = target.position - transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= range * range;
    }

    /// <summary>Hits the target if it's alive, in range, and the attack is off cooldown.</summary>
    public bool TryAttack(Health target)
    {
        if (target == null || target.IsDead || !IsReady || !InRange(target.transform))
            return false;

        Vector3 direction = target.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction);

        float attacksPerSecond = Mathf.Max(0.1f, stats.GetValue(StatType.AttackSpeed));
        cooldown = 1f / attacksPerSecond;

        DamageResult result = DamageCalculator.RollAttack(stats);
        target.TakeDamage(result.Amount);
        AnyHit?.Invoke(this, target, result);
        return true;
    }
}
