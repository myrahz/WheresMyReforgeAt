using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.Elements.InventoryElements;
using ExileCore2.Shared.Enums;
using ExileCore2;
using System.Collections.Generic;
using System.Linq;

namespace WheresMyReforgeAt
{
    public class MapScanner
    {
        private readonly GameController _gameController;
        private readonly WheresMyReforgeAtSettings _settings;

        public MapScanner(GameController gameController, WheresMyReforgeAtSettings settings)
        {
            _gameController = gameController;
            _settings = settings;
        }

        public IEnumerable<MapGroup> ScanInventoryForMaps()
        {
            var items = _gameController.IngameState.IngameUi
                .InventoryPanel[InventoryIndex.PlayerInventory]
                .VisibleInventoryItems;

            return items
                .Where(item => IsValidMap(item))
                .Select(item => (
                    Item: item,
                    Map: item.Item.GetComponent<Map>(),
                    Mods: item.Item.GetComponent<Mods>()))
                .GroupBy(m => CreateMapGroupKey(m.Map, m.Mods))
                .Select(CreateMapGroup)
                .OrderBy(g => g.Tier)
                .ThenBy(g => g.Rarity);
        }

        private static bool IsValidMap(NormalInventoryItem inventoryItem)
        {
            var item = inventoryItem.Item;
            return item != null
                && item.TryGetComponent(out Mods mods)
                && mods.Identified
                && item.TryGetComponent(out Map _);
        }

        private object CreateMapGroupKey(Map map, Mods mods) => _settings.MapSettings.UseRarity
            ? new { map.Tier, Rarity = mods.ItemRarity }
            : new { map.Tier, Rarity = ItemRarity.Normal };

        private static MapGroup CreateMapGroup(IGrouping<dynamic, (NormalInventoryItem Item, Map Map, Mods Mods)> group) =>
            new(group.Key.Tier, group.Key.Rarity, group.Select(m => m.Item).ToList());
    }
}
