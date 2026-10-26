using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An item lying on the ground, with a PoE-style name label you click to pick it up.
/// Hover the label to see the item's tooltip.
/// </summary>
public class ItemPickup : MonoBehaviour
{
    private static readonly List<ItemPickup> all = new List<ItemPickup>();
    private static readonly Dictionary<ItemRarity, Material> rarityMaterials = new Dictionary<ItemRarity, Material>();

    [Tooltip("How far above the item to draw its label.")]
    [SerializeField] private float labelHeight = 0.6f;
    [SerializeField] private int fontSize = 14;

    private GUIStyle style;
    private Camera cam;

    public Item Item { get; private set; }

    /// <summary>Label position in GUI coordinates (origin top-left), from the last frame.</summary>
    public Rect LabelRect { get; private set; }
    public bool LabelVisible { get; private set; }

    /// <summary>Finds the ground item whose label is under a GUI-space point.</summary>
    public static bool TryGetLabelAt(Vector2 guiPosition, out ItemPickup pickup)
    {
        // Check newest first so the label drawn on top wins.
        for (int i = all.Count - 1; i >= 0; i--)
        {
            ItemPickup p = all[i];
            if (p != null && p.LabelVisible && p.LabelRect.Contains(guiPosition))
            {
                pickup = p;
                return true;
            }
        }
        pickup = null;
        return false;
    }

    /// <summary>Drops an item into the world as a small cube colored by rarity.</summary>
    public static ItemPickup Spawn(Item item, Vector3 position)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Item: {item.DisplayName}";
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        go.transform.localScale = Vector3.one * 0.35f;

        // No collider: items are picked up by clicking their label, and never block clicks or movement.
        Destroy(go.GetComponent<Collider>());

        Renderer renderer = go.GetComponent<Renderer>();
        if (!rarityMaterials.TryGetValue(item.Rarity, out Material material) || material == null)
        {
            material = new Material(renderer.sharedMaterial)
            {
                name = $"Placeholder{item.Rarity}Item",
                color = Item.RarityColor(item.Rarity),
            };
            rarityMaterials[item.Rarity] = material;
        }
        renderer.sharedMaterial = material;

        ItemPickup pickup = go.AddComponent<ItemPickup>();
        pickup.Item = item;
        return pickup;
    }

    private void OnEnable() => all.Add(this);
    private void OnDisable() => all.Remove(this);

    private void OnGUI()
    {
        if (Item == null) return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * labelHeight);
        if (screen.z < 0f)
        {
            LabelVisible = false;
            return;
        }

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

        var content = new GUIContent(Item.DisplayName);
        Vector2 size = style.CalcSize(content);
        size.x += 14f;
        size.y += 4f;

        var rect = new Rect(screen.x - size.x / 2f, Screen.height - screen.y - size.y, size.x, size.y);
        LabelRect = rect;
        LabelVisible = true;

        if (Event.current.type != EventType.Repaint) return;

        bool hovered = rect.Contains(Event.current.mousePosition);
        Color rarityColor = Item.RarityColor(Item.Rarity);
        Color previous = GUI.color;

        GUI.color = rarityColor;
        GUI.DrawTexture(new Rect(rect.x - 1f, rect.y - 1f, rect.width + 2f, rect.height + 2f), Texture2D.whiteTexture);
        GUI.color = hovered ? new Color(0.22f, 0.22f, 0.25f, 0.95f) : new Color(0f, 0f, 0f, 0.85f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;

        style.normal.textColor = rarityColor;
        GUI.Label(rect, content, style);

        if (hovered)
            ItemTooltip.Draw(Item, Event.current.mousePosition);
    }
}
