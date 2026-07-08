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
        }
    }
}
