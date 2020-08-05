using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace s7dotnet {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            var currentProcess = Process.GetCurrentProcess();
            var processes = Process.GetProcesses();
            foreach (var process in processes) {
                if (process.ProcessName == currentProcess.ProcessName) {
                    if (currentProcess.Id != process.Id) {
                        //MessageBox.Show("Appication already running!");
                        SetForegroundWindow(process.MainWindowHandle);
                        Application.Current.Shutdown();
                    }
                }
            }
        }
    }
}
