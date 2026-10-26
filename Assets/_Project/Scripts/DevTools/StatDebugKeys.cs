using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Temporary test keys for the stat system. Remove from the player once you're happy it works.
///   1 = +30% increased movement speed
///   2 = +50 flat max life
///   3 = 50% less movement speed
///   H = take 20 damage
///   L = gain a level (needs CharacterLevel)
///   0 = remove all test modifiers
/// </summary>
[RequireComponent(typeof(CharacterStats))]
public class StatDebugKeys : MonoBehaviour
{
    private CharacterStats stats;
    private Health health;
    private CharacterLevel characterLevel;
    private readonly object debugSource = new object();

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        health = GetComponent<Health>();
        characterLevel = GetComponent<CharacterLevel>();
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame)
        {
            stats.AddModifier(StatType.MovementSpeed, new StatModifier(30f, ModifierType.Increased, debugSource));
            Log(StatType.MovementSpeed);
        }
        if (kb.digit2Key.wasPressedThisFrame)
        {
            stats.AddModifier(StatType.MaxLife, new StatModifier(50f, ModifierType.Flat, debugSource));
            Log(StatType.MaxLife);
        }
        if (kb.digit3Key.wasPressedThisFrame)
        {
            stats.AddModifier(StatType.MovementSpeed, new StatModifier(-50f, ModifierType.More, debugSource));
            Log(StatType.MovementSpeed);
        }
        if (kb.hKey.wasPressedThisFrame && health != null)
        {
            health.TakeDamage(20f);
            Debug.Log($"Life: {health.Current:0}/{health.Max:0}");
        }
        if (kb.lKey.wasPressedThisFrame && characterLevel != null)
            characterLevel.DebugGainLevel();
        if (kb.digit0Key.wasPressedThisFrame)
        {
            stats.RemoveAllFromSource(debugSource);
            Debug.Log("Removed all test modifiers.");
            Log(StatType.MovementSpeed);
            Log(StatType.MaxLife);
        }
    }

    private void Log(StatType type)
    {
        Debug.Log($"{type}: {stats.GetValue(type):0.##}");
    }
}
