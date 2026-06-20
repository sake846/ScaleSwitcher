using System.Collections.Generic;

namespace ScaleSwitcher.Models
{
    public class AppSettings
    {
        public int TargetMonitorIndex { get; set; } = 0;
        public List<int> ActiveDpiPercentages { get; set; } = new() { 100, 200 };
    }
}
