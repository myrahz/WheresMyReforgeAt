using ExileCore2;
using ExileCore2.PoEMemory;
using ExileCore2.PoEMemory.Elements.InventoryElements;
using ExileCore2.Shared;
using ExileCore2.Shared.Enums;
using ExileCore2.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace WheresMyReforgeAt
{
    public record MapGroup(int Tier, ItemRarity Rarity, List<NormalInventoryItem> Items);
    public record ReforgeUIElements(Element ReforgePanel, Element ReforgeButton, IReadOnlyList<Element> MapSlots);

    public class WheresMyReforgeAt : BaseSettingsPlugin<WheresMyReforgeAtSettings>
    {
        private ReforgeController _reforgeController;
        private ReforgeUIHandler _uiHandler;
        private SyncTask<bool> _reforgeTask;

        public override bool Initialise()
        {
            var windowPosition = GameController.Window.GetWindowRectangleTimeCache.TopLeft.RoundToVector2I();

            var inputHandler = new InputHandler(windowPosition, Settings);
            var mapScanner = new MapScanner(GameController, Settings);
            _uiHandler = new ReforgeUIHandler(GameController);
            _reforgeController = new ReforgeController(GameController, inputHandler, mapScanner, Settings);

            return true;
        }

        public override void Tick()
        {
            if (Settings.ReforgeHotkey.PressedOnce())
            {
                _reforgeController.ToggleReforging();
            }

            if (_reforgeController.IsReforging)
            {
                var uiElements = _uiHandler.FindReforgeElements();
                if (uiElements != null)
                {
                    TaskUtils.RunOrRestart(ref _reforgeTask, () => _reforgeController.ProcessReforgeOperation(uiElements));
                }
            }
        }

        public override void Render()
        {
            _reforgeController.Render(Graphics);

            Graphics.DrawTextWithBackground(
                    $"Reforging: {_reforgeController.IsReforging}",
                    new Vector2(10, 10),
                    Color.White,
                    Color.Black);
        }
    }
}