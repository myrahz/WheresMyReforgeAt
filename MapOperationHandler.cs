using ExileCore2.PoEMemory.Elements.InventoryElements;
using ExileCore2.PoEMemory;
using ExileCore2.Shared.Enums;
using ExileCore2.Shared;
using ExileCore2;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WheresMyReforgeAt
{
    public class MapOperationHandler
    {
        private readonly GameController _gameController;
        private readonly InputHandler _inputHandler;
        private readonly WheresMyReforgeAtSettings _settings;

        public MapOperationHandler(
            GameController gameController,
            InputHandler inputHandler,
            WheresMyReforgeAtSettings settings)
        {
            _gameController = gameController;
            _inputHandler = inputHandler;
            _settings = settings;
        }

        public async SyncTask<bool> MoveMapToSlot(NormalInventoryItem map, Element targetSlot, int slotIndex, CancellationToken cancellationToken)
        {
            try
            {
                if (!await MoveMapToCursor(map, cancellationToken)) return false;
                if (!await PlaceMapInSlot(targetSlot, slotIndex, cancellationToken)) return false;

                return true;
            }
            catch (OperationCanceledException)
            {
                DebugWindow.LogMsg("Map movement cancelled");
                return false;
            }
        }

        public async Task CollectMapFromSlot(Element slot, CancellationToken cancellationToken)
        {
            try
            {
                await _inputHandler.PressControlKey(async () =>
                {
                    await _inputHandler.MoveCursorAndClick(
                        slot.GetClientRectCache.Center,
                        cancellationToken);
                }, cancellationToken);

                await Task.Delay(_settings.Timings.ReforgeDelayMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                DebugWindow.LogMsg("Map collection cancelled");
                throw;
            }
        }

        private async SyncTask<bool> MoveMapToCursor(NormalInventoryItem map, CancellationToken cancellationToken)
        {
            await _inputHandler.MoveCursorAndClick(map.GetClientRectCache.Center, cancellationToken);
            return await WaitForMapOnCursor();
        }

        private async SyncTask<bool> PlaceMapInSlot(Element targetSlot, int slotIndex, CancellationToken cancellationToken)
        {
            await _inputHandler.MoveCursorAndClick(targetSlot.GetClientRectCache.Center, cancellationToken);

            if (!await WaitForMapInSlot(targetSlot))
            {
                DebugWindow.LogError($"Failed to place map in slot {slotIndex}");
                return false;
            }

            return true;
        }

        private async SyncTask<bool> WaitForMapOnCursor() =>
            await WaitForCondition(
                () => _gameController.Game.IngameState.ServerData
                    .PlayerInventories[(int)InventorySlotE.Cursor1]
                    .Inventory.ItemCount > 0,
                _settings.Timings.CursorWaitTimeoutMs,
                "Timeout waiting for map on cursor");

        private async SyncTask<bool> WaitForMapInSlot(Element slot) =>
            await WaitForCondition(
                () => slot.ChildCount > 1,
                _settings.Timings.SlotWaitTimeoutMs,
                "Timeout waiting for map in slot");

        private async SyncTask<bool> WaitForCondition(Func<bool> condition, int timeoutMs, string errorMessage)
        {
            try
            {
                return await TaskUtils.CheckEveryFrame(
                    condition,
                    new CancellationTokenSource(timeoutMs).Token);
            }
            catch (OperationCanceledException)
            {
                DebugWindow.LogError(errorMessage);
                return false;
            }
        }
    }
}
