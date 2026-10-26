using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Temporary inventory screen. Press I to open/close.
///   Click a backpack item to equip it.
///   Click an equipped item to move it back to the backpack.
///   Hover anything to see its tooltip.
/// </summary>
[RequireComponent(typeof(Inventory), typeof(Equipment))]
public class InventoryWindow : MonoBehaviour
{
    [SerializeField] private bool startOpen = false;
    [SerializeField] private int fontSize = 14;
    [SerializeField] private float width = 640f;
    [SerializeField] private float height = 480f;

    private static Rect? openRect;

    /// <summary>True if a GUI-space point is inside the open inventory window (so clicks there don't move you).</summary>
    public static bool IsPointerOverWindow(Vector2 guiPosition) =>
        openRect.HasValue && openRect.Value.Contains(guiPosition);

    private Inventory inventory;
    private Equipment equipment;
    private bool isOpen;
    private Vector2 scroll;
    private Item hovered;
    private GUIStyle rowStyle;
    private GUIStyle headerStyle;
    private readonly List<Item> snapshot = new List<Item>();

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
        equipment = GetComponent<Equipment>();
        isOpen = startOpen;
    }

    private void OnDisable() => openRect = null;

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.iKey.wasPressedThisFrame) isOpen = !isOpen;
        if (!isOpen) openRect = null;
    }

    private void OnGUI()
    {
        if (!isOpen)
        {
            openRect = null;
            return;
        }

        EnsureStyles();

        var window = new Rect(Screen.width - width - 10f, 10f, width, Mathf.Min(height, Screen.height - 20f));
        openRect = window;

        if (Event.current.type == EventType.Repaint) hovered = null;

        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.88f);
        GUI.DrawTexture(window, Texture2D.whiteTexture);
        GUI.color = previous;

        Item toEquip = null;
        EquipSlot? toUnequip = null;

        GUILayout.BeginArea(new Rect(window.x + 10f, window.y + 10f, window.width - 20f, window.height - 20f));
        GUILayout.Label("<b>Inventory</b>   <color=#888>I to close · click backpack items to equip · click equipped items to unequip</color>", headerStyle);
        GUILayout.Space(6f);
        GUILayout.BeginHorizontal();

        // Equipped column
        GUILayout.BeginVertical(GUILayout.Width(290f));
        GUILayout.Label("<b>Equipped</b>", headerStyle);
        foreach (EquipSlot slot in (EquipSlot[])Enum.GetValues(typeof(EquipSlot)))
        {
            Item item = equipment.Get(slot);
            string text = item == null
                ? $"<color=#666>{slot}: empty</color>"
                : $"<color=#999>{slot}:</color> <color={item.RarityHex}>{item.DisplayName}</color>";

            if (GUILayout.Button(text, rowStyle) && item != null) toUnequip = slot;
            TrackHover(item);
        }
        GUILayout.EndVertical();

        GUILayout.Space(10f);

        // Backpack column
        GUILayout.BeginVertical();
        GUILayout.Label($"<b>Backpack</b>  <color=#888>{inventory.Items.Count}/{inventory.Capacity}</color>", headerStyle);
        scroll = GUILayout.BeginScrollView(scroll);

        snapshot.Clear();
        snapshot.AddRange(inventory.Items);
        if (snapshot.Count == 0)
            GUILayout.Label("<color=#666>Empty. Kill monsters and click item labels on the ground to pick them up.</color>", headerStyle);

        foreach (Item item in snapshot)
        {
            if (GUILayout.Button($"<color={item.RarityHex}>{item.DisplayName}</color>", rowStyle)) toEquip = item;
            TrackHover(item);
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        if (toEquip != null) equipment.EquipFromInventory(toEquip);
        if (toUnequip.HasValue) equipment.Unequip(toUnequip.Value);

        if (hovered != null) ItemTooltip.Draw(hovered, Event.current.mousePosition);
    }

    private void TrackHover(Item item)
    {
        if (item != null && Event.current.type == EventType.Repaint &&
            GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
        {
            hovered = item;
        }
    }

    private void EnsureStyles()
    {
        if (rowStyle != null && rowStyle.fontSize == fontSize) return;

        rowStyle = new GUIStyle(GUI.skin.button)
        {
            richText = true,
            alignment = TextAnchor.MiddleLeft,
            fontSize = fontSize,
            padding = new RectOffset(8, 8, 4, 4),
        };
        headerStyle = new GUIStyle(GUI.skin.label)
        {
            richText = true,
            wordWrap = true,
            fontSize = fontSize,
        };
        headerStyle.normal.textColor = Color.white;
    }
}
