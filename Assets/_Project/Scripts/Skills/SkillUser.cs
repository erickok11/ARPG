using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum SkillUseResult
{
    Ready,              // could be used right now (only returned by Check)
    Used,
    Busy,               // still swinging or casting the last skill
    NoSkill,
    RequirementsNotMet,
    NotEnoughMana,
    NoTarget,
}

/// <summary>
/// Uses the skills on a character's SkillBar: checks requirements and mana, spends mana,
/// works out timing from attack or cast speed, and creates the hits, projectiles and slams.
/// Skills are used with their socketed supports already applied (SkillGem.Effective).
/// </summary>
[RequireComponent(typeof(CharacterStats), typeof(Mana), typeof(SkillBar))]
public class SkillUser : MonoBehaviour
{
    [Tooltip("Layers this character's skills can hit. For the player, set this to Enemy.")]
    [SerializeField] private LayerMask targetMask;
    [Tooltip("Layers that stop projectiles. Default = your walls.")]
    [SerializeField] private LayerMask obstacleMask = 1;

    /// <summary>Fires with a short reason when a skill can't be used ("Not enough mana").</summary>
    public event Action<string> UseFailed;
    /// <summary>Fires when a skill is used: (slot, skill).</summary>
    public event Action<int, SkillData> SkillUsed;

    private CharacterStats stats;
    private Mana mana;
    private SkillBar bar;
    private CharacterMover mover;
    private float busyTimer;
    private float busyDuration;
    private readonly HashSet<Health> found = new HashSet<Health>();

