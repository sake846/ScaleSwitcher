using System.Windows;
using ScaleSwitcher.ViewModels;

namespace ScaleSwitcher.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            
            var vm = new SettingsViewModel();
            vm.RequestClose += (result) =>
            {
                DialogResult = result;
                Close();
            };
            DataContext = vm;
        }
    }
}
