using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using System.Windows.Forms;

namespace WheresMyReforgeAt;

public class WheresMyReforgeAtSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new ToggleNode(false);
    public ToggleNode DebugMode { get; set; } = new ToggleNode(false);


    [Menu("Toggle", "Hotkey used to start/stop reforging")]
    public HotkeyNode ReforgeHotkey { get; set; } = new HotkeyNode(Keys.None);

    [Menu("Process Vendor Triples Hotkey", "Hotkey to process vendor recipe triples")]
    public HotkeyNode ProcessTriplesHotkey { get; set; } = new HotkeyNode(Keys.None);

    public Timings Timings { get; set; } = new Timings();
    public MapSettings MapSettings { get; set; } = new MapSettings();

    [Menu("Scan Stash Items", "Include items from visible stash tab")]
    public ToggleNode ScanStashItems { get; set; } = new ToggleNode(false);    
    
    [Menu("Thickness border")]
    public RangeNode<int> BorderThickness { get; set; } = new RangeNode<int>(5, 1, 10);


        
    [Menu("Vendor Recipe Rarities", "Configure which rarities to include for vendor recipes")]
    public VendorRecipeRarities VendorRecipeRarities2 { get; set; } = new VendorRecipeRarities();

    [Submenu(CollapsedByDefault = false)]
    public class VendorRecipeRarities
    {
        [Menu("Include Magic Items", "Include Magic (blue) items in vendor recipe search")]
        public ToggleNode IncludeMagic { get; set; } = new ToggleNode(true);

        [Menu("Include Rare Items", "Include Rare (yellow) items in vendor recipe search")]
        public ToggleNode IncludeRare { get; set; } = new ToggleNode(true);
    }
}

[Submenu(CollapsedByDefault = false)]
public class Timings
{
    [Menu("Cursor Wait Timeout (ms)", "Maximum time to wait for map to appear on cursor after clicking")]
    public RangeNode<int> CursorWaitTimeoutMs { get; set; } = new RangeNode<int>(150, 0, 2000);
    [Menu("Slot Wait Timeout (ms)", "Maximum time to wait for map to appear in slot after clicking")]
    public RangeNode<int> SlotWaitTimeoutMs { get; set; } = new RangeNode<int>(1000, 0, 2000);
    [Menu("Input Delay (ms)", "Delay between input actions")]
    public RangeNode<int> InputDelayMs { get; set; } = new RangeNode<int>(150, 0, 2000);
    [Menu("Reforge Delay (ms)", "Delay to wait after reforging before moving on")]
    public RangeNode<int> ReforgeDelayMs { get; set; } = new RangeNode<int>(500, 0, 2000);
}

[Submenu(CollapsedByDefault = false)]
public class MapSettings
{
    [Menu("Reforge By Rarity", "Only reforge maps of the same rarity")]
    public ToggleNode UseRarity { get; set; } = new ToggleNode(false);

    
}