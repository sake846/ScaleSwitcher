using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace ScaleSwitcher.Views
{
    public partial class OsdWindow : Window
    {
        public OsdWindow(string message, double fontSize = 36, bool hideCursor = true)
        {
            InitializeComponent();
            MessageText.Text = message;
            MessageText.FontSize = fontSize;
            Cursor = hideCursor ? System.Windows.Input.Cursors.None : System.Windows.Input.Cursors.Arrow;
        }

        public void CloseWithFade()
        {
            try
            {
                ReleaseMouseCapture();
            }
            catch
            {
                // Ignore
            }

            // Respect OS animation settings (SPI_GETCLIENTAREAANIMATION)
            if (!SystemParameters.ClientAreaAnimation)
            {
                Close();
                return;
            }

            // motion-normal token: 200ms
            var anim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(200));
            anim.Completed += (s, e) => Close();
            BeginAnimation(OpacityProperty, anim);
        }
    }
}
