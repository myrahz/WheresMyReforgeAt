using ExileCore2.PoEMemory;
using ExileCore2.Shared;
using ExileCore2;
using System;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Numerics;
using Graphics = ExileCore2.Graphics;

namespace WheresMyReforgeAt
{
    public class ReforgeController
    {
        private readonly GameController _gameController;
        private readonly MapOperationHandler _mapOperationHandler;
        private readonly MapScanner _mapScanner;
        private readonly WheresMyReforgeAtSettings _settings;
        private readonly InputHandler _inputHandler;

        private bool _isReforging;
        private CancellationTokenSource _cancellationTokenSource;
        private ReforgeUIElements _uiElements;

        private DateTime _splashStartTime;
        private bool _splashShown;
        private float _splashOpacity;
        private const float SPLASH_DURATION = 5f;
        private const float FADE_DURATION = 0.5f;

        public ReforgeController(
            GameController gameController,
            InputHandler inputHandler,
            MapScanner mapScanner,
            WheresMyReforgeAtSettings settings)
        {
            _gameController = gameController;
            _inputHandler = inputHandler;
            _mapScanner = mapScanner;
            _settings = settings;
            _mapOperationHandler = new MapOperationHandler(gameController, inputHandler, settings);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public bool IsReforging => _isReforging;

        public void ToggleReforging()
        {
            _isReforging = !_isReforging;
            if (!_isReforging)
            {
                ResetCancellationToken();
            }
        }

        private void ResetCancellationToken()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Render(Graphics graphics)
        {
            if (_splashShown)
            {
                var elapsedTime = (float)(DateTime.UtcNow - _splashStartTime).TotalSeconds;

                if (elapsedTime < FADE_DURATION)
                {
                    _splashOpacity = elapsedTime / FADE_DURATION;
                }
                else if (elapsedTime > SPLASH_DURATION - FADE_DURATION)
                {
                    _splashOpacity = Math.Max(0f, 1f - (elapsedTime - (SPLASH_DURATION - FADE_DURATION)) / FADE_DURATION);
                }
                else
                {
                    _splashOpacity = 1f;
                }

                if (elapsedTime >= SPLASH_DURATION)
                {
                    _splashShown = false;
                }
            }

            if (_splashShown)
            {
                RenderSplashScreen(graphics);
            }
        }

        private void RenderSplashScreen(Graphics graphics)
        {
            var windowSize = _gameController.Window.GetWindowRectangleTimeCache.Size;
            var center = new Vector2(windowSize.X / 2, windowSize.Y / 2);

            var boxWidth = 300f;
            var boxHeight = 100f;
            var boxPos = new Vector2(center.X - boxWidth / 2, center.Y - boxHeight / 2);

            var overlayColor = Color.FromArgb((int)(200 * _splashOpacity), 0, 0, 0);
            graphics.DrawBox(
                new Vector2(0, 0),
                new Vector2(windowSize.X, windowSize.Y),
                overlayColor);

            
            var borderColor = Color.FromArgb((int)(255 * _splashOpacity), 61, 133, 224);
            graphics.DrawBox(
                new Vector2(boxPos.X - 2, boxPos.Y - 2),
                new Vector2(boxPos.X + boxWidth + 2, boxPos.Y + boxHeight + 2),
                borderColor);

            var boxColor = Color.FromArgb((int)(255 * _splashOpacity), 40, 44, 52);
            graphics.DrawBox(
                new Vector2(boxPos.X, boxPos.Y),
                new Vector2(boxPos.X + boxWidth, boxPos.Y + boxHeight),
                boxColor);

            var text = "Powered By Claude";
            var fontScale = graphics.SetTextScale(1.5f);
            var textSize = graphics.MeasureText(text);
            var textPos = new Vector2(
                center.X - textSize.X / 2,
                center.Y - textSize.Y / 2);

            var glowColor = Color.FromArgb((int)(100 * _splashOpacity), 61, 133, 224);
            for (var i = 0; i < 360; i += 45)
            {
                var angle = i * Math.PI / 180;
                var offset = new Vector2(
                    (float)Math.Cos(angle) * 2,
                    (float)Math.Sin(angle) * 2);
                graphics.DrawText(
                    text,
                    textPos + offset,
                    glowColor);
            }

            var textColor = Color.FromArgb((int)(255 * _splashOpacity), 255, 255, 255);
            graphics.DrawText(text, textPos, textColor);

            fontScale.Dispose();
        }

        public async SyncTask<bool> ProcessReforgeOperation(ReforgeUIElements uiElements)
        {
            try
            {
                _splashStartTime = DateTime.UtcNow;
                _splashOpacity = 0f;
                _splashShown = true;

                _uiElements = uiElements;

                while (_isReforging && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    if (!await ProcessNextReforge())
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"Reforge task failed: {ex.Message}");
                return false;
            }
            finally
            {
                CleanupReforgeOperation();
            }
        }

        private async SyncTask<bool> ProcessNextReforge()
        {
            var mapGroups = _mapScanner.ScanInventoryForMaps();

            foreach (var group in mapGroups)
            {
                if (await ProcessMapGroup(group))
                {
                    return true;
                }
            }

            return false;
        }

        private async SyncTask<bool> ProcessMapGroup(MapGroup group)
        {
            if (group.Items.Count < 3) return false;

            for (int i = 0; i < 3; i++)
            {
                if (!await _mapOperationHandler.MoveMapToSlot(
                    group.Items[i],
                    _uiElements.MapSlots[i + 1],
                    i + 1,
                    _cancellationTokenSource.Token))
                {
                    return false;
                }
            }

            return await PerformReforge();
        }

        private async SyncTask<bool> PerformReforge()
        {
            try
            {
                await _inputHandler.MoveCursorAndClick(
                    _uiElements.ReforgeButton.GetClientRectCache.Center,
                    _cancellationTokenSource.Token);

                var outputSlot = _uiElements.MapSlots[0];
                if (!await WaitForMapInSlot(outputSlot))
                {
                    DebugWindow.LogError("Failed to reforge map");
                    return false;
                }

                await _mapOperationHandler.CollectMapFromSlot(outputSlot, _cancellationTokenSource.Token);
                return true;
            }
            catch (OperationCanceledException)
            {
                DebugWindow.LogMsg("Reforge operation cancelled");
                return false;
            }
        }

        private async SyncTask<bool> WaitForMapInSlot(Element slot) =>
            await TaskUtils.CheckEveryFrame(
                () => slot.ChildCount > 1,
                new CancellationTokenSource(_settings.Timings.SlotWaitTimeoutMs).Token);

        private void CleanupReforgeOperation()
        {
            Input.KeyUp(Keys.LControlKey);
            _isReforging = false;
        }
    }
}
