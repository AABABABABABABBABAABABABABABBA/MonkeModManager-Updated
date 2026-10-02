using System;
using System.Windows.Forms;

namespace MonkeModManager
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => ShowCrash(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => ShowCrash(e.ExceptionObject as Exception);

            Application.Run(new FormMain());
        }

        private static void ShowCrash(Exception ex)
        {
            MessageBox.Show("Something went wrong:\n\n" + ex, "Monke Mod Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
