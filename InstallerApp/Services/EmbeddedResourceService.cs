using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SkypeStyleInstaller.Services
{
    internal static class EmbeddedResourceService
    {
        private static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

        public static Stream Open(string logicalName)
        {
            return Assembly.GetManifestResourceStream(logicalName);
        }

        public static string ReadText(string logicalName)
        {
            using (var stream = Open(logicalName))
            {
                if (stream == null) return null;
                using (var reader = new StreamReader(stream)) return reader.ReadToEnd();
            }
        }

        public static Bitmap LoadBitmap(string logicalName)
        {
            using (var stream = Open(logicalName))
            {
                if (stream == null) return null;
                using (var temp = new Bitmap(stream)) return new Bitmap(temp);
            }
        }

        public static Icon LoadIcon(string logicalName)
        {
            using (var stream = Open(logicalName))
            {
                if (stream == null) return null;
                using (var temp = new Icon(stream)) return (Icon)temp.Clone();
            }
        }

        public static IEnumerable<string> EnumeratePayloadResources()
        {
            return Assembly.GetManifestResourceNames()
                .Where(n => n.StartsWith("Payload/", StringComparison.OrdinalIgnoreCase))
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase);
        }

        public static string GetPayloadRelativePath(string resourceName)
        {
            string relative = resourceName.Substring("Payload/".Length)
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            return relative;
        }
    }
}
