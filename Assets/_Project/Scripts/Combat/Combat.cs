using System;
using UnityEngine;

/// <summary>
/// The one place damage gets dealt. Basic attacks, skills and projectiles all go through Combat.Hit,
/// so damage numbers (and later things like life leech or on-hit effects) only need to listen here.
/// </summary>
public static class Combat
{
    /// <summary>Fires for every hit by anyone: (attacker, target, result). Attacker can be null.</summary>
    public static event Action<GameObject, Health, DamageResult> AnyHit;

    public static void Hit(GameObject attacker, Health target, DamageResult result)
    {
        if (target == null || target.IsDead) return;
        target.TakeDamage(result.Amount);
        AnyHit?.Invoke(attacker, target, result);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => AnyHit = null;
}
