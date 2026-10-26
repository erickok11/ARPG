using System.Collections.Generic;
using UnityEngine;

/// <summary>A full-screen-ish window (inventory, crafting, skills) that can be opened and closed.</summary>
public interface IGameWindow
{
    void SetOpen(bool open);
}

/// <summary>
/// Keeps only one window open at a time: opening one closes the others.
/// Windows register themselves in OnEnable and unregister in OnDisable.
/// </summary>
public static class GameWindows
{
    private static readonly List<IGameWindow> windows = new List<IGameWindow>();

    public static void Register(IGameWindow window)
    {
        if (window != null && !windows.Contains(window)) windows.Add(window);
    }

    public static void Unregister(IGameWindow window) => windows.Remove(window);

    public static void CloseOthers(IGameWindow except)
    {
        foreach (IGameWindow w in windows.ToArray())
            if (w != except) w.SetOpen(false);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => windows.Clear();
}
