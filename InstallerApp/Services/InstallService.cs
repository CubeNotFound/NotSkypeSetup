using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using SkypeStyleInstaller.Models;

namespace SkypeStyleInstaller.Services
{
    public static class InstallService
    {
        private const string ManifestFileName = ".installer-manifest.txt";
        private const string UninstallerFileName = "Uninstall.exe";

        public static void Install(InstallerConfig c, string target, bool desktop, bool start,
            IProgress<Tuple<int, string>> progress)
        {
            Directory.CreateDirectory(target);
            string fullTarget = Path.GetFullPath(target).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var resources = new List<string>(EmbeddedResourceService.EnumeratePayloadResources());
            if (resources.Count == 0)
                throw new InvalidOperationException("No payload files were embedded in Setup.exe. Add files to the Payload folder and rebuild.");

            var installedRelativePaths = new List<string>();
            for (int i = 0; i < resources.Count; i++)
            {
                string relative = EmbeddedResourceService.GetPayloadRelativePath(resources[i]);
                if (string.IsNullOrWhiteSpace(relative)) continue;
                string destination = Path.GetFullPath(Path.Combine(target, relative));
                if (!destination.StartsWith(fullTarget, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Unsafe embedded payload path: " + relative);

                string parent = Path.GetDirectoryName(destination);
                if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
                using (var source = EmbeddedResourceService.Open(resources[i]))
                using (var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    if (source == null) throw new InvalidDataException("Embedded payload resource is missing: " + resources[i]);
                    source.CopyTo(output);
                }
                installedRelativePaths.Add(relative);
                progress?.Report(Tuple.Create((int)((i + 1) * 80.0 / resources.Count), "Copying " + relative));
            }

            string mainExe = Path.Combine(target, c.MainExecutable);
            if (!File.Exists(mainExe))
                throw new FileNotFoundException("MainExecutable does not match an embedded payload file.", mainExe);

            progress?.Report(Tuple.Create(84, "Creating uninstaller"));
            string uninstallerPath = Path.Combine(target, UninstallerFileName);
            File.Copy(Assembly.GetExecutingAssembly().Location, uninstallerPath, true);
            installedRelativePaths.Add(UninstallerFileName);

            File.WriteAllLines(Path.Combine(target, ManifestFileName), installedRelativePaths);

            progress?.Report(Tuple.Create(90, "Creating shortcuts"));
            string desktopLink = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), c.ProductName + ".lnk");
            string startDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), c.Publisher);
            string startLink = Path.Combine(startDir, c.ProductName + ".lnk");
            if (desktop) ShortcutService.Create(desktopLink, mainExe, target);
            else TryDelete(desktopLink);
            if (start)
            {
                Directory.CreateDirectory(startDir);
                ShortcutService.Create(startLink, mainExe, target);
            }
            else TryDelete(startLink);

            progress?.Report(Tuple.Create(95, "Registering application"));
            RegisterUninstaller(c, target, mainExe, uninstallerPath, installedRelativePaths);
            progress?.Report(Tuple.Create(100, "Installation complete"));
        }

        private static void RegisterUninstaller(InstallerConfig c, string target, string mainExe,
            string uninstallerPath, IList<string> files)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + Sanitize(c.ProductName)))
            {
                key.SetValue("DisplayName", c.ProductName);
                key.SetValue("DisplayVersion", c.Version);
                key.SetValue("Publisher", c.Publisher);
                key.SetValue("InstallLocation", target);
                key.SetValue("DisplayIcon", mainExe + ",0");
                key.SetValue("UninstallString", Quote(uninstallerPath) + " /uninstall");
                key.SetValue("QuietUninstallString", Quote(uninstallerPath) + " /uninstall /quiet");
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                key.SetValue("EstimatedSize", EstimateSizeKb(target, files), RegistryValueKind.DWord);
            }
        }

        private static int EstimateSizeKb(string target, IEnumerable<string> relativePaths)
        {
            long total = 0;
            foreach (string relative in relativePaths)
            {
                try { total += new FileInfo(Path.Combine(target, relative)).Length; } catch { }
            }
            return (int)Math.Min(int.MaxValue, Math.Max(1, total / 1024));
        }

        public static string UninstallKeyName(string productName)
        {
            return @"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + Sanitize(productName);
        }

        internal static string Quote(string value) { return "\"" + value + "\""; }
        internal static void TryDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }

        private static string Sanitize(string value)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_');
            return value;
        }
    }
}