    public bool IsBusy => busyTimer > 0f;
    /// <summary>1 right after using a skill, counting down to 0 when you can act again.</summary>
    public float BusyFraction => busyDuration > 0f ? Mathf.Clamp01(busyTimer / busyDuration) : 0f;
    public LayerMask TargetMask => targetMask;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        mana = GetComponent<Mana>();
        bar = GetComponent<SkillBar>();
        mover = GetComponent<CharacterMover>();
    }

    private void Update()
    {
        if (busyTimer > 0f) busyTimer -= Time.deltaTime;
    }

    // -----------------------------------------------------------------
    // Queries (used by PlayerController and the UI)
    // -----------------------------------------------------------------

    public SkillData SkillIn(int slot) => bar.Get(slot)?.Effective;
    public bool HasSkill(int slot) => SkillIn(slot) != null;
    public bool NeedsTarget(int slot) => SkillIn(slot)?.skillKind == SkillKind.Strike;
    public float RangeOf(int slot) => SkillIn(slot)?.range ?? 0f;

    public bool MeetsRequirements(SkillData skill, out string missing)
    {
        missing = null;
        if (skill == null) return false;

        if (!MeetsAttributes(skill.reqStr, skill.reqDex, skill.reqInt, ref missing)) return false;

        // Every socketed support has to be usable too.
        foreach (SupportData support in skill.supports)
        {
            if (MeetsAttributes(support.reqStr, support.reqDex, support.reqInt, ref missing)) continue;
            missing = $"{support.name}: {missing}";
            return false;
        }
        return true;
    }

    public bool HasManaFor(SkillData skill) => skill != null && mana.Has(skill.manaCost);

    /// <summary>Seconds one use takes. Attacks use Attack Speed, spells use their cast time and Cast Speed bonuses.</summary>
    public float UseTime(SkillData skill)
    {
        if (skill.IsAttack)
        {
            float attacksPerSecond = stats.GetValue(StatType.AttackSpeed) * skill.attackSpeedMultiplier;
            return 1f / Mathf.Max(0.1f, attacksPerSecond);
        }

        float castsPerSecond = stats.Get(StatType.CastSpeed).Evaluate(1f / Mathf.Max(0.05f, skill.castTime));
        return 1f / Mathf.Max(0.1f, castsPerSecond);
    }

    public SkillUseResult Check(int slot, out SkillData skill, out string reason)
    {
        reason = null;
        skill = SkillIn(slot);
        if (skill == null) return SkillUseResult.NoSkill;
        if (IsBusy) return SkillUseResult.Busy;
        if (!MeetsRequirements(skill, out reason)) return SkillUseResult.RequirementsNotMet;
        if (!HasManaFor(skill))
        {
            reason = $"Not enough mana for {skill.name}";
            return SkillUseResult.NotEnoughMana;
        }
        return SkillUseResult.Ready;
    }

    public void ReportNoTarget() => UseFailed?.Invoke("No target");

    // -----------------------------------------------------------------
    // Using skills
    // -----------------------------------------------------------------

    /// <summary>Use a skill on a specific enemy (Strikes need this; others aim at the enemy's position).</summary>
    public SkillUseResult TryUseOnTarget(int slot, Health target)
    {
        SkillUseResult check = Check(slot, out SkillData skill, out string reason);
        if (check != SkillUseResult.Ready) return Report(check, reason);
        if (target == null || target.IsDead) return SkillUseResult.NoTarget;

        Begin(slot, skill, target.transform.position);
        switch (skill.skillKind)
        {
            case SkillKind.Strike: Strike(skill, target); break;
            case SkillKind.Projectile: FireProjectiles(skill, target.transform.position); break;
            case SkillKind.Slam: Slam(skill, target.transform.position); break;
        }
        return SkillUseResult.Used;
    }

    /// <summary>Use a skill toward a point on the ground (Projectiles and Slams).</summary>
    public SkillUseResult TryUseAt(int slot, Vector3 point)
    {
        SkillUseResult check = Check(slot, out SkillData skill, out string reason);
        if (check != SkillUseResult.Ready) return Report(check, reason);
        if (skill.skillKind == SkillKind.Strike) return SkillUseResult.NoTarget;

        Begin(slot, skill, point);
        if (skill.skillKind == SkillKind.Projectile) FireProjectiles(skill, point);
        else Slam(skill, point);
        return SkillUseResult.Used;
    }

    private SkillUseResult Report(SkillUseResult result, string reason)
    {
        if (result != SkillUseResult.Busy && !string.IsNullOrEmpty(reason))
            UseFailed?.Invoke(reason);
        return result;
    }

    private void Begin(int slot, SkillData skill, Vector3 aimPoint)
    {
        mana.TrySpend(skill.manaCost);
        busyDuration = UseTime(skill);
        busyTimer = busyDuration;
        if (mover != null) mover.Stop();
        Face(aimPoint);
        SkillUsed?.Invoke(slot, skill);
    }

    /// <summary>One hit's damage. Usually a single result; supports like Added Fire Damage add a second.</summary>
    private DamageResult[] Roll(SkillData skill)
    {
        DamageResult main = skill.IsAttack
            ? DamageCalculator.RollAttack(stats, skill.damageMultiplier * skill.bonusDamageMultiplier)
            : DamageCalculator.RollSpell(stats, skill.parsedDamageType, skill.minDamage, skill.maxDamage,
                                         skill.bonusDamageMultiplier);

        if (skill.extraFireFraction <= 0f) return new[] { main };
        return new[] { main, DamageCalculator.Extra(main, DamageType.Fire, skill.extraFireFraction) };
    }

    private void Strike(SkillData skill, Health target)
    {
        Combat.Hit(gameObject, target, Roll(skill));
        SkillEffect.Burst(target.transform.position, skill.parsedColor, 1.3f, 0.18f);
    }

    private void FireProjectiles(SkillData skill, Vector3 aimPoint)
    {
        Vector3 direction = FlatDirectionTo(aimPoint);
        Vector3 origin = transform.position;
        int count = Mathf.Max(1, skill.projectileCount);
        float startAngle = -skill.spreadAngle * (count - 1) / 2f;

        for (int i = 0; i < count; i++)
        {
            Vector3 d = Quaternion.Euler(0f, startAngle + skill.spreadAngle * i, 0f) * direction;
            Projectile.Spawn(gameObject, origin + d * 0.6f, d, skill.projectileSpeed, skill.range,
                             skill.radius, skill.parsedColor, Roll(skill), skill.pierceCount, targetMask, obstacleMask);
        }
    }

    private void Slam(SkillData skill, Vector3 aimPoint)
    {
        Vector3 center = transform.position + FlatDirectionTo(aimPoint) * skill.range;

        foreach (Health target in FindTargets(center, skill.radius))
            Combat.Hit(gameObject, target, Roll(skill));

        Vector3 ground = NavMesh.SamplePosition(center, out NavMeshHit hit, 3f, NavMesh.AllAreas)
            ? hit.position
            : center;
        SkillEffect.Ring(ground, skill.radius, skill.parsedColor, 0.25f);
    }

    /// <summary>Every living target (on targetMask) within radius of a point.</summary>
    public List<Health> FindTargets(Vector3 center, float radius)
    {
        found.Clear();
        var result = new List<Health>();
        foreach (Collider c in Physics.OverlapSphere(center, radius, targetMask))
        {
            Health h = c.GetComponentInParent<Health>();
            if (h == null || h.IsDead || h.gameObject == gameObject || !found.Add(h)) continue;
            result.Add(h);
        }
        return result;
    }

    private Vector3 FlatDirectionTo(Vector3 point)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
    }

    private void Face(Vector3 point)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    private bool MeetsAttributes(int str, int dex, int intel, ref string missing) =>
        Meets(StatType.Strength, str, ref missing) &&
        Meets(StatType.Dexterity, dex, ref missing) &&
        Meets(StatType.Intelligence, intel, ref missing);

    private bool Meets(StatType attribute, int required, ref string missing)
    {
        if (required <= 0) return true;
        float have = stats.GetValue(attribute);
        if (have + 0.001f >= required) return true;
        missing = $"Requires {required} {attribute} (you have {Mathf.FloorToInt(have)})";
        return false;
    }
}
