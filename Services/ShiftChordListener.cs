using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using ScaleSwitcher.Models;

namespace ScaleSwitcher.Services
{
    internal sealed class KeyboardChordListener : IDisposable
    {
        private const int WhKeyboardLl = 13;
        private const int WmKeyDown = 0x0100;
        private const int WmKeyUp = 0x0101;
        private const int WmSysKeyDown = 0x0104;
        private const int WmSysKeyUp = 0x0105;
        private const int VkLShift = 0xA0;
        private const int VkRShift = 0xA1;
        private const int VkLControl = 0xA2;
        private const int VkRControl = 0xA3;
        private const int VkLMenu = 0xA4;
        private const int VkRMenu = 0xA5;

        private readonly LowLevelKeyboardProc _hookCallback;
        private IntPtr _hook;
        private bool _chordTriggered;
        private KeyboardSwitchMode _mode;

        public KeyboardChordListener(KeyboardSwitchMode mode)
        {
            Mode = mode;
            _hookCallback = HookCallback;
            IntPtr moduleHandle = GetModuleHandle(null);
            if (moduleHandle == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            _hook = SetWindowsHookEx(WhKeyboardLl, _hookCallback, moduleHandle, 0);
            if (_hook == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        public KeyboardSwitchMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                _chordTriggered = false;
            }
        }

        public event EventHandler? ChordPressed;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0)
            {
                int message = unchecked((int)wParam.ToInt64());
                int virtualKey = Marshal.ReadInt32(lParam);
                bool isKeyDown = message is WmKeyDown or WmSysKeyDown;
                bool isKeyUp = message is WmKeyUp or WmSysKeyUp;

                var (leftKey, rightKey) = Mode switch
                {
                    KeyboardSwitchMode.Shift => (VkLShift, VkRShift),
                    KeyboardSwitchMode.Control => (VkLControl, VkRControl),
                    KeyboardSwitchMode.Alt => (VkLMenu, VkRMenu),
                    _ => (0, 0)
                };

                if (leftKey != 0 &&
                    (virtualKey == leftKey || virtualKey == rightKey) &&
                    (isKeyDown || isKeyUp))
                {
                    bool leftDown = (GetAsyncKeyState(leftKey) & 0x8000) != 0;
                    bool rightDown = (GetAsyncKeyState(rightKey) & 0x8000) != 0;

                    if (virtualKey == leftKey) leftDown = isKeyDown;
                    if (virtualKey == rightKey) rightDown = isKeyDown;

                    if (leftDown && rightDown)
                    {
                        if (!_chordTriggered)
                        {
                            _chordTriggered = true;
                            ChordPressed?.Invoke(this, EventArgs.Empty);
                        }
                    }
                    else
                    {
                        _chordTriggered = false;
                    }
                }
            }

            return CallNextHookEx(_hook, code, wParam, lParam);
        }

        public void Dispose()
        {
            if (_hook == IntPtr.Zero)
            {
                return;
            }

            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
            GC.SuppressFinalize(this);
        }

        private delegate IntPtr LowLevelKeyboardProc(int code, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? moduleName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
            int hookId,
            LowLevelKeyboardProc callback,
            IntPtr moduleHandle,
            uint threadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(
            IntPtr hook,
            int code,
            IntPtr wParam,
            IntPtr lParam);
    }
}
