using System;
using UnityEngine;

/// <summary>
/// Current life for a character. Max life and regen come from CharacterStats.
/// </summary>
[RequireComponent(typeof(CharacterStats))]
public class Health : MonoBehaviour
{
    public event Action<float> Damaged;   // amount taken
    public event Action Died;
    public event Action Changed;          // any change to current or max (for UI)

    private CharacterStats stats;
    private float current;
    private float lastMax;

    public float Current => current;
    public float Max => stats.GetValue(StatType.MaxLife);
    public float Fraction => Max > 0f ? current / Max : 0f;
    public bool IsDead { get; private set; }

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
    }

    private void OnEnable() => stats.StatChanged += OnStatChanged;
    private void OnDisable() => stats.StatChanged -= OnStatChanged;

    private void Start()
    {
        lastMax = Max;
        current = lastMax;
        Changed?.Invoke();
    }

    private void Update()
    {
        if (IsDead) return;

        float regen = stats.GetValue(StatType.LifeRegen);
        if (regen > 0f && current < Max)
        {
            current = Mathf.Min(Max, current + regen * Time.deltaTime);
            Changed?.Invoke();
        }
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        current = Mathf.Max(0f, current - amount);
        Damaged?.Invoke(amount);
        Changed?.Invoke();

        if (current <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        current = Mathf.Min(Max, current + amount);
        Changed?.Invoke();
    }

    private void Die()
    {
        IsDead = true;
        Died?.Invoke();
    }

    // Keep the same percentage of life when max life changes (e.g. equipping +life gear).
    private void OnStatChanged(StatType type)
    {
        if (type != StatType.MaxLife) return;

        float newMax = Max;
        current = lastMax > 0f ? current / lastMax * newMax : newMax;
        lastMax = newMax;
        Changed?.Invoke();
    }

    [ContextMenu("Debug: Take 20 Damage")]
    private void DebugTakeDamage() => TakeDamage(20f);
}
