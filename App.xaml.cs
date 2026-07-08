using System;
using System.Drawing;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using ScaleSwitcher.Models;
using ScaleSwitcher.Services;
using ScaleSwitcher.Views;
using Forms = System.Windows.Forms;

namespace ScaleSwitcher
{
    public partial class App : System.Windows.Application
    {
        private Forms.NotifyIcon _notifyIcon = null!;
        private AppSettings _settings = null!;
        private ISettingsService _settingsService = null!;
        private int _currentScaleCycleIndex = 0;
        private bool _hasScaleCyclePosition;
        private Icon? _lightTrayIcon;
        private Icon? _darkTrayIcon;
        private KeyboardChordListener? _keyboardChordListener;
        private const string SingleInstanceMutexName = @"Local\ScaleSwitcher.SingleInstance";
        private System.Threading.Mutex? _singleInstanceMutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            if (!TryAcquireSingleInstanceMutex())
            {
                Shutdown();
                return;
            }

            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _settingsService = new SettingsService();
            _settings = _settingsService.Load();
            _lightTrayIcon = LoadIcon("pack://application:,,,/Assets/app.light.ico");
            _darkTrayIcon = LoadIcon("pack://application:,,,/Assets/app.dark.ico");

            var contextMenu = new Forms.ContextMenuStrip();
            contextMenu.Opening += (s, ev) => UpdateContextMenu();

            _notifyIcon = new Forms.NotifyIcon
            {
                Icon = GetTrayIconForCurrentTheme() ?? SystemIcons.Application,
                Visible = true,
                Text = "ScaleSwitcher",
                ContextMenuStrip = contextMenu
            };

            _notifyIcon.MouseClick += NotifyIcon_MouseClick;
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

            _keyboardChordListener = new KeyboardChordListener(_settings.KeyboardSwitchMode);
            _keyboardChordListener.ChordPressed += KeyboardChordListener_ChordPressed;

            UpdateContextMenu();
        }

        private void KeyboardChordListener_ChordPressed(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(() => CycleDpi(restoreCursorPosition: false));
        }

        private static Icon? LoadIcon(string iconUri)
        {
            try
            {
                var sri = System.Windows.Application.GetResourceStream(new Uri(iconUri, UriKind.Absolute));
                if (sri == null) return null;

                using var stream = sri.Stream;
                return new Icon(stream);
            }
            catch
            {
                return null;
            }
        }

        private Icon? GetTrayIconForCurrentTheme()
        {
            return IsSystemLightTheme() ? _lightTrayIcon ?? _darkTrayIcon : _darkTrayIcon ?? _lightTrayIcon;
        }

        private static bool IsSystemLightTheme()
        {
            const string personalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            const string valueName = "SystemUsesLightTheme";

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(personalizeKeyPath);
                if (key?.GetValue(valueName) is int value)
                {
                    return value != 0;
                }
            }
            catch
            {
                // Fall through to the default.
            }

