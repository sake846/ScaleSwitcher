using System.Collections.Generic;

namespace ScaleSwitcher.Models
{
    public enum KeyboardSwitchMode
    {
        Off,
        Shift,
        Control,
        Alt
    }

    public static class DisplayNumberSources
    {
        public const string PathOrder = "PathOrder";
        public const string SourceId = "SourceId";
        public const string TargetId = "TargetId";
        public const string GdiDeviceName = "GdiDeviceName";

        public static readonly string[] All =
        {
            PathOrder,
            SourceId,
            TargetId,
            GdiDeviceName
        };
    }

    public class AppSettings
    {
        public int TargetMonitorIndex { get; init; } = 0;
        public List<int> ActiveDpiPercentages { get; init; } = new() { 100, 200 };
        public string DisplayNumberSource { get; init; } = DisplayNumberSources.TargetId;
        public bool UseCustomDisplayName { get; init; } = false;
        public string CustomDisplayName { get; init; } = string.Empty;
        public KeyboardSwitchMode KeyboardSwitchMode { get; init; } = KeyboardSwitchMode.Shift;
    }
}
