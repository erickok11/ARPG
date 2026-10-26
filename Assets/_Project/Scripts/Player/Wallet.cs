using System;
using UnityEngine;

/// <summary>The player's gold.</summary>
public class Wallet : MonoBehaviour
{
    [SerializeField] private int gold;

    public int Gold => gold;

    /// <summary>Fires with the new total whenever gold changes.</summary>
    public event Action<int> Changed;

    public void Add(int amount)
    {
        if (amount <= 0) return;
        gold += amount;
        Changed?.Invoke(gold);
    }

    /// <summary>Spends gold if there's enough. Returns false (and spends nothing) otherwise.</summary>
    public bool TrySpend(int amount)
    {
        if (amount < 0 || gold < amount) return false;
        gold -= amount;
        Changed?.Invoke(gold);
        return true;
    }
}