            return true;
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.VisualStyle)
            {
                var icon = GetTrayIconForCurrentTheme();
                if (icon != null)
                {
                    _notifyIcon.Icon = icon;
                }
            }
        }

        private void NotifyIcon_MouseClick(object? sender, Forms.MouseEventArgs e)
        {
            if (e.Button == Forms.MouseButtons.Left)
            {
                CycleDpi(restoreCursorPosition: true);
            }
        }

        private void CycleDpi(bool restoreCursorPosition)
        {
            _settings = _settingsService.Load(); // reload in case it changed
            if (_settings.ActiveDpiPercentages.Count == 0) return;

            var displays = DisplayManager.GetDisplays();
            if (_settings.TargetMonitorIndex >= displays.Count) return;

            var targetDisplay = displays[_settings.TargetMonitorIndex];

            // GetDpiForMonitor can briefly return the old value after a DPI change.
            // Keep the last successful position for both mouse and keyboard input.
            if (!_hasScaleCyclePosition &&
                targetDisplay.CurrentDpi != null &&
                _settings.ActiveDpiPercentages.Contains(targetDisplay.CurrentDpi.Percentage))
            {
                _currentScaleCycleIndex = _settings.ActiveDpiPercentages.IndexOf(targetDisplay.CurrentDpi.Percentage);
            }

            // Use the last successfully applied position as the old DPI as well.
            // The value reported by Windows may still describe the previous cycle.
            if (_hasScaleCyclePosition &&
                _currentScaleCycleIndex >= 0 &&
                _currentScaleCycleIndex < _settings.ActiveDpiPercentages.Count)
            {
                int currentPercentage = _settings.ActiveDpiPercentages[_currentScaleCycleIndex];
                var currentDpi = targetDisplay.AvailableDpis.FirstOrDefault(d => d.Percentage == currentPercentage);
                if (currentDpi != null)
                {
                    targetDisplay.CurrentDpi = currentDpi;
                }
            }

            // Next index
            _currentScaleCycleIndex = (_currentScaleCycleIndex + 1) % _settings.ActiveDpiPercentages.Count;
            int nextPercentage = _settings.ActiveDpiPercentages[_currentScaleCycleIndex];

            // Find DpiInfo
            var nextDpi = targetDisplay.AvailableDpis.FirstOrDefault(d => d.Percentage == nextPercentage);
            if (nextDpi != null)
            {
                _hasScaleCyclePosition = DisplayManager.SetDpi(targetDisplay, nextDpi, restoreCursorPosition);
            }
            else
            {
                _hasScaleCyclePosition = false;
            }
        }

        private void UpdateContextMenu()
        {
            _settings = _settingsService.Load();

            var menu = _notifyIcon.ContextMenuStrip;
            if (menu == null) return;

            menu.Items.Clear();

            var displays = DisplayManager.GetDisplays();
            
            if (displays.Count == 1)
            {
                var display = displays[0];
                menu.Items.Add(CreateScaleSubMenu(display));
                menu.Items.Add(CreateResolutionSubMenu(display));
            }
            else
            {
                for (int i = 0; i < displays.Count; i++)
                {
                    var display = displays[i];
                    string displayName = GetMenuDisplayName(display, i);

                    var displayMenu = new Forms.ToolStripMenuItem(displayName);
                    displayMenu.DropDownItems.Add(CreateScaleSubMenu(display));
                    displayMenu.DropDownItems.Add(CreateResolutionSubMenu(display));

                    menu.Items.Add(displayMenu);
                }
            }

            if (displays.Count > 0)
            {
                menu.Items.Add(new Forms.ToolStripSeparator());
            }

            AddSystemMenuItems(menu);
        }

        private Forms.ToolStripMenuItem CreateScaleSubMenu(DisplayInfo display)
        {
            var scaleSubMenu = new Forms.ToolStripMenuItem(AppLocalization.Instance.Menu_Scale);
            foreach (var dpi in display.AvailableDpis.OrderBy(d => d.Percentage))
            {
                var dpiItem = new Forms.ToolStripMenuItem($"{dpi.Percentage}%")
                {
                    Checked = display.CurrentDpi?.Percentage == dpi.Percentage
                };
                dpiItem.Click += (s, ev) =>
                {
                    if (!DisplayManager.SetDpi(display, dpi))
                    {
                        _hasScaleCyclePosition = false;
                        return;
                    }

                    int selectedIndex = _settings.ActiveDpiPercentages.IndexOf(dpi.Percentage);
                    _hasScaleCyclePosition = selectedIndex >= 0;
                    if (_hasScaleCyclePosition)
                    {
                        _currentScaleCycleIndex = selectedIndex;
                    }
                };
                scaleSubMenu.DropDownItems.Add(dpiItem);
            }
            return scaleSubMenu;
        }

        private static Forms.ToolStripMenuItem CreateResolutionSubMenu(DisplayInfo display)
        {
            var resSubMenu = new Forms.ToolStripMenuItem(AppLocalization.Instance.Menu_Resolution);
            foreach (var res in display.AvailableResolutions)
            {
                var resItem = new Forms.ToolStripMenuItem($"{res.Width} x {res.Height}")
                {
                    Checked = display.CurrentResolution != null && display.CurrentResolution.Equals(res)
                };
                resItem.Click += (s, ev) => DisplayManager.SetResolution(display, res);
                resSubMenu.DropDownItems.Add(resItem);
            }
            return resSubMenu;
        }

        private void AddSystemMenuItems(Forms.ContextMenuStrip menu)
        {
            var runAtStartupItem = new Forms.ToolStripMenuItem(AppLocalization.Instance.Menu_RunAtStartup)
            {
                CheckOnClick = true,
                Checked = AutoStartService.IsEnabled()
            };
            runAtStartupItem.CheckedChanged += (s, e) =>
            {
                if (runAtStartupItem.Checked)
                    AutoStartService.Enable();
                else
                    AutoStartService.Disable();
            };
            menu.Items.Add(runAtStartupItem);

            var keyboardSwitchItem = new Forms.ToolStripMenuItem(AppLocalization.Instance.Menu_KeyboardSwitch)
            {
                Checked = _settings.KeyboardSwitchMode != KeyboardSwitchMode.Off
            };
            AddKeyboardSwitchMenuItem(keyboardSwitchItem, AppLocalization.Instance.Menu_KeyboardOff, KeyboardSwitchMode.Off);
            AddKeyboardSwitchMenuItem(keyboardSwitchItem, AppLocalization.Instance.Menu_KeyboardShift, KeyboardSwitchMode.Shift);
            AddKeyboardSwitchMenuItem(keyboardSwitchItem, AppLocalization.Instance.Menu_KeyboardControl, KeyboardSwitchMode.Control);
            AddKeyboardSwitchMenuItem(keyboardSwitchItem, AppLocalization.Instance.Menu_KeyboardAlt, KeyboardSwitchMode.Alt);
            menu.Items.Add(keyboardSwitchItem);

            var showDisplayInfoItem = new Forms.ToolStripMenuItem(AppLocalization.Instance.Menu_ShowDisplayInfo)
            {
                CheckOnClick = true,
                Checked = DisplayManager.DisplayInfoOsdsVisible
            };
            showDisplayInfoItem.CheckedChanged += (s, e) =>
            {
                if (showDisplayInfoItem.Checked)
                    DisplayManager.ShowDisplayInfoOsds();
                else
                    DisplayManager.HideDisplayInfoOsds();
            };
            menu.Items.Add(showDisplayInfoItem);

            var settingsItem = new Forms.ToolStripMenuItem(AppLocalization.Instance.Menu_Settings);
            settingsItem.Click += (s, e) => OpenSettings();
            menu.Items.Add(settingsItem);

            menu.Items.Add(new Forms.ToolStripSeparator());

            var exitItem = new Forms.ToolStripMenuItem(AppLocalization.Instance.Menu_Exit);
            exitItem.Click += (s, e) => ExitApp();
            menu.Items.Add(exitItem);
        }

        private void AddKeyboardSwitchMenuItem(
            Forms.ToolStripMenuItem parent,
            string text,
            KeyboardSwitchMode mode)
        {
            var item = new Forms.ToolStripMenuItem(text)
            {
                Checked = _settings.KeyboardSwitchMode == mode
            };
            item.Click += (s, e) =>
            {
                var newSettings = new AppSettings
                {
                    TargetMonitorIndex = _settings.TargetMonitorIndex,
                    ActiveDpiPercentages = _settings.ActiveDpiPercentages,
                    DisplayNumberSource = _settings.DisplayNumberSource,
                    UseCustomDisplayName = _settings.UseCustomDisplayName,
                    CustomDisplayName = _settings.CustomDisplayName,
                    KeyboardSwitchMode = mode
                };
                _settingsService.Save(newSettings);
                _settings = newSettings;

                if (_keyboardChordListener != null)
                {
                    _keyboardChordListener.Mode = mode;
                }
                UpdateContextMenu();
            };
            parent.DropDownItems.Add(item);
        }

        private string GetMenuDisplayName(DisplayInfo display, int displayIndex)
        {
            if (_settings.UseCustomDisplayName &&
                displayIndex == _settings.TargetMonitorIndex &&
                !string.IsNullOrWhiteSpace(_settings.CustomDisplayName))
            {
                return _settings.CustomDisplayName.Trim();
            }

            string displayName = $"{AppLocalization.Instance.DisplayPrefix} {display.SettingsDisplayNumber}";
            if (display.IsPrimary) displayName += " (Primary)";
            return displayName;
        }

        private void OpenSettings()
        {
            var settingsWindow = new SettingsWindow(_settingsService);
            if (settingsWindow.ShowDialog() == true)
            {
                _hasScaleCyclePosition = false;
                // Refresh context menu after settings changed
                UpdateContextMenu();
            }
        }

        private void ExitApp()
        {
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
            if (_keyboardChordListener != null)
            {
                _keyboardChordListener.ChordPressed -= KeyboardChordListener_ChordPressed;
                _keyboardChordListener.Dispose();
            }
            _lightTrayIcon?.Dispose();
            _darkTrayIcon?.Dispose();

            ReleaseSingleInstanceMutex();
            base.OnExit(e);
        }

        private bool TryAcquireSingleInstanceMutex()
        {
            _singleInstanceMutex = new System.Threading.Mutex(true, SingleInstanceMutexName, out var createdNew);
            if (createdNew)
            {
                return true;
            }

            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
            return false;
        }

        private void ReleaseSingleInstanceMutex()
        {
            if (_singleInstanceMutex != null)
            {
                try
                {
                    _singleInstanceMutex.ReleaseMutex();
                }
                catch
                {
                    // Ignore
                }
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
            }
        }
    }
}
