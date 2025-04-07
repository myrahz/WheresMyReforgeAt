using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.Elements.InventoryElements;
using ExileCore2.Shared.Enums;
using ExileCore2;
using System.Collections.Generic;
using System.Drawing;
using System;
using WheresMyReforgeAt;
using ExileCore2.PoEMemory.MemoryObjects;

public class InventoryScanner
{
    private readonly GameController _gameController;
    private readonly WheresMyReforgeAtSettings _settings;

    public InventoryScanner(GameController gameController, WheresMyReforgeAtSettings settings)
    {
        _gameController = gameController;
        _settings = settings;
        if (_settings.DebugMode)
            DebugWindow.LogMsg("InventoryScanner initialized", 5, Color.Cyan);
    }



    public (List<NormalInventoryItem> allItems, List<NormalInventoryItem> inventoryItems, List<NormalInventoryItem> stashItems) ScanInventoryAndStash(Func<NormalInventoryItem, bool> filter = null)
    {
        if (_settings.DebugMode)
            DebugWindow.LogMsg("Starting inventory and stash scan", 5, Color.Yellow);

        var allItems = new List<NormalInventoryItem>();
        var inventoryItems = new List<NormalInventoryItem>();
        var stashItems = new List<NormalInventoryItem>();

        var ingameState = _gameController.Game.IngameState;
        var inventoryPanel = ingameState.IngameUi.InventoryPanel;
        var playerInventory = inventoryPanel[InventoryIndex.PlayerInventory];

        // Get player inventory items
        var visibleInventoryItems = playerInventory.VisibleInventoryItems;
        if (_settings.DebugMode)
            DebugWindow.LogMsg($"Found {visibleInventoryItems.Count} items in player inventory", 5, Color.Yellow);

        // Process inventory items
        foreach (var item in visibleInventoryItems)
        {
            if (filter == null || filter(item))
            {
                allItems.Add(item);
                inventoryItems.Add(item);
            }
        }

        // Add stash items if stash is visible and setting is enabled
        if (_settings.ScanStashItems && ingameState.IngameUi.StashElement.IsVisible)
        {
            var visibleStashItems = ingameState.IngameUi.StashElement.VisibleStash.VisibleInventoryItems;
            if (_settings.DebugMode)
                DebugWindow.LogMsg($"Found {visibleStashItems.Count} items in visible stash tab", 5, Color.Yellow);

            // Process stash items
            foreach (var item in visibleStashItems)
            {
                if (filter == null || filter(item))
                {
                    allItems.Add(item);
                    stashItems.Add(item);
                }
            }
        }

        if (_settings.DebugMode)
        {
            DebugWindow.LogMsg($"Total items after filtering: {allItems.Count}", 5, Color.Yellow);
            DebugWindow.LogMsg($"Inventory items: {inventoryItems.Count}", 5, Color.Yellow);
            DebugWindow.LogMsg($"Stash items: {stashItems.Count}", 5, Color.Yellow);
        }

        return (allItems, inventoryItems, stashItems);
    }

