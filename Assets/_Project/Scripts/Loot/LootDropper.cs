using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Drops gold when this character dies. Items will be added here later.
/// </summary>
[RequireComponent(typeof(Health))]
public class LootDropper : MonoBehaviour
{
    [Header("Gold")]
    [SerializeField, Range(0f, 1f)] private float goldDropChance = 0.8f;
    [SerializeField] private int minGold = 3;
    [SerializeField] private int maxGold = 10;
    [Tooltip("Optional. Leave empty to use a placeholder gold coin.")]
    [SerializeField] private GameObject goldPrefab;

    private static Material placeholderCoinMaterial;
    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable() => health.Died += DropLoot;
    private void OnDisable() => health.Died -= DropLoot;

    private void DropLoot()
    {
        if (Random.value > goldDropChance) return;

        Vector2 scatter = Random.insideUnitCircle * 0.6f;
        Vector3 position = transform.position + new Vector3(scatter.x, 0f, scatter.y);

        // Put the coin on the floor rather than at the monster's waist.
        if (NavMesh.SamplePosition(position, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
            position = navHit.position;
        position += Vector3.up * 0.3f;

        GameObject coin = goldPrefab != null
            ? Instantiate(goldPrefab, position, Quaternion.identity)
            : CreatePlaceholderCoin(position);

        GoldPickup pickup = coin.GetComponent<GoldPickup>();
        if (pickup == null) pickup = coin.AddComponent<GoldPickup>();
        pickup.SetAmount(Random.Range(minGold, maxGold + 1));
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
