using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Placeholder visual effects: a shape that quickly grows, then shrinks away and deletes itself.
/// Real particles will replace these later; the skill code won't need to change.
/// </summary>
public class SkillEffect : MonoBehaviour
{
    private static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();

    private Vector3 startScale;
    private Vector3 endScale;
    private float duration;
    private float age;

    /// <summary>A quick ball-shaped pop, e.g. a melee impact or a projectile hitting.</summary>
    public static void Burst(Vector3 position, Color color, float size, float duration)
    {
        Create(PrimitiveType.Sphere, position, Vector3.one * size * 0.3f, Vector3.one * size, color, duration);
    }

    /// <summary>A flat expanding disc on the ground, showing an area of effect of the given radius.</summary>
    public static void Ring(Vector3 groundPosition, float radius, Color color, float duration)
    {
        float diameter = radius * 2f; // a Unity cylinder is 1 unit wide at scale 1
        Create(PrimitiveType.Cylinder, groundPosition + Vector3.up * 0.05f,
               new Vector3(diameter * 0.3f, 0.02f, diameter * 0.3f),
               new Vector3(diameter, 0.02f, diameter), color, duration);
    }

    private static void Create(PrimitiveType shape, Vector3 position, Vector3 from, Vector3 to, Color color, float duration)
    {
        GameObject go = GameObject.CreatePrimitive(shape);
        go.name = "SkillEffect";
        go.transform.position = position;
        go.transform.localScale = from;
        Destroy(go.GetComponent<Collider>());

        Renderer renderer = go.GetComponent<Renderer>();
        if (!materials.TryGetValue(color, out Material material) || material == null)
        {
            material = new Material(renderer.sharedMaterial) { name = "PlaceholderEffect", color = color };
            materials[color] = material;
        }
        renderer.sharedMaterial = material;

        SkillEffect effect = go.AddComponent<SkillEffect>();
        effect.startScale = from;
        effect.endScale = to;
        effect.duration = Mathf.Max(0.05f, duration);
    }

    private void Update()
    {
        age += Time.deltaTime;
        float t = age / duration;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        // Grow quickly for the first 70%, then shrink away.
        Vector3 scale = t < 0.7f
            ? Vector3.Lerp(startScale, endScale, 1f - Mathf.Pow(1f - t / 0.7f, 3f))
            : Vector3.Lerp(endScale, new Vector3(0f, endScale.y, 0f), (t - 0.7f) / 0.3f);
        transform.localScale = scale;
    }
}
