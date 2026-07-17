using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SkypeStyleInstaller.Services
{
    public static class ShortcutService
    {
        public static void Create(string link, string target, string workingDirectory)
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
                throw new InvalidOperationException("Windows Script Host is unavailable.");

            object shell = null;
            object shortcut = null;

            try
            {
                shell = Activator.CreateInstance(shellType);
                shortcut = shellType.InvokeMember(
                    "CreateShortcut",
                    BindingFlags.InvokeMethod,
                    null,
                    shell,
                    new object[] { link });

                Type shortcutType = shortcut.GetType();
                shortcutType.InvokeMember(
                    "TargetPath",
                    BindingFlags.SetProperty,
                    null,
                    shortcut,
                    new object[] { target });
                shortcutType.InvokeMember(
                    "WorkingDirectory",
                    BindingFlags.SetProperty,
                    null,
                    shortcut,
                    new object[] { workingDirectory });
                shortcutType.InvokeMember(
                    "IconLocation",
                    BindingFlags.SetProperty,
                    null,
                    shortcut,
                    new object[] { target + ",0" });
                shortcutType.InvokeMember(
                    "Save",
                    BindingFlags.InvokeMethod,
                    null,
                    shortcut,
                    null);
            }
            finally
            {
                if (shortcut != null && Marshal.IsComObject(shortcut))
                    Marshal.FinalReleaseComObject(shortcut);
                if (shell != null && Marshal.IsComObject(shell))
                    Marshal.FinalReleaseComObject(shell);
            }
        }
    }
}
