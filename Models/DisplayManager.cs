using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Microsoft.Win32;

namespace ScaleSwitcher.Models
{
    public static class DisplayManager
    {
        private const int DefaultDpi = 96;
        private const int CursorRestoreDelayMs = 1500;
        private static readonly int[] DpiArray = { 100, 125, 150, 175, 200, 225, 250, 300, 350, 400, 450, 500 };
        private static readonly List<ScaleSwitcher.Views.OsdWindow> DisplayInfoOsds = new();

        public static bool DisplayInfoOsdsVisible => DisplayInfoOsds.Count > 0;

        public static List<DisplayInfo> GetDisplays()
        {
            var displays = new List<DisplayInfo>();
            var settingsDisplayNumbers = GetWindowsDisplayNumbers(DisplayNumberSources.TargetId);
            int index = 0;

            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref NativeMethods.Rect lprcMonitor, IntPtr dwData)
            {
                var mi = new NativeMethods.MONITORINFOEX();
                mi.cbSize = Marshal.SizeOf(typeof(NativeMethods.MONITORINFOEX));
                if (NativeMethods.GetMonitorInfo(hMonitor, ref mi))
                {
                    bool hasConfig = settingsDisplayNumbers.TryGetValue(mi.szDevice, out var config);
                    var info = new DisplayInfo
                    {
                        MonitorIndex = index,
                        SettingsDisplayNumber = hasConfig ? config.DisplayNumber : index + 1,
                        HardwareId = hasConfig ? config.HardwareId : "",
                        MonitorHandle = hMonitor,
                        DeviceName = mi.szDevice,
                        IsPrimary = (mi.dwFlags & 1) != 0 // MONITORINFOF_PRIMARY
                    };

                    PopulateResolutions(info);
                    PopulateDpis(info);
                    displays.Add(info);
                    index++;
                }
                return true;
            }, IntPtr.Zero);

