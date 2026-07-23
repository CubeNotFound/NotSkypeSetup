using System;
using System.IO;
using System.Xml.Linq;
using SkypeStyleInstaller.Services;

namespace SkypeStyleInstaller.Models
{
    public enum SetupUiVersion
    {
        Skype741,
        Skype70
    }

    public enum Skype70OfferPage
    {
        Plugin,
        Bing,
        Yandex,
        Wlm,
        Teams
    }

    public sealed class InstallerConfig
    {
        public string ProductName { get; set; } = "My Application";
        public string Publisher { get; set; } = "My Company";
        public string Version { get; set; } = "1.0.0";
        public string MainExecutable { get; set; } = "MyApp.exe";
        public string DefaultInstallFolder { get; set; } = @"%LocalAppData%\My Company\My Application";
        public string LicenseUrl { get; set; } = "https://example.com/license";
        public string PrivacyUrl { get; set; } = "https://example.com/privacy";
        public string TermsDisplayName { get; set; } = "Microsoft's Terms of Use";
        public string PrivacyDisplayName { get; set; } = "Privacy Statement";
        public bool ShowOfferPage { get; set; }
        public string OfferTitle { get; set; } = "One more thing";
        public string OfferText { get; set; } = "Optional bundled settings can be presented here.";
        public bool CreateDesktopShortcut { get; set; } = true;
        public bool CreateStartMenuShortcut { get; set; } = true;
        public bool LaunchAfterInstall { get; set; } = true;
        public bool StayOnInstallingForever { get; set; } = false;
        public bool DetectExistingInstallation { get; set; } = true;
        public int MarqueeAnimationSpeed { get; set; } = 30;
        public SetupUiVersion SetupUiVersion { get; set; } = SetupUiVersion.Skype741;
        public Skype70OfferPage Skype70OfferPage { get; set; } = Skype70OfferPage.Plugin;

        public string ExpandedInstallFolder => Environment.ExpandEnvironmentVariables(DefaultInstallFolder);

        public static InstallerConfig LoadEmbedded()
        {
            string xml = EmbeddedResourceService.ReadText("InstallerConfig.xml");
            if (string.IsNullOrWhiteSpace(xml)) return new InstallerConfig();
            var x = XDocument.Parse(xml).Root;
            var c = new InstallerConfig();
            string V(string n, string d) => (string)x?.Element(n) ?? d;
            bool B(string n, bool d) => bool.TryParse(V(n, d.ToString()), out var v) ? v : d;
            c.ProductName = V("ProductName", c.ProductName); c.Publisher = V("Publisher", c.Publisher);
            c.Version = V("Version", c.Version); c.MainExecutable = V("MainExecutable", c.MainExecutable);
            c.DefaultInstallFolder = V("DefaultInstallFolder", c.DefaultInstallFolder);
            c.LicenseUrl = V("LicenseUrl", c.LicenseUrl); c.PrivacyUrl = V("PrivacyUrl", c.PrivacyUrl);
            c.TermsDisplayName = V("TermsDisplayName", c.TermsDisplayName); c.PrivacyDisplayName = V("PrivacyDisplayName", c.PrivacyDisplayName);
            c.ShowOfferPage = B("ShowOfferPage", c.ShowOfferPage); c.OfferTitle = V("OfferTitle", c.OfferTitle); c.OfferText = V("OfferText", c.OfferText);
            c.CreateDesktopShortcut = B("CreateDesktopShortcut", c.CreateDesktopShortcut);
            c.CreateStartMenuShortcut = B("CreateStartMenuShortcut", c.CreateStartMenuShortcut); c.LaunchAfterInstall = B("LaunchAfterInstall", c.LaunchAfterInstall);
            c.StayOnInstallingForever = B("StayOnInstallingForever", c.StayOnInstallingForever);
            c.DetectExistingInstallation = B("DetectExistingInstallation", c.DetectExistingInstallation);
            c.SetupUiVersion = ParseSetupUiVersion(V("SetupUiVersion", "7.41"));
            c.Skype70OfferPage = ParseSkype70OfferPage(V("Skype70OfferPage", "Plugin"));
            int marqueeSpeed;
            if (int.TryParse(V("MarqueeAnimationSpeed", c.MarqueeAnimationSpeed.ToString()), out marqueeSpeed))
                c.MarqueeAnimationSpeed = Math.Max(1, marqueeSpeed);
            return c;
        }

        private static Skype70OfferPage ParseSkype70OfferPage(string value)
        {
            string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized == "bing") return Skype70OfferPage.Bing;
            if (normalized == "yandex") return Skype70OfferPage.Yandex;
            if (normalized == "wlm" || normalized == "messenger") return Skype70OfferPage.Wlm;
            if (normalized == "teams") return Skype70OfferPage.Teams;
            return Skype70OfferPage.Plugin;
        }

        private static SetupUiVersion ParseSetupUiVersion(string value)
        {
            string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized == "7.0" || normalized == "70" || normalized == "skype70" ||
                normalized == "skype-7.0" || normalized == "skype7.0")
                return SetupUiVersion.Skype70;
            return SetupUiVersion.Skype741;
        }
    }
}
