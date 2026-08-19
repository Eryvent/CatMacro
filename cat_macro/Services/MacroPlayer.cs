using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CatMacro.Models;

namespace CatMacro.Services
{
    public class MacroPlayerService
    {
        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetMessageExtraInfo();

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const uint INPUT_MOUSE = 0;
        private const uint INPUT_KEYBOARD = 1;

        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        private bool _isPlaying = false;
        private CancellationTokenSource? _cancellationTokenSource;
        private KeyboardMouseHooker? _mouseHooker;
        private int _lastMouseX = 0;
        private int _lastMouseY = 0;

        public event EventHandler<int>? OnLoopUpdate;
        public event EventHandler? OnPlaybackComplete;
        public event EventHandler? OnMouseMovedDuringPlayback;
        public event EventHandler<int>? OnCountdown;

        public async Task PlayAsync(RecordingData recording, string repeatMode, int repeatCount, double playbackSpeed, CancellationToken cancellationToken)
        {
            _isPlaying = true;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // عد تنازلي من 5 قبل البدء
            for (int i = 5; i > 0; i--)
            {
                OnCountdown?.Invoke(this, i);
                await Task.Delay(1000, _cancellationTokenSource.Token);
            }

            OnCountdown?.Invoke(this, 0); // انتهى العد

            // نصب hook الماوس لكشف الحركة أثناء التشغيل
            _mouseHooker = new KeyboardMouseHooker();
            _mouseHooker.OnMouseMove += HandleMouseMovePlayback;
            _mouseHooker.Install();

            try
            {
                int totalLoops = repeatMode == "Once" ? 1 : (repeatMode == "Infinite" ? -1 : repeatCount);
                int currentLoop = 0;

                while (_isPlaying)
                {
                    currentLoop++;
                    OnLoopUpdate?.Invoke(this, currentLoop);

                    if (totalLoops > 0 && currentLoop > totalLoops)
                        break;

                    await PlayRecordingAsync(recording, playbackSpeed, _cancellationTokenSource.Token);

                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                        break;
                }
            }
            finally
            {
                _isPlaying = false;
                if (_mouseHooker != null)
                {
                    _mouseHooker.Uninstall();
                    _mouseHooker = null;
                }
                OnPlaybackComplete?.Invoke(this, EventArgs.Empty);
            }
        }

        private async Task PlayRecordingAsync(RecordingData recording, double playbackSpeed, CancellationToken cancellationToken)
        {
            long previousTimestamp = 0;

            foreach (var action in recording.Actions)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                long delay = (long)((action.Timestamp - previousTimestamp) / playbackSpeed);
                if (delay > 0)
                    await Task.Delay((int)delay, cancellationToken);

                ExecuteAction(action);
                previousTimestamp = action.Timestamp;
            }
        }

        private void ExecuteAction(MacroAction action)
        {
            switch (action)
            {
                case KeyPressAction kpa:
                    SendKeyInput((ushort)kpa.KeyCode, KEYEVENTF_KEYDOWN);
                    break;

                case KeyReleaseAction kra:
                    SendKeyInput((ushort)kra.KeyCode, KEYEVENTF_KEYUP);
                    break;

                case MouseMoveAction mma:
                    uint x = (uint)(mma.X * 65536 / System.Windows.SystemParameters.PrimaryScreenWidth);
                    uint y = (uint)(mma.Y * 65536 / System.Windows.SystemParameters.PrimaryScreenHeight);
                    mouse_event(MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE, x, y, 0, UIntPtr.Zero);
                    break;

                case MouseDownAction mda:
                    if (mda.Button == "Left")
                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                    else if (mda.Button == "Right")
                        mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
                    break;

                case MouseUpAction mua:
                    if (mua.Button == "Left")
                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                    else if (mua.Button == "Right")
                        mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
                    break;

                case MouseWheelAction mwa:
                    mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)mwa.Delta, UIntPtr.Zero);
                    break;
            }
        }

        private void SendKeyInput(ushort keyCode, uint flags)
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = keyCode;
            inputs[0].u.ki.wScan = 0;
            inputs[0].u.ki.dwFlags = flags;
            inputs[0].u.ki.time = 0;
            inputs[0].u.ki.dwExtraInfo = GetMessageExtraInfo();

            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private void HandleMouseMovePlayback(object? sender, (int x, int y) pos)
        {
            if (!_isPlaying) return;

            // إذا تحرك الماوس أثناء التشغيل، أوقف التشغيل
            if (_lastMouseX != pos.x || _lastMouseY != pos.y)
            {
                OnMouseMovedDuringPlayback?.Invoke(this, EventArgs.Empty);
                Stop();
            }

            _lastMouseX = pos.x;
            _lastMouseY = pos.y;
        }

        public void Stop()
        {
            _isPlaying = false;
            _cancellationTokenSource?.Cancel();
        }
    }
}
