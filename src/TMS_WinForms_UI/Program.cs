using System;
using WinFormsApplication = System.Windows.Forms.Application;

namespace TMS_WinForms_UI
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // WinForms owns the application lifetime in the hybrid shell.
            // WPF is loaded later only as hosted UserControls inside ElementHost.
            WinFormsApplication.SetHighDpiMode(System.Windows.Forms.HighDpiMode.SystemAware);
            WinFormsApplication.EnableVisualStyles();
            WinFormsApplication.SetCompatibleTextRenderingDefault(false);
            WinFormsApplication.Run(new LoginForm());
        }
    }
}
