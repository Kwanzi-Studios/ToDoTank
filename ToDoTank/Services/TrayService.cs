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
            System.Drawing.Icon? appIcon = null;
            try
            {
                appIcon = System.Drawing.Icon.ExtractAssociatedIcon(
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
            menu.Items.Add("Exit", null, (_, _) => System.Windows.Application.Current.Shutdown());

            _trayIcon.ContextMenuStrip = menu;
            _trayIcon.DoubleClick += (_, _) => RestoreFromTray();
        }

        private void RestoreFromTray()
        {
            _window.Dispatcher.Invoke(() =>
            {
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                }

                _window.Show();
                _window.WindowState = WindowState.Normal;
                _window.Activate();
                _window.Topmost = true;
                _window.Topmost = false;
            });
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