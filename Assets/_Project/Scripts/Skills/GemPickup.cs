using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A skill gem on the ground. Bobs with a name label, flies to the player when they get close,
/// and goes onto their SkillBar (straight onto an empty Q/W/E/R slot if there is one).
/// Color follows the main attribute: red = Strength, green = Dexterity, blue = Intelligence.
/// </summary>
public class GemPickup : MonoBehaviour
{
    private static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();

    [SerializeField] private float pickupRadius = 0.8f;
    [SerializeField] private float magnetRadius = 3f;
    [SerializeField] private float magnetSpeed = 12f;
    [SerializeField] private float spinSpeed = 90f;
    [SerializeField] private float bobHeight = 0.08f;
    [SerializeField] private float labelHeight = 0.55f;
    [SerializeField] private int fontSize = 13;

    private SkillBar bar;
    private bool magnetized;
    private Vector3 restPosition;
    private GUIStyle style;
    private Camera cam;
    private Color gemColor;

    public SkillGem Gem { get; private set; }

    public static GemPickup Spawn(SkillGem gem, Vector3 position)
    {
        SkillData data = gem?.Data;
        if (data == null) return null;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = $"Gem: {data.name}";
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.22f, 0.3f, 0.22f);
        Destroy(go.GetComponent<Collider>());

        Color color = SkillDatabase.GemColor(data);
        Renderer renderer = go.GetComponent<Renderer>();
        if (!materials.TryGetValue(color, out Material material) || material == null)
        {
            material = new Material(renderer.sharedMaterial) { name = "PlaceholderGem", color = color };
            materials[color] = material;
        }
        renderer.sharedMaterial = material;

        GemPickup pickup = go.AddComponent<GemPickup>();
        pickup.Gem = gem;
        pickup.gemColor = color;
        return pickup;
    }

    private void Start()
    {
        bar = FindFirstObjectByType<SkillBar>();
        restPosition = transform.position;
    }

    private void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        if (bar == null) return;

        Vector3 target = bar.transform.position;
        float distance = Vector3.Distance(transform.position, target);

        if (distance <= magnetRadius) magnetized = true;

        if (magnetized)
            transform.position = Vector3.MoveTowards(transform.position, target, magnetSpeed * Time.deltaTime);
        else
            transform.position = restPosition + Vector3.up * (Mathf.Sin(Time.time * 3f) * bobHeight);

        if (distance <= pickupRadius)
        {
            bar.AddGem(Gem);
            Destroy(gameObject);
        }
    }

    private void OnGUI()
    {
        if (magnetized || Gem == null || Event.current.type != EventType.Repaint) return;

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

        var content = new GUIContent($"{Gem.Name} Gem");
        Vector2 size = style.CalcSize(content);
        size.x += 12f;
        size.y += 2f;
        var rect = new Rect(screen.x - size.x / 2f, Screen.height - screen.y - size.y, size.x, size.y);

        Color previous = GUI.color;
        GUI.color = gemColor;
        GUI.DrawTexture(new Rect(rect.x - 1f, rect.y - 1f, rect.width + 2f, rect.height + 2f), Texture2D.whiteTexture);
        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;

        style.normal.textColor = gemColor;
        GUI.Label(rect, content, style);
    }
}
