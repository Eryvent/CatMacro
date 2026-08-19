using System.Runtime.InteropServices;

namespace CatMacro.Services
{
    public class KeyboardMouseHooker
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MOUSEWHEEL = 0x020A;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, Delegate lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr _keyboardHookHandle = IntPtr.Zero;
        private IntPtr _mouseHookHandle = IntPtr.Zero;

        private KeyboardHookProc _keyboardProc;
        private MouseHookProc _mouseProc;

        public event EventHandler<int>? OnKeyDown;
        public event EventHandler<int>? OnKeyUp;
        public event EventHandler<(int, int)>? OnMouseMove;
        public event EventHandler<string>? OnMouseDown;
        public event EventHandler<string>? OnMouseUp;
        public event EventHandler<int>? OnMouseWheel;

        public KeyboardMouseHooker()
        {
            _keyboardProc = KeyboardCallback;
            _mouseProc = MouseCallback;
        }

        public void Install()
        {
            IntPtr moduleHandle = GetModuleHandle("");
            _keyboardHookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, moduleHandle, 0);
            _mouseHookHandle = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, moduleHandle, 0);
        }

        public void Uninstall()
        {
            if (_keyboardHookHandle != IntPtr.Zero)
                UnhookWindowsHookEx(_keyboardHookHandle);
            if (_mouseHookHandle != IntPtr.Zero)
                UnhookWindowsHookEx(_mouseHookHandle);
        }

        private IntPtr KeyboardCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (wParam == (IntPtr)WM_KEYDOWN)
                    OnKeyDown?.Invoke(this, vkCode);
                else if (wParam == (IntPtr)WM_KEYUP)
                    OnKeyUp?.Invoke(this, vkCode);
            }
            return CallNextHookEx(_keyboardHookHandle, nCode, wParam, lParam);
        }

        private IntPtr MouseCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var structData = Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                if (structData is MSLLHOOKSTRUCT hookStruct)
                {
                    switch ((int)wParam)
                    {
                        case WM_MOUSEMOVE:
                            OnMouseMove?.Invoke(this, (hookStruct.pt.x, hookStruct.pt.y));
                            break;
                        case WM_LBUTTONDOWN:
                            OnMouseDown?.Invoke(this, "Left");
                            break;
                        case WM_LBUTTONUP:
                            OnMouseUp?.Invoke(this, "Left");
                            break;
                        case WM_RBUTTONDOWN:
                            OnMouseDown?.Invoke(this, "Right");
                            break;
                        case WM_RBUTTONUP:
                            OnMouseUp?.Invoke(this, "Right");
                            break;
                        case WM_MOUSEWHEEL:
                            int delta = (int)hookStruct.mouseData >> 16;
                            OnMouseWheel?.Invoke(this, delta > 0 ? 1 : -1);
                            break;
                    }
                }
            }
            return CallNextHookEx(_mouseHookHandle, nCode, wParam, lParam);
        }
    }
}
