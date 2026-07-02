using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ScaleSwitcher.Services
{
    internal sealed class ShiftChordListener : IDisposable
    {
        private const int WhKeyboardLl = 13;
        private const int WmKeyDown = 0x0100;
        private const int WmKeyUp = 0x0101;
        private const int WmSysKeyDown = 0x0104;
        private const int WmSysKeyUp = 0x0105;
        private const int VkLShift = 0xA0;
        private const int VkRShift = 0xA1;

        private readonly LowLevelKeyboardProc _hookCallback;
        private IntPtr _hook;
        private bool _leftShiftDown;
        private bool _rightShiftDown;
        private bool _chordTriggered;

        public ShiftChordListener()
        {
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

        public event EventHandler? ShiftChordPressed;

        private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0)
            {
                int message = unchecked((int)wParam.ToInt64());
                int virtualKey = Marshal.ReadInt32(lParam);
                bool isKeyDown = message is WmKeyDown or WmSysKeyDown;
                bool isKeyUp = message is WmKeyUp or WmSysKeyUp;

                if (virtualKey == VkLShift && (isKeyDown || isKeyUp))
                {
                    _leftShiftDown = isKeyDown;
                }
                else if (virtualKey == VkRShift && (isKeyDown || isKeyUp))
                {
                    _rightShiftDown = isKeyDown;
                }

                if (_leftShiftDown && _rightShiftDown)
                {
                    if (!_chordTriggered)
                    {
                        _chordTriggered = true;
                        ShiftChordPressed?.Invoke(this, EventArgs.Empty);
                    }
                }
                else
                {
                    _chordTriggered = false;
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
