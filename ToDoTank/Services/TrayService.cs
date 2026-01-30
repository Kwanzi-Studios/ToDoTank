using System;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;

namespace ToDoTank.Services
{
    public class TrayService
    {
        private readonly Window _window;
        private NotifyIcon? _trayIcon;

        public TrayService(Window window)
        {
            _window = window;
            SetupTrayIcon();
        }

        private void SetupTrayIcon()
        {
            Icon? appIcon = null;
            try
            {
                appIcon = Icon.ExtractAssociatedIcon(
                    System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            catch
            {
                appIcon = SystemIcons.Application;
            }

            _trayIcon = new NotifyIcon
            {
                Icon = appIcon,
                Visible = false,
                Text = "ToDoTank"
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add("Restore", null, (_, _) => RestoreFromTray());
            menu.Items.Add("-");  // separator
            menu.Items.Add("Exit", null, (_, _) =>
            {
                // Call back to MainWindow to allow clean shutdown
                if (_window is MainWindow mainWindow)
                {
                    mainWindow.ConfirmAndExit();
                }
                else
                {
                    // Fallback if cast fails
                    Cleanup();
                    System.Windows.Application.Current.Shutdown();
                }
            });

            _trayIcon.ContextMenuStrip = menu;
            _trayIcon.DoubleClick += (_, _) => RestoreFromTray();
        }

        private void RestoreFromTray()
        {
            _window.Dispatcher.Invoke(() =>
            {
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;  // Hide tray icon when restored (optional — can keep visible)
                }

                _window.Show();
                _window.ShowInTaskbar = true;
                _window.WindowState = WindowState.Normal;
                _window.Activate();

                // Bring to front reliably
                _window.Topmost = true;
                _window.Topmost = false;
            });
        }

        public void ShowTrayIcon()
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = true;
            }
        }

        public void HideTrayIcon()
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
            }
        }

        // Optional: for user education on first minimize
        public void ShowBalloonTip(string title, string text, ToolTipIcon icon = ToolTipIcon.Info, int timeout = 5000)
        {
            if (_trayIcon != null && _trayIcon.Visible)
            {
                _trayIcon.ShowBalloonTip(timeout, title, text, icon);
            }
        }

        public void Cleanup()
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
        }
    }
}