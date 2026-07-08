using ScaleSwitcher.Models;

namespace ScaleSwitcher.Services
{
    public interface ISettingsService
    {
        AppSettings Load();
        void Save(AppSettings settings);
    }
}
