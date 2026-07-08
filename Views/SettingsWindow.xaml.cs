using System.Windows;
using ScaleSwitcher.ViewModels;
using ScaleSwitcher.Services;

namespace ScaleSwitcher.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow(ISettingsService settingsService)
        {
            InitializeComponent();
            
            var vm = new SettingsViewModel(settingsService);
            vm.RequestClose += (result) =>
            {
                DialogResult = result;
                Close();
            };
            DataContext = vm;
        }
    }
}
