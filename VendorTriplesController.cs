using ExileCore2.PoEMemory.Elements.InventoryElements;
using ExileCore2.PoEMemory;
using ExileCore2.Shared.Enums;
using ExileCore2.Shared;
using ExileCore2;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System;
using WheresMyReforgeAt;
using System.Linq;

public class VendorTriplesController
{
    private readonly GameController _gameController;
    private readonly InputHandler _inputHandler;
    private readonly InventoryScanner _inventoryScanner;
    private readonly WheresMyReforgeAtSettings _settings;

    private bool _isProcessingTriples;
    private List<ItemGroup> _currentGroups;
    private CancellationTokenSource _cancellationTokenSource;

    public VendorTriplesController(
        GameController gameController,
        InputHandler inputHandler,
        InventoryScanner inventoryScanner,
        WheresMyReforgeAtSettings settings)
    {
        _gameController = gameController;
        _inputHandler = inputHandler;
        _inventoryScanner = inventoryScanner;
        _settings = settings;
        _cancellationTokenSource = new CancellationTokenSource();
        _currentGroups = new List<ItemGroup>();
    }

    public bool IsProcessingTriples => _isProcessingTriples;

    public void ToggleProcessingTriples()
    {
        _isProcessingTriples = !_isProcessingTriples;
        if (!_isProcessingTriples)
        {
            ResetCancellationToken();
        }
        else
        {
            // Find all triples when starting
            _currentGroups = _inventoryScanner.FindItemTriplesForVendor();
            DebugWindow.LogMsg($"Found {_currentGroups.Count} triples for processing");
        }
    }

    private void ResetCancellationToken()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async SyncTask<bool> ProcessTriples(ReforgeUIElements uiElements)
    {
        try
        {
            var ingameState = _gameController.Game.IngameState;
            bool stashVisible = ingameState.IngameUi.StashElement.IsVisible;
            bool reforgeVisible = uiElements != null;

            // Check conditions
            if (stashVisible)
            {
                // Move items from stash to inventory
                return await ProcessStashToInventory();
            }
            else if (reforgeVisible)
            {
                // Move items from inventory to reforge
                return await ProcessInventoryToReforge(uiElements);
            }
            else
            {
                // Neither stash nor reforge panel is open
                DebugWindow.LogError("Please open either your stash or the reforge panel first.");
                _isProcessingTriples = false;
                return false;
            }
        }
        catch (Exception ex)
        {
            DebugWindow.LogError($"Processing triples failed: {ex.Message}");
            return false;
        }
        finally
        {
            CleanupOperation();
        }
    }

    private async SyncTask<bool> ProcessStashToInventory()
    {
        DebugWindow.LogMsg("Moving items from stash to inventory");

        // Find a group that has items in stash
        ItemGroup groupToProcess = null;
        foreach (var group in _currentGroups)
        {
            if (group.StashItems.Count > 0)
            {
                groupToProcess = group;
                break;
            }
        }

        if (groupToProcess == null)
        {
            DebugWindow.LogError("No triples with stash items found");
            _isProcessingTriples = false;
            return false;
        }

        DebugWindow.LogMsg($"Moving group from stash: {groupToProcess.Rarity} {groupToProcess.BaseType}");
        DebugWindow.LogMsg($"Stash items: {groupToProcess.StashItems.Count}, Inventory items: {groupToProcess.InventoryItems.Count}");

        // Move each item to inventory by clicking it (Ctrl+Click)
        foreach (var item in groupToProcess.StashItems.Take(3))
        {
            DebugWindow.LogMsg($"Moving item: {item.Item.RenderName} from stash");

            try
            {
                await _inputHandler.PressControlKey(async () =>
                {
                    await _inputHandler.MoveCursorAndClick(
                        item.GetClientRect().Center,
                        _cancellationTokenSource.Token);
                }, _cancellationTokenSource.Token);

                // Add a longer delay to ensure the item has time to move
                await Task.Delay(300, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"Error moving item: {ex.Message}");
            }
        }

        // Operation complete
        _isProcessingTriples = false;
        return true;
    }

