using System.Windows;
using ScaleSwitcher.ViewModels;
using ScaleSwitcher.Services;

namespace ScaleSwitcher.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow(ISettingsService settingsService, AppLocalization localization)
        {
            InitializeComponent();
            
            var vm = new SettingsViewModel(settingsService, localization);
            vm.RequestClose += (result) =>
            {
                DialogResult = result;
                Close();
            };
            DataContext = vm;

            SourceInitialized += (s, e) =>
            {
                try
                {
                    var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                    int isDark = ThemeManager.IsDarkTheme() ? 1 : 0;
                    ScaleSwitcher.Models.NativeMethods.DwmSetWindowAttribute(
                        hwnd,
                        ScaleSwitcher.Models.NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE,
                        ref isDark,
                        sizeof(int));
                }
                catch
                {
                    // Ignore if DWM attribute is not supported
                }
            };
        }
    }
}
