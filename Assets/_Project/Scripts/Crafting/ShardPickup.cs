using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A crafting shard on the ground. Bobs and spins with a name label, flies to the player
/// when they get close, and goes into their CurrencyPouch. No clicking needed, like gold.
/// </summary>
public class ShardPickup : MonoBehaviour
{
    private static readonly Dictionary<ShardType, Material> materials = new Dictionary<ShardType, Material>();

    [SerializeField] private float pickupRadius = 0.8f;
    [Tooltip("Shard starts flying toward the player inside this distance.")]
    [SerializeField] private float magnetRadius = 3f;
    [SerializeField] private float magnetSpeed = 12f;
    [SerializeField] private float spinSpeed = 120f;
    [SerializeField] private float bobHeight = 0.08f;
    [SerializeField] private float labelHeight = 0.5f;
    [SerializeField] private int fontSize = 13;

    private CurrencyPouch pouch;
    private bool magnetized;
    private Vector3 restPosition;
    private GUIStyle style;
    private Camera cam;

    public ShardType Type { get; private set; }

    /// <summary>Drops a shard into the world as a small spinning gem.</summary>
    public static ShardPickup Spawn(ShardType type, Vector3 position)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = ShardInfo.Name(type);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(45f, 0f, 45f); // stand the cube on its corner like a gem
        go.transform.localScale = Vector3.one * 0.2f;

        // No collider, so shards never block clicks or movement.
        Destroy(go.GetComponent<Collider>());

        Renderer renderer = go.GetComponent<Renderer>();
        if (!materials.TryGetValue(type, out Material material) || material == null)
        {
            material = new Material(renderer.sharedMaterial)
            {
                name = $"Placeholder{type}Shard",
                color = ShardInfo.GetColor(type),
            };
            materials[type] = material;
        }
        renderer.sharedMaterial = material;

        ShardPickup pickup = go.AddComponent<ShardPickup>();
        pickup.Type = type;
        return pickup;
    }

    private void Start()
    {
        pouch = FindFirstObjectByType<CurrencyPouch>();
        restPosition = transform.position;
    }

    private void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        if (pouch == null) return;

        Vector3 target = pouch.transform.position;
        float distance = Vector3.Distance(transform.position, target);

        if (distance <= magnetRadius) magnetized = true;

        if (magnetized)
            transform.position = Vector3.MoveTowards(transform.position, target, magnetSpeed * Time.deltaTime);
        else
            transform.position = restPosition + Vector3.up * (Mathf.Sin(Time.time * 3f) * bobHeight);

        if (distance <= pickupRadius)
        {
            pouch.Add(Type, 1);
            Destroy(gameObject);
        }
    }

    private void OnGUI()
    {
        if (magnetized || Event.current.type != EventType.Repaint) return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 screen = cam.WorldToScreenPoint(restPosition + Vector3.up * labelHeight);
        if (screen.z < 0f) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
            };
        }

        var content = new GUIContent(ShardInfo.Name(Type));
        Vector2 size = style.CalcSize(content);
        size.x += 12f;
        size.y += 2f;
        var rect = new Rect(screen.x - size.x / 2f, Screen.height - screen.y - size.y, size.x, size.y);

        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;

        style.normal.textColor = ShardInfo.GetColor(Type);
        GUI.Label(rect, content, style);
    }
}
