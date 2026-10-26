using UnityEngine;

/// <summary>
/// Flashes the character's color when it takes damage, turns it gray on death, and back when revived.
/// </summary>
[RequireComponent(typeof(Health))]
public class HitFlash : MonoBehaviour
{
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.08f;
    [SerializeField] private Color deadColor = new Color(0.25f, 0.25f, 0.25f);

    private Health health;
    private Renderer[] renderers;
    private Color[] originalColors;
    private float timer;

    private void Awake()
    {
        health = GetComponent<Health>();
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i].material.color;
    }

    private void OnEnable()
    {
        health.Damaged += OnDamaged;
        health.Died += OnDied;
        health.Revived += OnRevived;
    }

    private void OnDisable()
    {
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
        health.Revived -= OnRevived;
    }

    private void Update()
    {
        if (timer <= 0f) return;

        timer -= Time.deltaTime;
        if (timer <= 0f && !health.IsDead)
            RestoreColors();
    }

    private void OnDamaged(float amount)
    {
        timer = flashDuration;
        SetColor(flashColor);
    }

    private void OnDied()
    {
        timer = 0f;
        SetColor(deadColor);
    }

    private void OnRevived()
    {
        timer = 0f;
        RestoreColors();
    }

    private void SetColor(Color color)
    {
        foreach (Renderer r in renderers)
            if (r != null) r.material.color = color;
    }

    private void RestoreColors()
    {
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].material.color = originalColors[i];
    }
}