    private async SyncTask<bool> ProcessInventoryToReforge(ReforgeUIElements uiElements)
    {
        DebugWindow.LogMsg("Moving items from inventory to reforge panel");

        // Find a group that has items in inventory
        ItemGroup groupToProcess = null;
        foreach (var group in _currentGroups)
        {
            if (group.InventoryItems.Count >= 3)
            {
                groupToProcess = group;
                break;
            }
        }

        if (groupToProcess == null)
        {
            DebugWindow.LogError("No triples with enough inventory items found");
            _isProcessingTriples = false;
            return false;
        }

        DebugWindow.LogMsg($"Processing group: {groupToProcess.Rarity} {groupToProcess.BaseType}");
        DebugWindow.LogMsg($"Inventory items: {groupToProcess.InventoryItems.Count}");

        // Place items in slots
        var inventoryItems = groupToProcess.InventoryItems.Take(3).ToList();
        for (int i = 0; i < inventoryItems.Count; i++)
        {
            if (!await MoveItemToSlot(inventoryItems[i], uiElements.MapSlots[i + 1], i + 1))
            {
                return false;
            }
        }

        // ADD THIS CODE HERE:
        // After placing all items, click the reforge button
        DebugWindow.LogMsg("Clicking reforge button");

        await _inputHandler.MoveCursorAndClick(
            uiElements.ReforgeButton.GetClientRectCache.Center,
            _cancellationTokenSource.Token);

        var outputSlot = uiElements.MapSlots[0];
        if (!await WaitForItemInSlot(outputSlot))
        {
            DebugWindow.LogError("Failed to reforge items");
            return false;
        }
        await Task.Delay(_settings.Timings.ReforgeDelayMs, _cancellationTokenSource.Token);
        // Collect the output item
        DebugWindow.LogMsg("Collecting result");
        await _inputHandler.PressControlKey(async () =>
        {
            await _inputHandler.MoveCursorAndClick(
                outputSlot.GetClientRectCache.Center,
                _cancellationTokenSource.Token);
        }, _cancellationTokenSource.Token);

        
        // END OF ADDED CODE

        // Operation complete
        _isProcessingTriples = false;
        return true;
    }
    private bool IsItemInStash(NormalInventoryItem item)
    {
        try
        {
            var parent = item.Parent;
            if (parent == null)
            {
                DebugWindow.LogError($"Item {item.Item.RenderName} has no parent");
                return false;
            }

            var stashAddress = _gameController.Game.IngameState.IngameUi.StashElement.VisibleStash.Address;
            bool isInStash = parent.Address == stashAddress;

            if (isInStash)
            {
                DebugWindow.LogMsg($"Item {item.Item.RenderName} is in stash");
            }

            return isInStash;
        }
        catch (Exception ex)
        {
            DebugWindow.LogError($"Error checking if item is in stash: {ex.Message}");
            return false;
        }
    }

    private bool IsItemInInventory(NormalInventoryItem item)
    {
        try
        {
            var parent = item.Parent;
            return parent != null &&
                parent.Address == _gameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].Address;
        }
        catch
        {
            return false;
        }
    }

    private async SyncTask<bool> MoveItemToSlot(NormalInventoryItem item, Element targetSlot, int slotIndex)
    {
        try
        {
            DebugWindow.LogMsg($"Moving item {item.Item.RenderName} to slot {slotIndex}");

            // Ctrl+Click the item to place it directly in the slot
            await _inputHandler.PressControlKey(async () =>
            {
                await _inputHandler.MoveCursorAndClick(
                    item.GetClientRect().Center,
                    _cancellationTokenSource.Token);
            }, _cancellationTokenSource.Token);

            // Wait for item to appear in slot
            if (!await WaitForItemInSlot(targetSlot))
            {
                DebugWindow.LogError($"Failed to place item in slot {slotIndex}");
                return false;
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            DebugWindow.LogMsg("Item movement cancelled");
            return false;
        }
    }

    private async SyncTask<bool> PerformReforge(ReforgeUIElements uiElements)
    {
        try
        {
            DebugWindow.LogMsg("Clicking reforge button");

            await _inputHandler.MoveCursorAndClick(
                uiElements.ReforgeButton.GetClientRectCache.Center,
                _cancellationTokenSource.Token);

            var outputSlot = uiElements.MapSlots[0];
            if (!await WaitForItemInSlot(outputSlot))
            {
                DebugWindow.LogError("Failed to reforge items");
                return false;
            }

            // Collect the output item
            DebugWindow.LogMsg("Collecting result");
            await _inputHandler.PressControlKey(async () =>
            {
                await _inputHandler.MoveCursorAndClick(
                    outputSlot.GetClientRectCache.Center,
                    _cancellationTokenSource.Token);
            }, _cancellationTokenSource.Token);

            await Task.Delay(_settings.Timings.ReforgeDelayMs, _cancellationTokenSource.Token);

            return true;
        }
        catch (OperationCanceledException)
        {
            DebugWindow.LogError("Reforge operation cancelled");
            return false;
        }
    }

    private async SyncTask<bool> WaitForItemInSlot(Element slot) =>
        await TaskUtils.CheckEveryFrame(
            () => slot.ChildCount > 1,
            new CancellationTokenSource(_settings.Timings.SlotWaitTimeoutMs).Token);

    private void CleanupOperation()
    {
        Input.KeyUp(Keys.LControlKey);
        _isProcessingTriples = false;
    }
}