            return displays;
        }

        private static Dictionary<string, (int DisplayNumber, string HardwareId)> GetWindowsDisplayNumbers(string displayNumberSource)
        {
            var result = new Dictionary<string, (int DisplayNumber, string HardwareId)>(StringComparer.OrdinalIgnoreCase);

            if (NativeMethods.GetDisplayConfigBufferSizes(NativeMethods.QDC_ONLY_ACTIVE_PATHS, out uint pathCount, out uint modeCount) != 0)
            {
                return result;
            }

            var paths = new NativeMethods.DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new NativeMethods.DISPLAYCONFIG_MODE_INFO[modeCount];
            if (NativeMethods.QueryDisplayConfig(NativeMethods.QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero) != 0)
            {
                return result;
            }

            for (int i = 0; i < pathCount; i++)
            {
                ProcessDisplayConfigPath(i, paths[i], displayNumberSource, result);
            }

            return result;
        }

        private static void ProcessDisplayConfigPath(
            int pathIndex,
            NativeMethods.DISPLAYCONFIG_PATH_INFO path,
            string displayNumberSource,
            Dictionary<string, (int DisplayNumber, string HardwareId)> result)
        {
            var sourceName = new NativeMethods.DISPLAYCONFIG_SOURCE_DEVICE_NAME
            {
                header = new NativeMethods.DISPLAYCONFIG_DEVICE_INFO_HEADER
                {
                    type = NativeMethods.DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME,
                    size = (uint)Marshal.SizeOf<NativeMethods.DISPLAYCONFIG_SOURCE_DEVICE_NAME>(),
                    adapterId = path.sourceInfo.adapterId,
                    id = path.sourceInfo.id
                },
                viewGdiDeviceName = new string('\0', 32)
            };

            string sourceDeviceName = "";
            if (NativeMethods.DisplayConfigGetDeviceInfo(ref sourceName) == 0)
            {
                sourceDeviceName = sourceName.viewGdiDeviceName.TrimEnd('\0');
            }

            int selectedDisplayNumber = ResolveDisplayNumber(displayNumberSource, pathIndex, path, sourceDeviceName);

            var targetName = new NativeMethods.DISPLAYCONFIG_TARGET_DEVICE_NAME
            {
                header = new NativeMethods.DISPLAYCONFIG_DEVICE_INFO_HEADER
                {
                    type = NativeMethods.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME,
                    size = (uint)Marshal.SizeOf<NativeMethods.DISPLAYCONFIG_TARGET_DEVICE_NAME>(),
                    adapterId = path.targetInfo.adapterId,
                    id = path.targetInfo.id
                },
                monitorFriendlyDeviceName = new string('\0', 64),
                monitorDevicePath = new string('\0', 128)
            };

            string targetDevicePath = "";
            if (NativeMethods.DisplayConfigGetDeviceInfo(ref targetName) == 0)
            {
                targetDevicePath = targetName.monitorDevicePath.TrimEnd('\0');
            }

            string hardwareId = "";
            if (!string.IsNullOrEmpty(targetDevicePath))
            {
                string[] parts = targetDevicePath.Split('#');
                if (parts.Length > 1)
                {
                    hardwareId = parts[1];
                }
            }

            if (!string.IsNullOrWhiteSpace(sourceDeviceName))
            {
                result.TryAdd(sourceDeviceName, (selectedDisplayNumber, hardwareId));
            }
        }

        private static int ResolveDisplayNumber(string displayNumberSource, int pathIndex, NativeMethods.DISPLAYCONFIG_PATH_INFO path, string sourceDeviceName)
        {
            return displayNumberSource switch
            {
                DisplayNumberSources.PathOrder => pathIndex + 1,
                DisplayNumberSources.TargetId => (int)path.targetInfo.id + 1,
                DisplayNumberSources.GdiDeviceName => TryGetGdiDeviceNumber(sourceDeviceName) ?? pathIndex + 1,
                _ => (int)path.sourceInfo.id + 1
            };
        }

        private static int? TryGetGdiDeviceNumber(string sourceDeviceName)
        {
            const string prefix = @"\\.\DISPLAY";
            if (!sourceDeviceName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return int.TryParse(sourceDeviceName[prefix.Length..], out int displayNumber)
                ? displayNumber
                : null;
        }

        private static void PopulateResolutions(DisplayInfo info)
        {
            var resolutions = new HashSet<ResolutionInfo>();
            var devMode = new NativeMethods.DEVMODE();
            devMode.dmSize = (short)Marshal.SizeOf(typeof(NativeMethods.DEVMODE));

            // Get available
            int modeNum = 0;
            while (NativeMethods.EnumDisplaySettings(info.DeviceName, modeNum, ref devMode))
            {
                resolutions.Add(new ResolutionInfo { Width = devMode.dmPelsWidth, Height = devMode.dmPelsHeight });
                modeNum++;
            }
            
            info.AvailableResolutions = resolutions.OrderByDescending(r => r.Width).ThenByDescending(r => r.Height).ToList();

            // Get current
            if (NativeMethods.EnumDisplaySettings(info.DeviceName, NativeMethods.ENUM_CURRENT_SETTINGS, ref devMode))
            {
                info.CurrentResolution = new ResolutionInfo { Width = devMode.dmPelsWidth, Height = devMode.dmPelsHeight };
            }
        }

        private static readonly Dictionary<string, int> RecommendedDpiCache = new();

        private static int? GetRelativeIndexFromRegistry(string hardwareId)
        {
            if (string.IsNullOrWhiteSpace(hardwareId)) return null;
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers\ScaleFactors"))
                {
                    if (key != null)
                    {
                        foreach (var subkeyName in key.GetSubKeyNames())
                        {
                            if (subkeyName.StartsWith(hardwareId, StringComparison.OrdinalIgnoreCase))
                            {
                                using (var subkey = key.OpenSubKey(subkeyName))
                                {
                                    if (subkey != null)
                                    {
                                        var val = subkey.GetValue("DpiValue");
                                        if (val != null)
                                        {
                                            return Convert.ToInt32(val);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore registry read errors
            }
            return null;
        }

        private static void PopulateDpis(DisplayInfo info)
        {
            // Get current DPI percentage
            NativeMethods.GetDpiForMonitor(info.MonitorHandle, 0, out uint dpiX, out _);
            int currentPercentage = (int)(dpiX * 100 / DefaultDpi);

            int recommendedPercentage;
            if (RecommendedDpiCache.TryGetValue(info.DeviceName, out int cachedRecommended))
            {
                recommendedPercentage = cachedRecommended;
            }
            else
            {
                int currentRelativeIndex = 0;
                int? registryRelativeIndex = GetRelativeIndexFromRegistry(info.HardwareId);
                if (registryRelativeIndex.HasValue)
                {
                    currentRelativeIndex = registryRelativeIndex.Value;
                }
                else
                {
                    // Fallback to SPI_GETLOGICALDPIOVERRIDE
                    IntPtr ptr = Marshal.AllocHGlobal(4);
                    try
                    {
                        if (NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETLOGICALDPIOVERRIDE, info.MonitorIndex, ptr, 0))
                        {
                            currentRelativeIndex = Marshal.ReadInt32(ptr);
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(ptr);
                    }
                }

                int arrayIndex = Array.IndexOf(DpiArray, currentPercentage);
                if (arrayIndex != -1)
                {
                    int recommendedArrayIndex = arrayIndex - currentRelativeIndex;
                    recommendedArrayIndex = Math.Clamp(recommendedArrayIndex, 0, DpiArray.Length - 1);
                    recommendedPercentage = DpiArray[recommendedArrayIndex];
                    RecommendedDpiCache[info.DeviceName] = recommendedPercentage;
                }
                else
                {
                    recommendedPercentage = currentPercentage; // Fallback
                }
            }

            int recommendedIdx = Array.IndexOf(DpiArray, recommendedPercentage);
            if (recommendedIdx == -1) recommendedIdx = 2; // Fallback to 150%

            int currentIdx = Array.IndexOf(DpiArray, currentPercentage);
            info.CurrentDpi = new DpiInfo
            {
                Percentage = currentPercentage,
                RelativeIndex = currentIdx != -1 ? currentIdx - recommendedIdx : 0
            };

            if (currentIdx == -1)
            {
                info.AvailableDpis.Add(info.CurrentDpi);
                return;
            }

            // Populate available DPIs based on the static recommended index
            for (int i = 0; i < DpiArray.Length; i++)
            {
                int relIndex = i - recommendedIdx;
                // Avoid too extreme negative relatives. Min relative is usually -3. Max is usually 4.
                if (relIndex >= -3 && relIndex <= 4)
                {
                    info.AvailableDpis.Add(new DpiInfo { Percentage = DpiArray[i], RelativeIndex = relIndex });
                }
            }
        }

        public static bool SetResolution(DisplayInfo info, ResolutionInfo res)
        {
            var oldPos = System.Windows.Forms.Cursor.Position;
            var oldScreenObj = System.Windows.Forms.Screen.FromPoint(oldPos);
            var oldScreen = oldScreenObj.Bounds;
            int offsetX = oldScreen.Right - oldPos.X;
            int offsetY = oldScreen.Bottom - oldPos.Y;
            string deviceName = oldScreenObj.DeviceName;

            string oldResStr = info.CurrentResolution != null ? $"{info.CurrentResolution.Width}x{info.CurrentResolution.Height}" : "";
            string newResStr = $"{res.Width}x{res.Height}";
            var osd = ShowOsd($"{oldResStr} → {newResStr}", info);

            var devMode = new NativeMethods.DEVMODE();
            devMode.dmSize = (short)Marshal.SizeOf(typeof(NativeMethods.DEVMODE));
            if (NativeMethods.EnumDisplaySettings(info.DeviceName, NativeMethods.ENUM_CURRENT_SETTINGS, ref devMode))
            {
                devMode.dmPelsWidth = res.Width;
                devMode.dmPelsHeight = res.Height;
                devMode.dmFields = NativeMethods.DM_PELSWIDTH | NativeMethods.DM_PELSHEIGHT;

                int result = NativeMethods.ChangeDisplaySettingsEx(info.DeviceName, ref devMode, IntPtr.Zero, 0, IntPtr.Zero);
                if (result == NativeMethods.DISP_CHANGE_SUCCESSFUL)
                {
                    // 解像度変更ではDPIによるアイコンのピクセルサイズは変わらないため、そのままのオフセットを渡す
                    RestoreCursorPosition(offsetX, offsetY, deviceName, osd);
                    return true;
                }
            }

            if (osd != null && System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => osd.CloseWithFade());
            }
            return false;
        }

        public static bool SetDpi(DisplayInfo info, DpiInfo dpi, bool restoreCursorPosition = true)
        {
            var oldPos = System.Windows.Forms.Cursor.Position;
            var oldScreenObj = System.Windows.Forms.Screen.FromPoint(oldPos);
            var oldScreen = oldScreenObj.Bounds;
            int offsetX = oldScreen.Right - oldPos.X;
            int offsetY = oldScreen.Bottom - oldPos.Y;
            string deviceName = oldScreenObj.DeviceName;

            string oldDpiStr = info.CurrentDpi != null ? $"{info.CurrentDpi.Percentage}%" : "";
            string newDpiStr = $"{dpi.Percentage}%";
            var osd = ShowOsd($"{oldDpiStr} → {newDpiStr}", info);

            bool success = NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETLOGICALDPIOVERRIDE, dpi.RelativeIndex, (IntPtr)info.MonitorIndex, NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDCHANGE);
            if (success)
            {
                // スケーリング変更時は、タスクバー等のサイズが変わるためDPIの比率をオフセットにかける
                double ratio = info.CurrentDpi != null && info.CurrentDpi.Percentage > 0 
                                ? (double)dpi.Percentage / info.CurrentDpi.Percentage 
                                : 1.0;
                int newOffsetX = (int)Math.Round(offsetX * ratio);
                int newOffsetY = (int)Math.Round(offsetY * ratio);
                if (restoreCursorPosition)
                {
                    RestoreCursorPosition(newOffsetX, newOffsetY, deviceName, osd);
                }
                else
                {
                    CloseOsdAfterDisplayChange(osd);
                }
            }
            else
            {
                if (osd != null && System.Windows.Application.Current != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => osd.CloseWithFade());
                }
            }
            return success;
        }

        public static void ShowDisplayInfoOsds()
        {
            HideDisplayInfoOsds();

            var displays = GetDisplays();
            foreach (var display in displays)
            {
                var osd = ShowOsd(BuildDisplayInfoMessage(display), display, 30, captureMouse: false, hideCursor: false);
                if (osd != null)
                {
                    DisplayInfoOsds.Add(osd);
                }
            }
        }

        public static void HideDisplayInfoOsds()
        {
            if (DisplayInfoOsds.Count == 0) return;

            if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var osd in DisplayInfoOsds)
                    {
                        osd.CloseWithFade();
                    }
                    DisplayInfoOsds.Clear();
                });
            }
            else
            {
                DisplayInfoOsds.Clear();
            }
        }

        private static string BuildDisplayInfoMessage(DisplayInfo display)
        {
            string resolution = display.CurrentResolution != null
                ? $"{display.CurrentResolution.Width}x{display.CurrentResolution.Height}"
                : "unknown";
            string dpi = display.CurrentDpi != null ? $"{display.CurrentDpi.Percentage}%" : "unknown";

            return string.Join(Environment.NewLine,
                $"Display: {display.SettingsDisplayNumber}",
                $"Primary: {display.IsPrimary}",
                $"Resolution: {resolution}",
                $"Scale: {dpi}");
        }

        private static ScaleSwitcher.Views.OsdWindow? ShowOsd(string message, DisplayInfo display)
        {
            return ShowOsd(message, display, 48, captureMouse: true, hideCursor: true);
        }

        private static ScaleSwitcher.Views.OsdWindow? ShowOsd(string message, DisplayInfo display, double fontSize, bool captureMouse, bool hideCursor)
        {
            ScaleSwitcher.Views.OsdWindow? osd = null;
            if (System.Windows.Application.Current != null && System.Windows.Application.Current.Dispatcher != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    osd = new ScaleSwitcher.Views.OsdWindow(message, fontSize, hideCursor);

                    osd.Show();
                    PositionOsdOnDisplay(osd, display);

                    if (captureMouse)
                    {
                        // Cursor="None" を確実に効かせるため、マウスをキャプチャする
                        osd.CaptureMouse();
                    }
                });
            }
            return osd;
        }

        private static void PositionOsdOnDisplay(ScaleSwitcher.Views.OsdWindow osd, DisplayInfo display)
        {
            var mi = new NativeMethods.MONITORINFOEX();
            mi.cbSize = Marshal.SizeOf(typeof(NativeMethods.MONITORINFOEX));

            NativeMethods.Rect rect;
            if (NativeMethods.GetMonitorInfo(display.MonitorHandle, ref mi))
            {
                rect = mi.rcMonitor;
            }
            else
            {
                var screen = System.Windows.Forms.Screen.AllScreens.FirstOrDefault(s => s.DeviceName == display.DeviceName)
                             ?? System.Windows.Forms.Screen.PrimaryScreen;
                if (screen == null) return;

                rect = new NativeMethods.Rect
                {
                    left = screen.Bounds.Left,
                    top = screen.Bounds.Top,
                    right = screen.Bounds.Right,
                    bottom = screen.Bounds.Bottom
                };
            }

            var hwnd = new WindowInteropHelper(osd).Handle;
            if (hwnd == IntPtr.Zero) return;

            NativeMethods.SetWindowPos(
                hwnd,
                NativeMethods.HWND_TOPMOST,
                rect.left,
                rect.top,
                rect.right - rect.left,
                rect.bottom - rect.top,
                NativeMethods.SWP_NOACTIVATE);
        }

        private static async void RestoreCursorPosition(int offsetX, int offsetY, string deviceName, ScaleSwitcher.Views.OsdWindow? osd)
        {
            // OSによる画面レイアウトの再構成とマウスの中央リセット処理が完了するのを少し待つ
            await System.Threading.Tasks.Task.Delay(CursorRestoreDelayMs);

            var screenObj = System.Windows.Forms.Screen.AllScreens.FirstOrDefault(s => s.DeviceName == deviceName) 
                         ?? System.Windows.Forms.Screen.PrimaryScreen;

            if (screenObj != null)
            {
                var newScreen = screenObj.Bounds;

                int newX = newScreen.Right - offsetX;
                int newY = newScreen.Bottom - offsetY;

                newX = Math.Max(newScreen.Left, Math.Min(newX, newScreen.Right - 1));
                newY = Math.Max(newScreen.Top, Math.Min(newY, newScreen.Bottom - 1));

                System.Windows.Forms.Cursor.Position = new System.Drawing.Point(newX, newY);
            }

            if (osd != null && System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    osd.CloseWithFade();
                });
            }
        }

        private static async void CloseOsdAfterDisplayChange(ScaleSwitcher.Views.OsdWindow? osd)
        {
            await System.Threading.Tasks.Task.Delay(CursorRestoreDelayMs);

            if (osd != null && System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => osd.CloseWithFade());
            }
        }
    }
}
