using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CatMacro.Models;

namespace CatMacro.Services
{
    public class MacroRecorderService
    {
        private readonly KeyboardMouseHooker _hooker;
        private List<MacroAction> _actions = new();
        private long _startTime = 0;
        private bool _isRecording = false;
        private int _lastMouseX = 0;
        private int _lastMouseY = 0;

        public event EventHandler<string>? OnActionRecorded;
        public event EventHandler<int>? OnCountdown;
        public event EventHandler? OnMouseMoved;

        public MacroRecorderService()
        {
            _hooker = new KeyboardMouseHooker();
            _hooker.OnKeyDown += HandleKeyDown;
            _hooker.OnKeyUp += HandleKeyUp;
            _hooker.OnMouseMove += HandleMouseMove;
            _hooker.OnMouseDown += HandleMouseDown;
            _hooker.OnMouseUp += HandleMouseUp;
            _hooker.OnMouseWheel += HandleMouseWheel;
        }

        public async Task StartWithCountdown()
        {
            _hooker.Install();
            _actions.Clear();
            _startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            _isRecording = true;
            _lastMouseX = 0;
            _lastMouseY = 0;

            // عد تنازلي من 5
            for (int i = 5; i > 0; i--)
            {
                OnCountdown?.Invoke(this, i);
                await Task.Delay(1000);
            }

            OnCountdown?.Invoke(this, 0); // انتهى العد
        }

        public void Start()
        {
            _actions.Clear();
            _startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            _isRecording = true;
            _hooker.Install();
        }

        public void Stop()
        {
            _isRecording = false;
            _hooker.Uninstall();
        }

        public RecordingData GetRecording()
        {
            return new RecordingData
            {
                Name = "Recording",
                Duration = DateTimeOffset.Now.ToUnixTimeMilliseconds() - _startTime,
                Actions = _actions
            };
        }

        private void HandleKeyDown(object? sender, int keyCode)
        {
            if (!_isRecording) return;
            var action = new KeyPressAction(keyCode, GetKeyName(keyCode));
            action.Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds() - _startTime;
            _actions.Add(action);
            OnActionRecorded?.Invoke(this, $"▼ Press: {action.KeyName}");
        }

        private void HandleKeyUp(object? sender, int keyCode)
        {
            if (!_isRecording) return;
            var action = new KeyReleaseAction(keyCode, GetKeyName(keyCode));
            action.Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds() - _startTime;
            _actions.Add(action);
            OnActionRecorded?.Invoke(this, $"▲ Release: {action.KeyName}");
        }

        private void HandleMouseMove(object? sender, (int x, int y) pos)
        {
            if (!_isRecording) return;

            // لا نسجل حركة الماوس في التسجيل
            // فقط الضغطات والرفعات
        }

        private void HandleMouseDown(object? sender, string button)
        {
            if (!_isRecording) return;
            var action = new MouseDownAction(button);
            action.Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds() - _startTime;
            _actions.Add(action);
            OnActionRecorded?.Invoke(this, $"🖱 {button} Click");
        }

        private void HandleMouseUp(object? sender, string button)
        {
            if (!_isRecording) return;
            var action = new MouseUpAction(button);
            action.Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds() - _startTime;
            _actions.Add(action);
            OnActionRecorded?.Invoke(this, $"🖱 {button} Release");
        }

        private void HandleMouseWheel(object? sender, int delta)
        {
            if (!_isRecording) return;
            var action = new MouseWheelAction(delta);
            action.Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds() - _startTime;
            _actions.Add(action);
            OnActionRecorded?.Invoke(this, $"🎡 Scroll {(delta > 0 ? "Up" : "Down")}");
        }

        private string GetKeyName(int keyCode)
        {
            return keyCode switch
            {
                0x08 => "Backspace",
                0x09 => "Tab",
                0x0D => "Enter",
                0x10 => "Shift",
                0x11 => "Ctrl",
                0x12 => "Alt",
                0x1B => "Escape",
                0x20 => "Space",
                0x41 => "A", 0x42 => "B", 0x43 => "C", 0x44 => "D", 0x45 => "E",
                0x46 => "F", 0x47 => "G", 0x48 => "H", 0x49 => "I", 0x4A => "J",
                0x4B => "K", 0x4C => "L", 0x4D => "M", 0x4E => "N", 0x4F => "O",
                0x50 => "P", 0x51 => "Q", 0x52 => "R", 0x53 => "S", 0x54 => "T",
                0x55 => "U", 0x56 => "V", 0x57 => "W", 0x58 => "X", 0x59 => "Y",
                0x5A => "Z",
                0x30 => "0", 0x31 => "1", 0x32 => "2", 0x33 => "3", 0x34 => "4",
                0x35 => "5", 0x36 => "6", 0x37 => "7", 0x38 => "8", 0x39 => "9",
                0x25 => "Left", 0x26 => "Up", 0x27 => "Right", 0x28 => "Down",
                _ => $"Key({keyCode})"
            };
        }
    }
}
