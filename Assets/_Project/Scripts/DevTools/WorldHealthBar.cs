using UnityEngine;

/// <summary>
/// Temporary health bar drawn above a character's head. Will be replaced by real UI later.
/// </summary>
[RequireComponent(typeof(Health))]
public class WorldHealthBar : MonoBehaviour
{
    [Tooltip("How far above the character's pivot to draw the bar.")]
    [SerializeField] private float heightAboveOrigin = 1.4f;
    [SerializeField] private float width = 60f;
    [SerializeField] private float height = 7f;
    [SerializeField] private Color fillColor = new Color(0.85f, 0.15f, 0.15f);
    [Tooltip("Only show the bar once the character has taken damage.")]
    [SerializeField] private bool hideWhenFull = false;

    private Health health;
    private Camera cam;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;
        if (health.IsDead) return;
        if (hideWhenFull && health.Fraction >= 0.999f) return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * heightAboveOrigin);
        if (screen.z < 0f) return;

        float x = screen.x - width / 2f;
        float y = Screen.height - screen.y - height;

        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(new Rect(x - 1f, y - 1f, width + 2f, height + 2f), Texture2D.whiteTexture);
        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(x, y, width * health.Fraction, height), Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
