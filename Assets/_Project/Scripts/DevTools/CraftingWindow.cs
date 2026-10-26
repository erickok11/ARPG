using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Temporary crafting screen. Press C to open/close.
///   Left:  pick any equipped or backpack item.
///   Right: see its affixes, Reforge a specific affix, or use Augment / Reroll on the whole item.
/// Crafting an equipped item updates your stats immediately.
/// </summary>
[RequireComponent(typeof(Crafter), typeof(Inventory), typeof(Equipment))]
public class CraftingWindow : MonoBehaviour, IGameWindow
{
    [SerializeField] private bool startOpen = false;
    [SerializeField] private int fontSize = 14;
    [SerializeField] private float width = 820f;
    [SerializeField] private float height = 560f;
    [Tooltip("Shows a button that gives free shards and gold for testing.")]
    [SerializeField] private bool showDebugButton = true;

    private static readonly EquipSlot[] AllSlots = (EquipSlot[])Enum.GetValues(typeof(EquipSlot));

    private Crafter crafter;
    private Inventory inventory;
    private Equipment equipment;
    private Wallet wallet;
    private CurrencyPouch pouch;

    private bool isOpen;
    private Item selected;
    private Vector2 listScroll;
    private Vector2 detailScroll;
    private string status = "";
    private bool statusIsError;
    private readonly HashSet<RolledAffix> highlighted = new HashSet<RolledAffix>();
    private float highlightUntil;
    private Action pending;

