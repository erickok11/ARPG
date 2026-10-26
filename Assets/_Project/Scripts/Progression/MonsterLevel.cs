using UnityEngine;

/// <summary>
/// A monster's level. Each level after 1 gives it more life and damage, and it's worth more
/// experience when killed. Also sets the item level of its drops (LootDropper).
/// EnemySpawner sets the level of every monster it spawns.
/// </summary>
[RequireComponent(typeof(CharacterStats), typeof(Health))]
public class MonsterLevel : MonoBehaviour
{
    [SerializeField, Min(1)] private int level = 1;
    [Tooltip("Percent more life per level after 1.")]
    [SerializeField] private float lifePerLevel = 15f;
    [Tooltip("Percent more damage per level after 1.")]
    [SerializeField] private float damagePerLevel = 8f;

    private CharacterStats stats;
    private Health health;
    private LootDropper loot;

    public int Level => level;
    public int ExperienceReward => Leveling.MonsterExperience(level);

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        health = GetComponent<Health>();
        loot = GetComponent<LootDropper>();
        Apply();
    }

    private void OnEnable() => health.Died += GiveExperience;
    private void OnDisable() => health.Died -= GiveExperience;

    public void SetLevel(int newLevel)
    {
        level = Mathf.Max(1, newLevel);
        Apply();
    }

    private void Apply()
    {
        stats.RemoveAllFromSource(this);
        int levels = level - 1;
        if (levels > 0)
        {
            stats.AddModifier(StatType.MaxLife, new StatModifier(lifePerLevel * levels, ModifierType.More, this));
            stats.AddModifier(StatType.PhysicalDamage, new StatModifier(damagePerLevel * levels, ModifierType.More, this));
        }
        if (loot != null) loot.SetMonsterLevel(level);
    }

    private void GiveExperience()
    {
        CharacterLevel player = FindFirstObjectByType<CharacterLevel>();
        if (player != null) player.AddExperience(ExperienceReward);
    }
}