    public List<ItemGroup> FindItemTriplesForVendor()
    {
        if (_settings.DebugMode)
            DebugWindow.LogMsg("Searching for vendor recipe triples...");

        // Get all eligible items with their location information
        var (allItems, inventoryItems, stashItems) = ScanInventoryAndStash(IsEligibleForVendorRecipe);
        if (_settings.DebugMode)
            DebugWindow.LogMsg($"Found {allItems.Count} eligible items for vendor recipes");

        // Create a dictionary to manually group items
        var groupedItems = new Dictionary<string, List<NormalInventoryItem>>();
        var groupedRarities = new Dictionary<string, ItemRarity>();
        var groupedBaseTypes = new Dictionary<string, string>();
        var groupedStashItems = new Dictionary<string, List<NormalInventoryItem>>();
        var groupedInventoryItems = new Dictionary<string, List<NormalInventoryItem>>();

        // Manually group items by base type and rarity
        foreach (var item in allItems)
        {
            var baseType = GetBaseType(item.Item.Path);
            var rarity = item.Item.GetComponent<Mods>().ItemRarity;

            // Create a unique key for this base type and rarity combination
            var key = $"{baseType}_{rarity}";

            // Add to the appropriate group or create a new one
            if (!groupedItems.ContainsKey(key))
            {
                if (_settings.DebugMode)
                    DebugWindow.LogMsg($"Creating new group for: {baseType} ({rarity})");
                groupedItems[key] = new List<NormalInventoryItem>();
                groupedRarities[key] = rarity;
                groupedBaseTypes[key] = baseType;
                groupedStashItems[key] = new List<NormalInventoryItem>();
                groupedInventoryItems[key] = new List<NormalInventoryItem>();
            }

            groupedItems[key].Add(item);

            // Determine if the item is in stash or inventory based on our scan results
            if (stashItems.Contains(item))
            {
                groupedStashItems[key].Add(item);
            }
            else if (inventoryItems.Contains(item))
            {
                groupedInventoryItems[key].Add(item);
            }
        }

        // Convert dictionary groups to ItemGroup list, filtering for groups with at least 3 items
        var result = new List<ItemGroup>();
        foreach (var key in groupedItems.Keys)
        {
            var items = groupedItems[key];
            var itemsInStash = groupedStashItems[key];
            var itemsInInventory = groupedInventoryItems[key];

            if (_settings.DebugMode)
                DebugWindow.LogMsg($"Group {key}: {items.Count} items (Stash: {itemsInStash.Count}, Inventory: {itemsInInventory.Count})");

            if (items.Count >= 3)  // Only groups with at least 3 items
            {
                if (_settings.DebugMode)
                    DebugWindow.LogMsg($"FOUND TRIPLE! {groupedBaseTypes[key]} ({groupedRarities[key]})");
                result.Add(new ItemGroup(
                    groupedRarities[key],
                    groupedBaseTypes[key],
                    items,
                    itemsInStash,
                    itemsInInventory
                ));
            }
        }

        if (_settings.DebugMode)
            DebugWindow.LogMsg($"Found {result.Count} triples for vendor recipes");
        return result;
    }
    private bool IsItemInStash(NormalInventoryItem item)
    {
        try
        {
            var parent = item.Parent;
            if (parent == null) return false;

            var stashAddress = _gameController.IngameState.IngameUi.StashElement.VisibleStash.Address;
            return parent.Address == stashAddress;
        }
        catch
        {
            return false;
        }
    }
    private bool IsEligibleForVendorRecipe(NormalInventoryItem item)
    {
        // Check if it has Mods component
        if (!item.Item.HasComponent<Mods>())
            return false;

        var mods = item.Item.GetComponent<Mods>();

        // Check rarity - only Magic or Rare
        if (mods.ItemRarity != ItemRarity.Magic && mods.ItemRarity != ItemRarity.Rare)
            return false;

        // Check item path for eligible categories
        var path = item.Item.Path;
        var isEligible = path.Contains("Metadata/Items/Rings/") ||
                        path.Contains("Metadata/Items/Amulets/") ||
                        path.Contains("Metadata/Items/Belts/") ||
                        path.Contains("Metadata/Items/Weapons/") ||
                        path.Contains("Metadata/Items/Quivers/") ||
                        path.Contains("Metadata/Items/Armours/");

        if (isEligible)
        {
            if (_settings.DebugMode)
                DebugWindow.LogMsg($"Eligible item found: {item.Item.RenderName}", 5, Color.Orange);
        }

        return isEligible;
    }

    private string GetBaseType(string itemPath)
    {
        // Extract the base type from the item path
        var parts = itemPath.Split('/');
        if (parts.Length >= 4)
        {
            var baseType = parts[parts.Length - 2] + "/" + parts[parts.Length - 1];
            if (_settings.DebugMode)
                DebugWindow.LogMsg($"Path: {itemPath} -> BaseType: {baseType}", 5, Color.LightBlue);
            return baseType;
        }
        if (_settings.DebugMode)
            DebugWindow.LogMsg($"Unexpected path format: {itemPath}", 5, Color.Red);
        return itemPath; // Fallback to full path if format is unexpected
    }
}