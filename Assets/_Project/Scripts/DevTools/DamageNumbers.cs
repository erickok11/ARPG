using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Temporary floating damage numbers. Creates itself automatically when you press Play,
/// so you don't need to add it to anything.
///   White  = physical hit on a monster
///   Yellow = physical critical hit
///   Orange = fire (crits are bigger and end in "!")
///   Red    = damage taken by the player
/// </summary>
public class DamageNumbers : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.8f;
    [SerializeField] private float riseSpeed = 1.5f;
    [SerializeField] private int fontSize = 22;
    [SerializeField] private int critFontSize = 32;

    private class Entry
    {
        public Vector3 worldPosition;
        public string text;
        public Color color;
        public bool isCrit;
        public float age;
    }

    private static DamageNumbers instance;
    private readonly List<Entry> entries = new List<Entry>();
    private GUIStyle style;
    private Camera cam;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (FindFirstObjectByType<DamageNumbers>() != null) return;
        new GameObject("DamageNumbers").AddComponent<DamageNumbers>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void OnEnable() => Combat.AnyHit += OnHit;
    private void OnDisable() => Combat.AnyHit -= OnHit;

    private void OnHit(GameObject attacker, Health target, DamageResult result)
    {
        bool targetIsPlayer = target.GetComponent<PlayerController>() != null;
        Color color = targetIsPlayer ? new Color(1f, 0.35f, 0.35f) : ColorFor(result);

        Vector2 jitter = Random.insideUnitCircle * 0.3f;
        entries.Add(new Entry
        {
            worldPosition = target.transform.position + new Vector3(jitter.x, 1.5f, jitter.y),
            text = result.IsCrit ? $"{result.Amount:0}!" : $"{result.Amount:0}",
            color = color,
            isCrit = result.IsCrit,
        });
    }

    /// <summary>White = physical, yellow = physical crit, orange = fire, light blue = cold, etc.</summary>
    private static Color ColorFor(DamageResult result) => result.Type switch
    {
        DamageType.Fire => new Color(1f, 0.55f, 0.2f),
        DamageType.Cold => new Color(0.55f, 0.8f, 1f),
        DamageType.Lightning => new Color(1f, 1f, 0.45f),
        DamageType.Chaos => new Color(0.8f, 0.45f, 1f),
        _ => result.IsCrit ? new Color(1f, 0.85f, 0.2f) : Color.white,
    };

    private void Update()
    {
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            entries[i].age += Time.deltaTime;
            if (entries[i].age >= lifetime) entries.RemoveAt(i);
        }
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || entries.Count == 0) return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        if (style == null)
            style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

        foreach (Entry e in entries)
        {
            Vector3 world = e.worldPosition + Vector3.up * e.age * riseSpeed;
            Vector3 screen = cam.WorldToScreenPoint(world);
            if (screen.z < 0f) continue;

            float t = e.age / lifetime;
            float alpha = 1f - t * t;

            style.fontSize = e.isCrit ? critFontSize : fontSize;
            var content = new GUIContent(e.text);
            Vector2 size = style.CalcSize(content);
            var rect = new Rect(screen.x - size.x / 2f, Screen.height - screen.y - size.y / 2f, size.x, size.y);

            style.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.8f);
            GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), content, style);

            style.normal.textColor = new Color(e.color.r, e.color.g, e.color.b, alpha);
            GUI.Label(rect, content, style);
        }
    }
}
