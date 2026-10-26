using System;
using UnityEngine;

/// <summary>
/// The player's level and experience. Killing monsters gives experience (see MonsterLevel),
/// and the same amount goes to the gems on your skill bar. Each level adds life, mana and attributes,
/// and fully restores life and mana.
/// </summary>
[RequireComponent(typeof(CharacterStats))]
public class CharacterLevel : MonoBehaviour
{
    [Header("Bonuses per level after 1")]
    [SerializeField] private float lifePerLevel = 12f;
    [SerializeField] private float manaPerLevel = 6f;
    [Tooltip("Added to each of Strength, Dexterity and Intelligence.")]
    [SerializeField] private float attributesPerLevel = 2f;

    [Header("Death")]
    [Tooltip("Percent of the experience needed for the next level that you lose when you die. You never lose a level.")]
    [SerializeField, Range(0f, 100f)] private float deathPenaltyPercent = 10f;

    /// <summary>Fires with the new level.</summary>
    public event Action<int> LeveledUp;

    private CharacterStats stats;
    private Health health;
    private Mana mana;
    private SkillBar bar;

    public int Level { get; private set; } = 1;
    /// <summary>Experience towards the next level.</summary>
    public int Experience { get; private set; }
    public int ExperienceToNext => Leveling.ExperienceToNext(Level);
    public bool IsMaxLevel => Level >= Leveling.MaxCharacterLevel;
    public float Progress => IsMaxLevel ? 1f : (float)Experience / ExperienceToNext;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        health = GetComponent<Health>();
        mana = GetComponent<Mana>();
        bar = GetComponent<SkillBar>();
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0) return;
        if (bar != null) bar.GrantExperience(amount);
        if (IsMaxLevel) return;

        int startLevel = Level;
        Experience += amount;
        while (!IsMaxLevel && Experience >= ExperienceToNext)
        {
            Experience -= ExperienceToNext;
            Level++;
        }
        if (IsMaxLevel) Experience = 0;
        if (Level == startLevel) return;

        ApplyLevelBonuses();
        if (health != null) health.Heal(health.Max);
        if (mana != null) mana.Restore(mana.Max);
        Debug.Log($"Level up! You are now level {Level}.");
        LeveledUp?.Invoke(Level);
    }

    /// <summary>Takes away part of a level's experience. Returns how much was lost.</summary>
    public int ApplyDeathPenalty()
    {
        int lost = Mathf.Min(Experience, Mathf.RoundToInt(ExperienceToNext * deathPenaltyPercent / 100f));
        Experience -= lost;
        return lost;
    }

    private void ApplyLevelBonuses()
    {
        stats.RemoveAllFromSource(this);
        int levels = Level - 1;
        if (levels <= 0) return;

        stats.AddModifier(StatType.MaxLife, new StatModifier(lifePerLevel * levels, ModifierType.Flat, this));
        stats.AddModifier(StatType.MaxMana, new StatModifier(manaPerLevel * levels, ModifierType.Flat, this));
        stats.AddModifier(StatType.Strength, new StatModifier(attributesPerLevel * levels, ModifierType.Flat, this));
        stats.AddModifier(StatType.Dexterity, new StatModifier(attributesPerLevel * levels, ModifierType.Flat, this));
        stats.AddModifier(StatType.Intelligence, new StatModifier(attributesPerLevel * levels, ModifierType.Flat, this));
    }

    [ContextMenu("Debug: Gain One Level")]
    public void DebugGainLevel() => AddExperience(ExperienceToNext - Experience);
}
