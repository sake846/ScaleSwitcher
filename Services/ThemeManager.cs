using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using MediaColor = System.Windows.Media.Color;

namespace ScaleSwitcher.Services
{
    public static class ThemeManager
    {
        private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        public static bool IsDarkTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath);
                if (key?.GetValue("AppsUseLightTheme") is int appUsesLight)
                {
                    return appUsesLight == 0;
                }
                if (key?.GetValue("SystemUsesLightTheme") is int sysUsesLight)
                {
                    return sysUsesLight == 0;
                }
            }
            catch
            {
                // Fallback to light
            }

            return false;
        }

        public static void ApplyTheme(ResourceDictionary resources)
        {
            bool isDark = IsDarkTheme();

            if (isDark)
            {
                // Windows 11 Dark Mode Palette
                resources["Brush.Surface"]      = new SolidColorBrush(MediaColor.FromRgb(0x20, 0x20, 0x20));
                resources["Brush.SurfaceAlt"]   = new SolidColorBrush(MediaColor.FromRgb(0x2C, 0x2C, 0x2C));
                resources["Brush.Text"]         = new SolidColorBrush(MediaColor.FromRgb(0xFF, 0xFF, 0xFF));
                resources["Brush.TextMuted"]    = new SolidColorBrush(MediaColor.FromRgb(0xA6, 0xA6, 0xA6));
                resources["Brush.Border"]       = new SolidColorBrush(MediaColor.FromRgb(0x3E, 0x3E, 0x3E));
                resources["Brush.Accent"]       = new SolidColorBrush(MediaColor.FromRgb(0x00, 0x78, 0xD4));
                resources["Brush.AccentText"]   = new SolidColorBrush(MediaColor.FromRgb(0xFF, 0xFF, 0xFF));
                resources["Brush.Danger"]       = new SolidColorBrush(MediaColor.FromRgb(0xFF, 0x6B, 0x6B));
                resources["Brush.Overlay"]      = new SolidColorBrush(MediaColor.FromArgb(0xDC, 0x1A, 0x1A, 0x1A));
            }
            else
            {
                // Windows 11 Light Mode Palette
                resources["Brush.Surface"]      = new SolidColorBrush(MediaColor.FromRgb(0xFF, 0xFF, 0xFF));
                resources["Brush.SurfaceAlt"]   = new SolidColorBrush(MediaColor.FromRgb(0xF5, 0xF5, 0xF5));
                resources["Brush.Text"]         = new SolidColorBrush(MediaColor.FromRgb(0x1A, 0x1A, 0x1A));
                resources["Brush.TextMuted"]    = new SolidColorBrush(MediaColor.FromRgb(0x66, 0x66, 0x66));
                resources["Brush.Border"]       = new SolidColorBrush(MediaColor.FromRgb(0xDC, 0xDC, 0xDC));
                resources["Brush.Accent"]       = new SolidColorBrush(MediaColor.FromRgb(0x00, 0x67, 0xC0));
                resources["Brush.AccentText"]   = new SolidColorBrush(MediaColor.FromRgb(0xFF, 0xFF, 0xFF));
                resources["Brush.Danger"]       = new SolidColorBrush(MediaColor.FromRgb(0xC4, 0x2B, 0x1C));
                resources["Brush.Overlay"]      = new SolidColorBrush(MediaColor.FromArgb(0xCC, 0x00, 0x00, 0x00));
            }
        }
    }
}
