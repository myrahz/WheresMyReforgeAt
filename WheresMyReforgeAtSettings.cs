using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using System.Windows.Forms;

namespace WheresMyReforgeAt;

public class WheresMyReforgeAtSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new ToggleNode(false);
    [Menu("Toggle", "Hotkey used to start/stop reforging")]
    public HotkeyNode ReforgeHotkey { get; set; } = new HotkeyNode(Keys.None);

    public Timings Timings { get; set; } = new Timings();
    public MapSettings MapSettings { get; set; } = new MapSettings();
}

[Submenu(CollapsedByDefault = false)]
public class Timings
{
    [Menu("Cursor Wait Timeout (ms)", "Maximum time to wait for map to appear on cursor after clicking")]
    public RangeNode<int> CursorWaitTimeoutMs { get; set; } = new RangeNode<int>(60, 0, 2000);
    [Menu("Slot Wait Timeout (ms)", "Maximum time to wait for map to appear in slot after clicking")]
    public RangeNode<int> SlotWaitTimeoutMs { get; set; } = new RangeNode<int>(1000, 0, 2000);
    [Menu("Input Delay (ms)", "Delay between input actions")]
    public RangeNode<int> InputDelayMs { get; set; } = new RangeNode<int>(50, 0, 2000);
    [Menu("Reforge Delay (ms)", "Delay to wait after reforging before moving on")]
    public RangeNode<int> ReforgeDelayMs { get; set; } = new RangeNode<int>(100, 0, 2000);
}

[Submenu(CollapsedByDefault = false)]
public class MapSettings
{
    [Menu("Reforge By Rarity", "Only reforge maps of the same rarity")]
    public ToggleNode UseRarity { get; set; } = new ToggleNode(false);
}