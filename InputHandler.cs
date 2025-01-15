using ExileCore2;
using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WheresMyReforgeAt
{
    public class InputHandler
    {
        private readonly Vector2 _windowPosition;
        private readonly WheresMyReforgeAtSettings _settings;

        public InputHandler(Vector2 windowPosition, WheresMyReforgeAtSettings settings)
        {
            _windowPosition = windowPosition;
            _settings = settings;
        }

        public async Task MoveCursorAndClick(Vector2 position, CancellationToken cancellationToken)
        {
            Input.SetCursorPos(position + _windowPosition);
            await Task.Delay(_settings.Timings.InputDelayMs, cancellationToken);
            Input.Click(MouseButtons.Left);
        }

        public async Task PressControlKey(Func<Task> action, CancellationToken cancellationToken)
        {
            try
            {
                Input.KeyDown(Keys.LControlKey);
                await Task.Delay(_settings.Timings.InputDelayMs, cancellationToken);
                await action();
            }
            finally
            {
                Input.KeyUp(Keys.LControlKey);
            }
        }
    }
}
