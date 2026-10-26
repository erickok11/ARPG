using System;
using UnityEngine;

/// <summary>
/// Current mana for a character. Max mana and regen come from CharacterStats.
/// </summary>
[RequireComponent(typeof(CharacterStats))]
public class Mana : MonoBehaviour
{
    public event Action Changed;

    private CharacterStats stats;
    private float current;
    private float lastMax;

    public float Current => current;
    public float Max => stats.GetValue(StatType.MaxMana);
    public float Fraction => Max > 0f ? current / Max : 0f;

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
        float regen = stats.GetValue(StatType.ManaRegen);
        if (regen > 0f && current < Max)
        {
            current = Mathf.Min(Max, current + regen * Time.deltaTime);
            Changed?.Invoke();
        }
    }

    public bool Has(float amount) => current + 0.0001f >= amount;

    /// <summary>Spends mana if there's enough. Returns false (and spends nothing) otherwise.</summary>
    public bool TrySpend(float amount)
    {
        if (amount <= 0f) return true;
        if (!Has(amount)) return false;
        current = Mathf.Max(0f, current - amount);
        Changed?.Invoke();
        return true;
    }

    public void Restore(float amount)
    {
        if (amount <= 0f) return;
        current = Mathf.Min(Max, current + amount);
        Changed?.Invoke();
    }

    // Keep the same percentage of mana when max mana changes (e.g. equipping +mana gear).
    private void OnStatChanged(StatType type)
    {
        if (type != StatType.MaxMana) return;

        float newMax = Max;
        current = lastMax > 0f ? current / lastMax * newMax : newMax;
        lastMax = newMax;
        Changed?.Invoke();
    }
}
