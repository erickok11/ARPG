using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Drops gold, crafting shards, skill and support gems, and random items when this character dies.
/// </summary>
[RequireComponent(typeof(Health))]
public class LootDropper : MonoBehaviour
{
    [Header("Monster")]
    [Tooltip("Item level of drops. Higher levels unlock stronger affix tiers (try 60 to see top tiers). " +
             "A MonsterLevel component on the same object overrides this.")]
    [SerializeField, Min(1)] private int monsterLevel = 1;

    [Header("Gold")]
    [SerializeField, Range(0f, 1f)] private float goldDropChance = 0.8f;
    [SerializeField] private int minGold = 3;
    [SerializeField] private int maxGold = 10;
    [Tooltip("Optional. Leave empty to use a placeholder gold coin.")]
    [SerializeField] private GameObject goldPrefab;

    [Header("Items")]
    [Tooltip("Chance to drop a random item. Set to 1 while testing.")]
    [SerializeField, Range(0f, 1f)] private float itemDropChance = 0.35f;

    [Header("Shards (each rolls separately)")]
    [SerializeField, Range(0f, 1f)] private float augmentDropChance = 0.25f;
    [SerializeField, Range(0f, 1f)] private float reforgeDropChance = 0.15f;
    [SerializeField, Range(0f, 1f)] private float rerollDropChance = 0.1f;

    [Header("Skill gems")]
    [Tooltip("Chance to drop a random skill gem.")]
    [SerializeField, Range(0f, 1f)] private float gemDropChance = 0.08f;
    [Tooltip("Chance to drop a random support gem.")]
    [SerializeField, Range(0f, 1f)] private float supportDropChance = 0.08f;

    private static Material placeholderCoinMaterial;
    private Health health;

    public int MonsterLevel => monsterLevel;

    public void SetMonsterLevel(int level) => monsterLevel = Mathf.Max(1, level);

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable() => health.Died += DropLoot;
    private void OnDisable() => health.Died -= DropLoot;

    private void DropLoot()
    {
        if (Random.value < goldDropChance) DropGold();
        if (Random.value < itemDropChance) DropItem();
        if (Random.value < augmentDropChance) DropShard(ShardType.Augment);
        if (Random.value < reforgeDropChance) DropShard(ShardType.Reforge);
        if (Random.value < rerollDropChance) DropShard(ShardType.Reroll);
        if (Random.value < gemDropChance) DropGem();
        if (Random.value < supportDropChance) DropSupport();
    }

    private void DropSupport()
    {
        var supports = SupportDatabase.All;
        if (supports.Count == 0) return;
        SupportData support = supports[Random.Range(0, supports.Count)];
        GemPickup.Spawn(new SupportGem(support.id), RandomFloorPosition(1f) + Vector3.up * 0.3f);
    }

    private void DropGem()
    {
        var skills = SkillDatabase.All;
        if (skills.Count == 0) return;
        SkillData skill = skills[Random.Range(0, skills.Count)];
        GemPickup.Spawn(new SkillGem(skill.id), RandomFloorPosition(1f) + Vector3.up * 0.4f);
    }

    private void DropGold()
    {
        Vector3 position = RandomFloorPosition(0.6f) + Vector3.up * 0.3f;

        GameObject coin = goldPrefab != null
            ? Instantiate(goldPrefab, position, Quaternion.identity)
            : CreatePlaceholderCoin(position);

        GoldPickup pickup = coin.GetComponent<GoldPickup>();
        if (pickup == null) pickup = coin.AddComponent<GoldPickup>();
        pickup.SetAmount(Random.Range(minGold, maxGold + 1));
    }

    private void DropItem()
    {
        Item item = ItemGenerator.GenerateRandom(monsterLevel);
        if (item == null) return;
        ItemPickup.Spawn(item, RandomFloorPosition(1f) + Vector3.up * 0.2f);
    }

    private void DropShard(ShardType type)
    {
        ShardPickup.Spawn(type, RandomFloorPosition(1f) + Vector3.up * 0.4f);
    }

    /// <summary>A random point on the floor near this character.</summary>
    private Vector3 RandomFloorPosition(float scatterRadius)
    {
        Vector2 scatter = Random.insideUnitCircle * scatterRadius;
        Vector3 position = transform.position + new Vector3(scatter.x, 0f, scatter.y);

        if (NavMesh.SamplePosition(position, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
            position = navHit.position;
        return position;
    }

    private static GameObject CreatePlaceholderCoin(Vector3 position)
    {
        GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coin.name = "Gold";
        coin.transform.position = position;
        coin.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // stand the coin on its edge
        coin.transform.localScale = new Vector3(0.35f, 0.04f, 0.35f);

        // No collider, so coins never block clicks or movement.
        Destroy(coin.GetComponent<Collider>());

        Renderer renderer = coin.GetComponent<Renderer>();
        if (placeholderCoinMaterial == null)
        {
            placeholderCoinMaterial = new Material(renderer.sharedMaterial)
            {
                name = "PlaceholderGold",
                color = new Color(1f, 0.8f, 0.1f),
            };
        }
        renderer.sharedMaterial = placeholderCoinMaterial;
        return coin;
    }
}
