using UnityEngine;

public enum CharacterClass { Witch, Ranger, Warrior, Knight, Gladiator, Shade }

/// <summary>
/// Picks the player's class: sets starting Strength / Dexterity / Intelligence
/// and gives a starting skill gem on Q. Change the class in the Inspector and press Play.
/// </summary>
[RequireComponent(typeof(CharacterStats))]
public class StartingClass : MonoBehaviour
{
    [SerializeField] private CharacterClass characterClass = CharacterClass.Knight;
    [SerializeField] private bool giveStartingSkill = true;

    public CharacterClass Class => characterClass;

    private void Awake()
    {
        CharacterStats stats = GetComponent<CharacterStats>();
        (int str, int dex, int intel) = Attributes(characterClass);
        stats.Get(StatType.Strength).BaseValue = str;
        stats.Get(StatType.Dexterity).BaseValue = dex;
        stats.Get(StatType.Intelligence).BaseValue = intel;
    }

    private void Start()
    {
        if (!giveStartingSkill) return;
        SkillBar bar = GetComponent<SkillBar>();
        if (bar != null) bar.AddGem(new SkillGem(StartingSkill(characterClass)));
    }

    /// <summary>Starting attributes. Each class has 40 points, weighted toward its attributes.</summary>
    public static (int str, int dex, int intel) Attributes(CharacterClass c) => c switch
    {
        CharacterClass.Witch => (10, 10, 20),
        CharacterClass.Ranger => (10, 20, 10),
        CharacterClass.Warrior => (20, 10, 10),
        CharacterClass.Knight => (15, 10, 15),
        CharacterClass.Gladiator => (15, 15, 10),
        CharacterClass.Shade => (10, 15, 15),
        _ => (10, 10, 10),
    };

    public static string StartingSkill(CharacterClass c) => c switch
    {
        CharacterClass.Witch => "fireball",
        CharacterClass.Ranger => "scatter_blades",
        CharacterClass.Warrior => "heavy_strike",
        CharacterClass.Knight => "heavy_strike",
        CharacterClass.Gladiator => "ground_slam",
        CharacterClass.Shade => "scatter_blades",
        _ => "heavy_strike",
    };
}
