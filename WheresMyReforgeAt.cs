using ExileCore2;
using ExileCore2.PoEMemory;
using ExileCore2.PoEMemory.Elements;
using ExileCore2.PoEMemory.Elements.InventoryElements;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared;
using ExileCore2.Shared.Enums;
using ExileCore2.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace WheresMyReforgeAt
{
    public record MapGroup(int Tier, ItemRarity Rarity, List<NormalInventoryItem> Items);
    public record ReforgeUIElements(Element ReforgePanel, Element ReforgeButton, IReadOnlyList<Element> MapSlots);

    public record ItemGroup(
    ItemRarity Rarity,
    string BaseType,
    List<NormalInventoryItem> Items,
    List<NormalInventoryItem> StashItems,
    List<NormalInventoryItem> InventoryItems);

    public class WheresMyReforgeAt : BaseSettingsPlugin<WheresMyReforgeAtSettings>
    {
        private ReforgeController _reforgeController;
        private ReforgeUIHandler _uiHandler;
        private SyncTask<bool> _reforgeTask;
        private VendorTriplesController _vendorTriplesController;
        private SyncTask<bool> _triplesTask;
        private InventoryScanner _inventoryScanner;
        private Dictionary<string, Color> _itemGroupColors = new Dictionary<string, Color>();
        private List<ItemGroup> _currentItemGroups = new List<ItemGroup>();
        private static readonly Color[] _groupColors = new Color[]

        {
            Color.Red,
            Color.Green,
            Color.Blue,
            Color.Yellow,
            Color.Cyan,
            Color.Magenta,
            Color.Orange,
            Color.LimeGreen,
            Color.Purple,
            Color.Turquoise
        };
        private readonly Queue<SyncTask<bool>> _pendingOperations = new Queue<SyncTask<bool>>();
        private SyncTask<bool> _currentOperation;
        private bool _isProcessingTriples = false;
        public override bool Initialise()
        {
            var windowPosition = GameController.Window.GetWindowRectangleTimeCache.TopLeft.RoundToVector2I();
            _inventoryScanner = new InventoryScanner(GameController, Settings);
            var inputHandler = new InputHandler(windowPosition, Settings);
            var mapScanner = new MapScanner(GameController, Settings);
            _uiHandler = new ReforgeUIHandler(GameController);
            _reforgeController = new ReforgeController(GameController, inputHandler, mapScanner, Settings);
            _vendorTriplesController = new VendorTriplesController(GameController, inputHandler, _inventoryScanner, Settings);


            return true;
        }

        public override void Tick()
        {
            if (Settings.ReforgeHotkey.PressedOnce())
            {
                _reforgeController.ToggleReforging();
            }

            if (Settings.ProcessTriplesHotkey.PressedOnce())
            {
                // This now handles both stash-to-inventory and inventory-to-reforge operations
                _vendorTriplesController.ToggleProcessingTriples();
            }

            if (_reforgeController.IsReforging)
            {
                var uiElements = _uiHandler.FindReforgeElements();
                if (uiElements != null)
                {
                    TaskUtils.RunOrRestart(ref _reforgeTask, () => _reforgeController.ProcessReforgeOperation(uiElements));
                }
            }

            if (_vendorTriplesController.IsProcessingTriples)
            {
                // Note that we pass the reforge elements even if they're null
                // The controller will check and handle appropriately based on what's visible
                var reforgeElements = _uiHandler.FindReforgeElements();
                TaskUtils.RunOrRestart(ref _triplesTask, () => _vendorTriplesController.ProcessTriples(reforgeElements));
            }
        }

        
        public void ProcessVendorRecipeSets()
        {
            // Clear previous groups
            _currentItemGroups.Clear();
            _itemGroupColors.Clear();

            // Find new groups
            var itemGroups = _inventoryScanner.FindItemTriplesForVendor();
            _currentItemGroups = itemGroups;


            // Assign a unique color to each group
            for (int i = 0; i < itemGroups.Count; i++)
            {
                var group = itemGroups[i];
                var colorIndex = i % _groupColors.Length;
                var key = $"{group.Rarity}_{group.BaseType}";
                _itemGroupColors[key] = _groupColors[colorIndex];
    
            }
        }
        public void renderTriples()
        {
            if (_currentItemGroups.Count > 0)
            {
                foreach (var group in _currentItemGroups)
                {
                    var key = $"{group.Rarity}_{group.BaseType}";
                    var color = _itemGroupColors.TryGetValue(key, out var c) ? c : Color.White;

                    // Take only the first 3 items for each group
                    foreach (var item in group.Items.Take(3))
                    {
                        var clientRect = item.GetClientRect();
                        // Make the rectangle slightly larger for visibility
                        var drawRect = clientRect;
                        drawRect.Top += 3;
                        drawRect.Bottom -= 3;
                        drawRect.Right -= 3;
                        drawRect.Left += 3;

                        var hoveredItem = GameController.IngameState.UIHover.AsObject<HoverItemIcon>();

                        var hoveredItemTooltip = hoveredItem?.Tooltip;
                        var tooltipRect = hoveredItemTooltip?.GetClientRect();
                        var canDraw = false;
                        if (tooltipRect == null)
                        {
                            canDraw = true;
                        }
                        else
                        {
                            canDraw = !checkRectOverlaps(drawRect, (ExileCore2.Shared.RectangleF)tooltipRect);
                        }
                        if (canDraw && (GameController.IngameState.IngameUi.StashElement.IsVisible || GameController.IngameState.IngameUi.InventoryPanel.IsVisible))
                        Graphics.DrawFrame(drawRect, color, Settings.BorderThickness);

                        // Optional: Draw item number in group
                        var itemIndex = group.Items.IndexOf(item) + 1;
                        var textPos = new Vector2(clientRect.X + 5, clientRect.Y + 5);
                        Graphics.DrawText($"{itemIndex}", textPos, color);
                    }
                }
            }
        }

        private bool checkRectOverlaps(ExileCore2.Shared.RectangleF rect1, ExileCore2.Shared.RectangleF rect2)
        {

            if (rect1.BottomRight.X < rect2.TopLeft.X || rect2.BottomRight.X < rect1.TopLeft.X)
                return false;

            // Check if one rectangle is above the other
            if (rect1.BottomRight.Y < rect2.TopLeft.Y || rect2.BottomRight.Y < rect1.TopLeft.Y)
                return false;

            return true;
        }
        public override void Render()
        {
            _reforgeController.Render(Graphics);
            ProcessVendorRecipeSets();
            renderTriples();
            Graphics.DrawTextWithBackground(
                    $"Reforging: {_reforgeController.IsReforging}",
                    new Vector2(10, 10),
                    Color.White,
                    Color.Black);
        }
    }
}