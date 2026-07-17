using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Win32;
using SkypeStyleInstaller.Models;

namespace SkypeStyleInstaller.Services
{
    internal static class UninstallService
    {
        private const string ManifestFileName = ".installer-manifest.txt";
        private const string InstalledUninstallerFileName = "Uninstall.exe";

        public static void Start(InstallerConfig config, bool quiet)
        {
            string installFolder = ResolveInstallFolder(config);
            if (string.IsNullOrWhiteSpace(installFolder) || !Directory.Exists(installFolder))
            {
                if (!quiet)
                {
                    System.Windows.Forms.MessageBox.Show(
                        "The " + config.ProductName + " installation could not be found.",
                        config.ProductName + " Uninstall",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Warning);
                }
                return;
            }

            string current = Assembly.GetExecutingAssembly().Location;
            string tempCopy = Path.Combine(Path.GetTempPath(), "Uninstall-" + Guid.NewGuid().ToString("N") + ".exe");
            File.Copy(current, tempCopy, true);
            var args = "/uninstall-worker " + InstallService.Quote(installFolder) + (quiet ? " /quiet" : "");
            Process.Start(new ProcessStartInfo(tempCopy, args) { UseShellExecute = false });
        }

        private static string ResolveInstallFolder(InstallerConfig config)
        {
            // An installed Uninstall.exe is authoritative when it lives beside the manifest or main file.
            try
            {
                string current = Assembly.GetExecutingAssembly().Location;
                string currentDirectory = Path.GetDirectoryName(current);
                if (LooksLikeInstallation(config, currentDirectory))
                    return Path.GetFullPath(currentDirectory);
            }
            catch { }

            // Setup.exe /uninstall should resolve the registered installation, not its own launch folder.
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(InstallService.UninstallKeyName(config.ProductName)))
                {
                    string registered = key == null ? null : key.GetValue("InstallLocation") as string;
                    if (!string.IsNullOrWhiteSpace(registered) && Directory.Exists(registered))
                        return Path.GetFullPath(registered);
                }
            }
            catch { }

            // Last fallback: the configured default installation folder.
            try
            {
                string configured = Environment.ExpandEnvironmentVariables(config.DefaultInstallFolder ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(configured) && Directory.Exists(configured))
                    return Path.GetFullPath(configured);
            }
            catch { }

            return null;
        }

        private static bool LooksLikeInstallation(InstallerConfig config, string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) return false;
            if (File.Exists(Path.Combine(folder, ManifestFileName))) return true;
            if (!string.IsNullOrWhiteSpace(config.MainExecutable) &&
                File.Exists(Path.Combine(folder, config.MainExecutable))) return true;
            return false;
        }

        public static int RunWorker(InstallerConfig config, string installFolder, bool quiet)
        {
            if (string.IsNullOrWhiteSpace(installFolder) || !Directory.Exists(installFolder)) return 0;
            installFolder = Path.GetFullPath(installFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (!quiet)
            {
                var result = System.Windows.Forms.MessageBox.Show(
                    "Are you sure you want to completely remove " + config.ProductName + " from your computer?",
                    config.ProductName + " Uninstall",
                    System.Windows.Forms.MessageBoxButtons.YesNo,
                    System.Windows.Forms.MessageBoxIcon.Question,
                    System.Windows.Forms.MessageBoxDefaultButton.Button2);
                if (result != System.Windows.Forms.DialogResult.Yes) return 1;
            }

            string desktopLink = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), config.ProductName + ".lnk");
            string startDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), config.Publisher);
            string startLink = Path.Combine(startDir, config.ProductName + ".lnk");
            DeleteFileWithRetry(desktopLink);
            DeleteFileWithRetry(startLink);
            try { if (Directory.Exists(startDir) && !Directory.EnumerateFileSystemEntries(startDir).Any()) Directory.Delete(startDir); } catch { }

            try { Registry.CurrentUser.DeleteSubKeyTree(InstallService.UninstallKeyName(config.ProductName), false); } catch { }

            string manifestPath = Path.Combine(installFolder, ManifestFileName);
            var ownedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(manifestPath))
            {
                try
                {
                    foreach (string line in File.ReadAllLines(manifestPath))
                    {
                        string relative = (line ?? string.Empty).Trim();
                        if (relative.Length > 0) ownedFiles.Add(relative);
                    }
                }
                catch { }
            }

            // Recovery entries: old/broken manifests must not leave the primary app or uninstaller behind.
            if (!string.IsNullOrWhiteSpace(config.MainExecutable)) ownedFiles.Add(config.MainExecutable.Trim());
            ownedFiles.Add(InstalledUninstallerFileName);

            foreach (string relative in ownedFiles.OrderByDescending(x => x.Length))
            {
                string full;
                if (!TryResolveOwnedPath(installFolder, relative, out full)) continue;
                DeleteFileWithRetry(full);
            }
            DeleteFileWithRetry(manifestPath);

            RemoveEmptyDirectories(installFolder);

            if (!quiet)
            {
                bool mainStillExists = !string.IsNullOrWhiteSpace(config.MainExecutable) &&
                    File.Exists(Path.Combine(installFolder, config.MainExecutable));
                if (mainStillExists)
                {
                    System.Windows.Forms.MessageBox.Show(
                        config.ProductName + " could not be completely removed because its main file is still in use. Close the application and try again.",
                        config.ProductName + " Uninstall",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Warning);
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show(config.ProductName + " was successfully removed.",
                        config.ProductName + " Uninstall", System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Information);
                }
            }

            ScheduleSelfDelete();
            return 0;
        }

        private static bool TryResolveOwnedPath(string installFolder, string relative, out string full)
        {
            full = null;
            try
            {
                string root = Path.GetFullPath(installFolder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string candidate = Path.GetFullPath(Path.Combine(installFolder, relative));
                if (!candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return false;
                full = candidate;
                return true;
            }
            catch { return false; }
        }

        private static void DeleteFileWithRetry(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            for (int attempt = 0; attempt < 6; attempt++)
            {
                try
                {
                    if (!File.Exists(path)) return;
                    File.SetAttributes(path, FileAttributes.Normal);
                    File.Delete(path);
                    if (!File.Exists(path)) return;
                }
                catch { }
                Thread.Sleep(150);
            }
        }

        private static void RemoveEmptyDirectories(string installFolder)
        {
            try
            {
                foreach (string dir in Directory.GetDirectories(installFolder, "*", SearchOption.AllDirectories)
                    .OrderByDescending(x => x.Length))
                {
                    try
                    {
                        if (!Directory.EnumerateFileSystemEntries(dir).Any()) Directory.Delete(dir);
                    }
                    catch { }
                }

                if (Directory.Exists(installFolder) && !Directory.EnumerateFileSystemEntries(installFolder).Any())
                    Directory.Delete(installFolder);
            }
            catch { }
        }

        private static void ScheduleSelfDelete()
        {
            string self = Assembly.GetExecutingAssembly().Location;
            string command = "/D /C ping 127.0.0.1 -n 2 > nul & del /F /Q " + InstallService.Quote(self);
            try { Process.Start(new ProcessStartInfo("cmd.exe", command) { CreateNoWindow = true, UseShellExecute = false }); } catch { }
        }
    }
}
