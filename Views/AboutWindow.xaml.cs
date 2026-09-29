using System.Windows;
using ScaleSwitcher.Models;
using ScaleSwitcher.Services;
using ScaleSwitcher.ViewModels;

namespace ScaleSwitcher.Views
{
    public partial class AboutWindow : Window
    {
        public string Description { get; }
        public string VersionLabel { get; }
        public string VersionText { get; }

        public AboutWindow(AppLocalization localization)
        {
            Description = localization.About_Description;
            VersionLabel = $"{localization.About_VersionLabel}:";
            VersionText = SettingsViewModel.ReadVersion(localization.Settings_VersionUnknown);

            InitializeComponent();
            Title = localization.About_Title;
            DataContext = this;

            SourceInitialized += (s, e) =>
            {
                try
                {
                    var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                    int isDark = ThemeManager.IsDarkTheme() ? 1 : 0;
                    NativeMethods.DwmSetWindowAttribute(
                        hwnd, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref isDark, sizeof(int));
                }
                catch
                {
                    // Ignore if DWM does not support this attribute.
                }
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