    private GUIStyle labelStyle;
    private GUIStyle rowLabelStyle;
    private GUIStyle rowStyle;
    private GUIStyle actionStyle;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        crafter = GetComponent<Crafter>();
        inventory = GetComponent<Inventory>();
        equipment = GetComponent<Equipment>();
        wallet = GetComponent<Wallet>();
        pouch = GetComponent<CurrencyPouch>();
        isOpen = startOpen;
    }

    private void OnEnable() => GameWindows.Register(this);

    private void OnDisable()
    {
        GameWindows.Unregister(this);
        ClickBlocker.Clear(this);
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        if (!open) ClickBlocker.Clear(this);
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.cKey.wasPressedThisFrame)
        {
            SetOpen(!isOpen);
            if (isOpen) GameWindows.CloseOthers(this);
        }
        else if (isOpen && kb.escapeKey.wasPressedThisFrame)
        {
            SetOpen(false);
        }
    }

    private void OnGUI()
    {
        if (!isOpen) return;

        GUI.depth = -10; // draw on top of ground labels
        EnsureStyles();

        float w = Mathf.Min(width, Screen.width - 20f);
        float h = Mathf.Min(height, Screen.height - 20f);
        var window = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
        ClickBlocker.Set(this, window);
        DrawBox(window, new Color(0.03f, 0.03f, 0.05f, 0.95f));

        if (selected != null && !Owns(selected)) selected = null;
        pending = null;

        GUILayout.BeginArea(new Rect(window.x + 12f, window.y + 10f, window.width - 24f, window.height - 20f));

        GUILayout.BeginHorizontal();
        GUILayout.Label("<b>Crafting</b>   <color=#888>C to close</color>", rowLabelStyle);
        GUILayout.FlexibleSpace();
        if (showDebugButton &&
            GUILayout.Button("<color=#aaa>Debug: +10 shards, +500 gold</color>", rowStyle, GUILayout.Width(240f)))
        {
            pending = DebugGiveCurrency;
        }
        GUILayout.EndHorizontal();

        GUILayout.Label(CurrencyLine(), rowLabelStyle);
        GUILayout.Space(6f);

        GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
        DrawItemList();
        GUILayout.Space(12f);
        DrawDetails();
        GUILayout.EndHorizontal();

        GUILayout.Space(4f);
        GUILayout.Label(string.IsNullOrEmpty(status)
            ? " "
            : $"<color={(statusIsError ? "#f77" : "#7f7")}>{status}</color>", labelStyle);

        GUILayout.EndArea();

        // Run clicked actions after drawing, so lists don't change in the middle of the GUI pass.
        Action action = pending;
        pending = null;
        action?.Invoke();
    }

    // -----------------------------------------------------------------
    // Left column: item list
    // -----------------------------------------------------------------

    private void DrawItemList()
    {
        GUILayout.BeginVertical(GUILayout.Width(260f));
        listScroll = GUILayout.BeginScrollView(listScroll);

        GUILayout.Label("<b>Equipped</b>", rowLabelStyle);
        bool anyEquipped = false;
        foreach (EquipSlot slot in AllSlots)
        {
            Item item = equipment.Get(slot);
            if (item == null) continue;
            anyEquipped = true;
            ItemButton(item, $"<color=#888>{slot}</color>  ");
        }
        if (!anyEquipped) GUILayout.Label("<color=#666>Nothing equipped.</color>", rowLabelStyle);

        GUILayout.Space(8f);
        GUILayout.Label("<b>Backpack</b>", rowLabelStyle);
        if (inventory.Items.Count == 0) GUILayout.Label("<color=#666>Empty.</color>", rowLabelStyle);
        for (int i = 0; i < inventory.Items.Count; i++)
            ItemButton(inventory.Items[i], "");

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void ItemButton(Item item, string prefix)
    {
        Color previous = GUI.backgroundColor;
        if (item == selected) GUI.backgroundColor = new Color(0.45f, 0.75f, 1f);

        if (GUILayout.Button($"{prefix}<color={item.RarityHex}>{item.DisplayName}</color>", rowStyle))
        {
            Item clicked = item;
            pending = () =>
            {
                selected = clicked;
                status = "";
                highlighted.Clear();
                detailScroll = Vector2.zero;
            };
        }

        GUI.backgroundColor = previous;
    }

    // -----------------------------------------------------------------
    // Right column: selected item and crafting buttons
    // -----------------------------------------------------------------

    private void DrawDetails()
    {
        GUILayout.BeginVertical();

        Item item = selected;
        ItemBaseData b = item?.Base;
        if (b == null)
        {
            GUILayout.Label("<color=#888>Select an item on the left to craft on it.</color>", labelStyle);
            GUILayout.Space(10f);
            foreach (ShardType type in ShardInfo.All)
                GUILayout.Label($"<b><color={ShardInfo.Hex(type)}>{ShardInfo.Name(type)}</color></b>  <color=#aaa>{ShardInfo.Description(type)}</color>", labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            return;
        }

        detailScroll = GUILayout.BeginScrollView(detailScroll);

        GUILayout.Label($"<size={fontSize + 4}><b><color={item.RarityHex}>{item.DisplayName}</color></b></size>", rowLabelStyle);
        if (item.Rarity == ItemRarity.Rare)
            GUILayout.Label($"<color={item.RarityHex}>{b.name}</color>", rowLabelStyle);
        GUILayout.Label(
            $"<color=#888>{item.Rarity} {b.itemSlot}  ·  Item Level {item.itemLevel}  ·  " +
            $"Prefixes {item.PrefixCount}/{Item.MaxPrefixes}  ·  Suffixes {item.SuffixCount}/{Item.MaxSuffixes}</color>",
            labelStyle);

        foreach (ModifierData m in b.implicits)
            GUILayout.Label($"<color=#ccc>{StatText.Format(m.statType, m.modType, m.value)}</color>", rowLabelStyle);

        GUILayout.Space(8f);
        if (item.affixes.Count == 0)
        {
            GUILayout.Label("<color=#888>No affixes yet. Use an Augment Shard to add one.</color>", labelStyle);
        }
        else
        {
            DrawAffixRows(item, AffixGroup.Prefix);
            DrawAffixRows(item, AffixGroup.Suffix);
        }

        GUILayout.EndScrollView();

        GUILayout.Space(6f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(ActionLabel(ShardType.Augment, item), actionStyle, GUILayout.Height(46f)))
            pending = () => Apply(crafter.Augment(item));
        if (GUILayout.Button(ActionLabel(ShardType.Reroll, item), actionStyle, GUILayout.Height(46f)))
            pending = () => Apply(crafter.Reroll(item));
        GUILayout.EndHorizontal();
        GUILayout.Label("<color=#777>To use a Reforge Shard, click Reforge next to the affix you want to change.</color>", labelStyle);

        GUILayout.EndVertical();
    }

    private void DrawAffixRows(Item item, AffixGroup group)
    {
        bool glowing = Time.unscaledTime < highlightUntil;

        for (int i = 0; i < item.affixes.Count; i++)
        {
            RolledAffix a = item.affixes[i];
            AffixData d = a.Data;
            AffixTierData t = a.Tier;
            if (d == null || d.affixGroup != group) continue;

            string color = glowing && highlighted.Contains(a) ? "#7f7" : "#8af";
            string groupName = group == AffixGroup.Prefix ? "Prefix" : "Suffix";
            string tierText = t != null ? $"T{a.tierIndex + 1}/{d.tiers.Length}  ({t.min}-{t.max})" : "";

            GUILayout.BeginHorizontal();
            GUILayout.Label($"<color=#666>{groupName}</color>   <color={color}>{a.Describe()}</color>   <color=#555>{tierText}</color>", rowLabelStyle);
            GUILayout.FlexibleSpace();

            int index = i;
            if (GUILayout.Button(ReforgeLabel(item), rowStyle, GUILayout.Width(150f)))
                pending = () => Apply(crafter.Reforge(item, index));
            GUILayout.EndHorizontal();
        }
    }

    // -----------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------

    private void Apply(CraftResult result)
    {
        status = result.Message;
        statusIsError = !result.Success;
        if (!result.Success) return;

        highlighted.Clear();
        foreach (RolledAffix a in result.Changed) highlighted.Add(a);
        highlightUntil = Time.unscaledTime + 2.5f;
    }

    private string ActionLabel(ShardType type, Item item)
    {
        int cost = crafter.GoldCost(type, item);
        int have = pouch.Get(type);
        string haveColor = have > 0 ? "#ccc" : "#f66";
        string costColor = wallet.Gold >= cost ? "#fc3" : "#f66";
        return $"<b><color={ShardInfo.Hex(type)}>{ShardInfo.Name(type)}</color></b>\n" +
               $"<color={haveColor}>have {have}</color>   <color={costColor}>{cost} gold</color>";
    }

    private string ReforgeLabel(Item item)
    {
        int cost = crafter.GoldCost(ShardType.Reforge, item);
        string costColor = wallet.Gold >= cost && pouch.Get(ShardType.Reforge) > 0 ? "#fc3" : "#f66";
        return $"<color={ShardInfo.Hex(ShardType.Reforge)}>Reforge</color>  <color={costColor}>{cost}g</color>";
    }

    private string CurrencyLine()
    {
        string line = $"<b><color=#fc3>Gold {wallet.Gold}</color></b>";
        foreach (ShardType type in ShardInfo.All)
            line += $"      <color={ShardInfo.Hex(type)}>{ShardInfo.Name(type)}: {pouch.Get(type)}</color>";
        return line;
    }

    private bool Owns(Item item)
    {
        if (inventory.Contains(item)) return true;
        foreach (EquipSlot slot in AllSlots)
            if (equipment.Get(slot) == item) return true;
        return false;
    }

    private void DebugGiveCurrency()
    {
        foreach (ShardType type in ShardInfo.All) pouch.Add(type, 10);
        wallet.Add(500);
        status = "Debug: added 10 of each shard and 500 gold.";
        statusIsError = false;
    }

    private static void DrawBox(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private void EnsureStyles()
    {
        if (labelStyle != null && labelStyle.fontSize == fontSize) return;

        labelStyle = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = fontSize };
        labelStyle.normal.textColor = Color.white;

        rowLabelStyle = new GUIStyle(labelStyle) { wordWrap = false };

        rowStyle = new GUIStyle(GUI.skin.button)
        {
            richText = true,
            alignment = TextAnchor.MiddleLeft,
            fontSize = fontSize,
            padding = new RectOffset(8, 8, 4, 4),
        };

        actionStyle = new GUIStyle(GUI.skin.button)
        {
            richText = true,
            alignment = TextAnchor.MiddleCenter,
            fontSize = fontSize,
        };
    }
}
