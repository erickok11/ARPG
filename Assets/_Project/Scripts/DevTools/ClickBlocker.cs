using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps track of open on-screen windows so clicks on them don't also move or attack.
/// Windows call Set every frame while open and Clear when they close.
/// </summary>
public static class ClickBlocker
{
    private static readonly Dictionary<object, Rect> rects = new Dictionary<object, Rect>();

    public static void Set(object owner, Rect guiRect) => rects[owner] = guiRect;

    public static void Clear(object owner) => rects.Remove(owner);

    /// <summary>True if a GUI-space point (origin top-left) is over any open window.</summary>
    public static bool IsPointerOver(Vector2 guiPosition)
    {
        foreach (Rect r in rects.Values)
            if (r.Contains(guiPosition)) return true;
        return false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => rects.Clear();
}
