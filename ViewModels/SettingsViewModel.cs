using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ScaleSwitcher.Models;
using ScaleSwitcher.Services;

namespace ScaleSwitcher.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();
        public void Execute(object? parameter) => _execute();
    }

    public class ScaleOptionViewModel : ViewModelBase
    {
        private bool _isSelected;

        public int Percentage { get; set; }
        public string DisplayText => $"{Percentage}%";

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    public class DisplayItemViewModel
    {
        public DisplayInfo Display { get; }
        public string DisplayName { get; }
        public int Index { get; }

        public DisplayItemViewModel(DisplayInfo display, int index)
        {
            Display = display;
            Index = index;
            
            string name = $"{AppLocalization.Instance.DisplayPrefix} {display.SettingsDisplayNumber}";
            if (display.IsPrimary) name += " (Primary)";
            DisplayName = name;
        }
    }

    public class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly AppSettings _settings;
        private readonly List<DisplayInfo> _rawDisplays;
        private DisplayItemViewModel? _selectedDisplay;
        private bool _useCustomDisplayName;
        private string _customDisplayName = string.Empty;
        private ObservableCollection<ScaleOptionViewModel> _scaleOptions = new();

        public event Action<bool>? RequestClose;

        public string Title => AppLocalization.Instance.Settings_Title;
        public string TargetDisplayHeader => AppLocalization.Instance.Settings_TargetDisplay;
        public string UseCustomDisplayNameText => AppLocalization.Instance.Settings_UseCustomDisplayName;
        public string CustomDisplayNameHeader => AppLocalization.Instance.Settings_CustomDisplayName;
        public string ScalesHeader => AppLocalization.Instance.Settings_Scales;
        public string SaveButtonText => AppLocalization.Instance.Settings_Save;

        public List<DisplayItemViewModel> Displays { get; }
        public ICommand SaveCommand { get; }

        public DisplayItemViewModel? SelectedDisplay
        {
            get => _selectedDisplay;
            set
            {
                if (SetProperty(ref _selectedDisplay, value))
                {
                    PopulateScales(value?.Display);
                    OnPropertyChanged(nameof(DisplayNameText));
                }
            }
        }

        public ObservableCollection<ScaleOptionViewModel> ScaleOptions
        {
            get => _scaleOptions;
            set => SetProperty(ref _scaleOptions, value);
        }

        public bool UseCustomDisplayName
        {
            get => _useCustomDisplayName;
            set
            {
                if (SetProperty(ref _useCustomDisplayName, value))
                {
                    if (value && string.IsNullOrWhiteSpace(CustomDisplayName))
                    {
                        CustomDisplayName = SelectedDisplay?.DisplayName ?? string.Empty;
                    }
                    OnPropertyChanged(nameof(DisplayNameText));
                }
            }
        }

        public string CustomDisplayName
        {
            get => _customDisplayName;
            set
            {
                if (SetProperty(ref _customDisplayName, value))
                {
                    OnPropertyChanged(nameof(DisplayNameText));
                }
            }
        }

        public string DisplayNameText
        {
            get => UseCustomDisplayName
                ? CustomDisplayName
                : SelectedDisplay?.DisplayName ?? string.Empty;
            set => CustomDisplayName = value;
        }

        public SettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _settings = _settingsService.Load();
            _rawDisplays = DisplayManager.GetDisplays();

            Displays = _rawDisplays.Select((d, i) => new DisplayItemViewModel(d, i)).ToList();
            SaveCommand = new RelayCommand(Save);

            // Select default target display
            int selectedIndex = _settings.TargetMonitorIndex;
            if (selectedIndex < 0 || selectedIndex >= Displays.Count)
            {
                selectedIndex = 0;
            }
            if (Displays.Count > 0)
            {
                SelectedDisplay = Displays[selectedIndex];
            }

            UseCustomDisplayName = _settings.UseCustomDisplayName;
            CustomDisplayName = _settings.CustomDisplayName ?? string.Empty;
        }

        private void PopulateScales(DisplayInfo? display)
        {
            ScaleOptions.Clear();
            if (display == null) return;

            var sortedDpis = display.AvailableDpis.OrderBy(d => d.Percentage).ToList();
            foreach (var dpi in sortedDpis)
            {
                bool isSelected = _settings.ActiveDpiPercentages.Contains(dpi.Percentage);
                if (_settings.ActiveDpiPercentages.Count == 0 && (dpi.Percentage == 100 || dpi.Percentage == 200))
                {
                    isSelected = true;
                }

                ScaleOptions.Add(new ScaleOptionViewModel
                {
                    Percentage = dpi.Percentage,
                    IsSelected = isSelected
                });
            }
        }

        private void Save()
        {
            var activeDpiList = ScaleOptions
                .Where(o => o.IsSelected)
                .Select(o => o.Percentage)
                .ToList();

            var newSettings = new AppSettings
            {
                TargetMonitorIndex = SelectedDisplay?.Index ?? 0,
                ActiveDpiPercentages = activeDpiList,
                UseCustomDisplayName = UseCustomDisplayName,
                CustomDisplayName = (CustomDisplayName ?? string.Empty).Trim(),
                DisplayNumberSource = _settings.DisplayNumberSource,
                KeyboardSwitchMode = _settings.KeyboardSwitchMode
            };

            _settingsService.Save(newSettings);
            RequestClose?.Invoke(true);
        }
    }
}
