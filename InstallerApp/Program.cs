using System;
using System.Linq;
using System.Windows.Forms;
using SkypeStyleInstaller.Models;
using SkypeStyleInstaller.Services;

namespace SkypeStyleInstaller
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                InstallerConfig config = InstallerConfig.LoadEmbedded();
                bool quiet = args.Any(a => string.Equals(a, "/quiet", StringComparison.OrdinalIgnoreCase));
                if (args.Any(a => string.Equals(a, "/uninstall", StringComparison.OrdinalIgnoreCase)))
                {
                    UninstallService.Start(config, quiet);
                    return;
                }
                int workerIndex = Array.FindIndex(args, a => string.Equals(a, "/uninstall-worker", StringComparison.OrdinalIgnoreCase));
                if (workerIndex >= 0 && workerIndex + 1 < args.Length)
                {
                    Environment.ExitCode = UninstallService.RunWorker(config, args[workerIndex + 1], quiet);
                    return;
                }
                Application.Run(new MainForm(config));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Setup error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
