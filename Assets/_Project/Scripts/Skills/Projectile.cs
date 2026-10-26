using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A flying projectile (Fireball, Scatter Blades). Moves in a straight line, hits the first
/// target it touches (or passes through a few with Pierce), and stops at walls or after its max distance.
/// </summary>
public class Projectile : MonoBehaviour
{
    private static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();

    private GameObject owner;
    private DamageResult[] damage;
    private int piercesLeft;
    private readonly HashSet<Health> alreadyHit = new HashSet<Health>();
    private float speed;
    private float maxDistance;
    private float radius;
    private Color color;
    private LayerMask targetMask;
    private LayerMask obstacleMask;
    private float travelled;

    public static Projectile Spawn(GameObject owner, Vector3 position, Vector3 direction, float speed,
                                   float maxDistance, float radius, Color color, DamageResult[] damage,
                                   int pierceCount, LayerMask targetMask, LayerMask obstacleMask)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Projectile";
        go.transform.position = position;
        go.transform.rotation = Quaternion.LookRotation(direction);
        float size = Mathf.Max(0.15f, radius * 1.1f);
        go.transform.localScale = new Vector3(size, size, size * 1.6f); // stretched along its flight path

        // No collider: hits are found with overlap checks, so projectiles never push anything around.
        Destroy(go.GetComponent<Collider>());

        Renderer renderer = go.GetComponent<Renderer>();
        if (!materials.TryGetValue(color, out Material material) || material == null)
        {
            material = new Material(renderer.sharedMaterial) { name = "PlaceholderProjectile", color = color };
            materials[color] = material;
        }
        renderer.sharedMaterial = material;

        Projectile p = go.AddComponent<Projectile>();
        p.owner = owner;
        p.damage = damage;
        p.piercesLeft = pierceCount;
        p.speed = speed;
        p.maxDistance = maxDistance;
        p.radius = radius;
        p.color = color;
        p.targetMask = targetMask;
        p.obstacleMask = obstacleMask;
        return p;
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;
        Vector3 direction = transform.forward;

        if (Physics.Raycast(transform.position, direction, out RaycastHit wall, step, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            Explode(wall.point);
            return;
        }

        transform.position += direction * step;
        travelled += step;

        foreach (Collider c in Physics.OverlapSphere(transform.position, radius, targetMask))
        {
            Health target = c.GetComponentInParent<Health>();
            if (target == null || target.IsDead || target.gameObject == owner || !alreadyHit.Add(target)) continue;

            Combat.Hit(owner, target, damage);
            if (piercesLeft > 0)
            {
                piercesLeft--;
                SkillEffect.Burst(transform.position, color, Mathf.Max(0.4f, radius * 1.5f), 0.1f);
                continue;
            }

            Explode(transform.position);
            return;
        }

        if (travelled >= maxDistance) Destroy(gameObject);
    }

    private void Explode(Vector3 position)
    {
        SkillEffect.Burst(position, color, Mathf.Max(0.6f, radius * 2.5f), 0.15f);
        Destroy(gameObject);
    }
}
