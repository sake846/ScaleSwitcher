using System;
using System.Windows;
using System.Threading;
using ScaleSwitcher.Services;
using WpfApplication = System.Windows.Application;

namespace ScaleSwitcher
{
    public partial class App : WpfApplication
    {
        private const string SingleInstanceMutexName = @"Local\ScaleSwitcher.SingleInstance";
        private System.Threading.Mutex? _singleInstanceMutex;
        private AppController? _controller;

        protected override void OnStartup(StartupEventArgs e)
        {
            if (!TryAcquireSingleInstanceMutex())
            {
                Shutdown();
                return;
            }

            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _controller = new AppController();
            _controller.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _controller?.Dispose();
            ReleaseSingleInstanceMutex();
            base.OnExit(e);
        }

        private bool TryAcquireSingleInstanceMutex()
        {
            _singleInstanceMutex = new System.Threading.Mutex(true, SingleInstanceMutexName, out var createdNew);
            if (createdNew)
            {
                return true;
            }

            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
            return false;
        }

        private void ReleaseSingleInstanceMutex()
        {
            if (_singleInstanceMutex != null)
            {
                try
                {
                    _singleInstanceMutex.ReleaseMutex();
                }
                catch
                {
                    // Ignore
                }
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
            }
        }
    }
}
