using System;
using System.Windows;
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
            // グローバル未処理例外ハンドラ
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

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

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            HandleUnhandledException(e.Exception);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                HandleUnhandledException(ex);
            }
        }

        private void HandleUnhandledException(Exception ex)
        {
            try
            {
                var message = $"未処理の例外が発生しました。アプリケーションを終了します。\n\n【エラー内容】\n{ex.Message}\n\n【スタックトレース】\n{ex.StackTrace}";
                MessageBox(nint.Zero, message, "ScaleSwitcher - エラー", 0x00000010 | 0x00010000 | 0x00040000); // MB_ICONERROR | MB_SETFOREGROUND | MB_TOPMOST
            }
            catch
            {
            }
            finally
            {
                Shutdown();
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "MessageBoxW", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        private static extern int MessageBox(nint hWnd, string lpText, string lpCaption, uint uType);
    }
}
