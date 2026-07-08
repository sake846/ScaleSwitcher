using System.Windows.Forms;

namespace ScaleSwitcher.Services
{
    public interface IExitConfirmationService
    {
        DialogResult ConfirmExit(string appName, string message);
    }
}
