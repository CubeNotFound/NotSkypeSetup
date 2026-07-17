using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using SkypeStyleInstaller.Models;
using SkypeStyleInstaller.Services;

namespace SkypeStyleInstaller
{
    public sealed class MainForm : Form
    {
        private readonly InstallerConfig config;
        private readonly Color linkColor = Color.FromArgb(0, 120, 202);
        private readonly Color dividerColor = Color.FromArgb(216, 229, 239);
        private readonly Color footerColor = Color.FromArgb(249, 251, 253);
        // Colours are fixed by the selected installer skin.
        private static readonly Color Skype70Accent = Color.FromArgb(0, 175, 240);
        private static readonly Color Skype70Footer = Color.FromArgb(235, 235, 235);
        private static readonly Color Skype70Separator = Color.FromArgb(218, 218, 218);
        private bool optionsVisible;

        private Panel root;
        private Panel rightPanel;
        private ComboBox language;
        private Label optionsLink;
        private Label installPrompt;
        private TextBox folder;
        private Button browseButton;
        private CheckBox desktopShortcut;
        private Button actionButton;
        private ProgressBar progressBar;
        private bool allowClose;
        private bool installationComplete;
        private readonly bool updateMode;
        private readonly string detectedInstallFolder;
        private bool UseSkype70Ui => config.SetupUiVersion == SetupUiVersion.Skype70;

        public MainForm(InstallerConfig config)
        {
            this.config = config;

            // Controls are authored on the original 96-DPI pixel grid. The form
            // remains DPI-aware and scales that complete grid uniformly, keeping
            // proportions exact while allowing Windows to render text sharply.
            AutoScaleMode = AutoScaleMode.None;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            detectedInstallFolder = DetectExistingInstallFolder();
            updateMode = !string.IsNullOrEmpty(detectedInstallFolder);

            Text = (updateMode ? "Updating " : "Installing ") + config.ProductName;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            ShowIcon = true;
            ClientSize = UseSkype70Ui ? new Size(720, 490) : new Size(720, 487);
            BackColor = Color.White;
            DoubleBuffered = true;
            try { Icon = EmbeddedResourceService.LoadIcon("Assets/SkypeSetup.ico"); } catch { }

            FormClosing += MainForm_FormClosing;
            if (UseSkype70Ui) BuildSkype70Main(false);
            else BuildWelcome(false);
        }

        private void BuildWelcome(bool showOptions)
        {
            Controls.Clear();
            root = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            Controls.Add(root);

            AddLogo(root);

            root.Controls.Add(CreateLabel(
                "Not far to go now...",
                new Point(24, 158), new Size(315, 28), 12F, FontStyle.Bold));

            root.Controls.Add(CreateLabel(
                "You're just a few steps away from using " + config.ProductName + ".",
                new Point(24, 199), new Size(315, 25), 9F, FontStyle.Regular));

            root.Controls.Add(CreateLabel(
                "For future updates, " + config.ProductName +
                " may automatically install the latest version to your computer without you having to do anything.",
                new Point(24, 240), new Size(322, 67), 9F, FontStyle.Regular));

            var divider = new Panel
            {
                Location = new Point(361, 158),
                Size = new Size(1, 199),
                BackColor = dividerColor
            };
            root.Controls.Add(divider);

            rightPanel = new Panel
            {
                Location = new Point(385, 158),
                Size = new Size(312, 210),
                BackColor = Color.White
            };
            root.Controls.Add(rightPanel);

            rightPanel.Controls.Add(CreateLabel(
                "Select your language:",
                new Point(0, 0), new Size(300, 20), 8.25F, FontStyle.Regular));

            language = new ComboBox
            {
                Location = new Point(0, 27),
                Size = new Size(311, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Standard,
                IntegralHeight = false,
                DropDownHeight = 225,
                Font = new Font("Segoe UI", 9F)
            };
            language.Items.AddRange(new object[] {
                "English", "العربية", "Български", "Čeština", "Dansk", "Deutsch", "Ελληνικά",
                "Español", "Eesti", "Suomi", "Français", "עברית", "हिन्दी", "Hrvatski",
                "Magyar", "Bahasa Indonesia", "Italiano", "日本語", "한국어", "Lietuvių",
                "Latviešu", "Nederlands", "Norsk", "Polski", "Português", "Română",
                "Русский", "Slovenčina", "Slovenščina", "Svenska", "ไทย", "Türkçe",
                "Українська", "Tiếng Việt", "简体中文", "繁體中文"
            });
            language.SelectedIndex = 0;
            rightPanel.Controls.Add(language);

            optionsVisible = !updateMode && showOptions;
            optionsLink = CreateToggleLink(showOptions ? "Close Options" : "More Options", new Point(0, 58));
            optionsLink.MouseUp += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                    ToggleOptions();
            };
            optionsLink.Visible = !updateMode;
            rightPanel.Controls.Add(optionsLink);

            AddInlineOptions();
            SetOptionsVisible(!updateMode && showOptions);

            AddFooter();
            AcceptButton = actionButton;
            CancelButton = null;
        }

        private void AddInlineOptions()
        {
            installPrompt = CreateLabel(
                "Choose where " + config.ProductName + " should be installed:",
                new Point(0, 89), new Size(305, 22), 9F, FontStyle.Regular);
            rightPanel.Controls.Add(installPrompt);

            folder = new TextBox
            {
                Location = new Point(0, 115),
                Size = new Size(225, 23),
                Text = updateMode ? detectedInstallFolder : config.ExpandedInstallFolder,
                Font = new Font("Segoe UI", 9F)
            };
            rightPanel.Controls.Add(folder);

            browseButton = new Button
            {
                Text = "Browse...",
                Location = new Point(234, 114),
                Size = new Size(77, 25),
                FlatStyle = FlatStyle.System,
                Font = new Font("Segoe UI", 9F)
            };
            browseButton.Click += BrowseClick;
            rightPanel.Controls.Add(browseButton);

            desktopShortcut = new CheckBox
            {
                Text = "Create desktop icon",
                Location = new Point(0, 157),
                Size = new Size(200, 23),
                Checked = config.CreateDesktopShortcut,
                Font = new Font("Segoe UI", 9F),
                UseVisualStyleBackColor = true
            };
            rightPanel.Controls.Add(desktopShortcut);
        }

        private void AddFooter()
        {
            var line = new Panel
            {
                Location = new Point(0, 427),
                Size = new Size(720, 1),
                BackColor = dividerColor
            };
            root.Controls.Add(line);

            var footer = new Panel
            {
                Location = new Point(0, 428),
                Size = new Size(720, 59),
                BackColor = footerColor
            };
            root.Controls.Add(footer);

            string legalText = "By installing this application, you agree you have read and accepted "
                + config.TermsDisplayName + " and " + config.PrivacyDisplayName + ".";
            var legal = new LinkLabel
            {
                Text = legalText,
                Location = new Point(24, 13),
                Size = new Size(535, 37),
                AutoSize = false,
                Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(32, 32, 32),
                LinkColor = linkColor,
                ActiveLinkColor = linkColor,
                VisitedLinkColor = linkColor,
                UseCompatibleTextRendering = false
            };
            int termsStart = legalText.IndexOf(config.TermsDisplayName, StringComparison.Ordinal);
            int privacyStart = legalText.IndexOf(config.PrivacyDisplayName, StringComparison.Ordinal);
            if (termsStart >= 0) legal.Links.Add(termsStart, config.TermsDisplayName.Length, config.LicenseUrl);
            if (privacyStart >= 0) legal.Links.Add(privacyStart, config.PrivacyDisplayName.Length, config.PrivacyUrl);
            legal.LinkClicked += delegate(object sender, LinkLabelLinkClickedEventArgs e)
            {
                string url = e.Link.LinkData as string;
                if (!string.IsNullOrEmpty(url)) OpenUrl(url);
            };
            footer.Controls.Add(legal);

            actionButton = new Button
            {
                Text = "I agree - next",
                Location = new Point(598, 18),
                Size = new Size(98, 25),
                FlatStyle = FlatStyle.System,
                Font = new Font("Segoe UI", 9F),
                UseVisualStyleBackColor = true
            };
            actionButton.Click += BeginInstall;
            footer.Controls.Add(actionButton);
        }

        private void ToggleOptions()
        {
            rightPanel.SuspendLayout();
            try
            {
                optionsVisible = !optionsVisible;
                optionsLink.Text = optionsVisible ? "Close Options" : "More Options";
                if (UseSkype70Ui) SetSkype70OptionsVisible(optionsVisible);
                else SetOptionsVisible(optionsVisible);
            }
            finally
            {
                rightPanel.ResumeLayout(false);
            }
        }

        private void SetOptionsVisible(bool visible)
        {
            if (installPrompt != null) installPrompt.Visible = visible;
            if (folder != null) folder.Visible = visible;
            if (browseButton != null) browseButton.Visible = visible;
            if (desktopShortcut != null) desktopShortcut.Visible = visible;
        }

        private void AddLogo(Control parent)
        {
            var logo = new PictureBox
            {
                Location = new Point(18, 18),
                Size = new Size(122, 60),
                SizeMode = PictureBoxSizeMode.Normal,
                BackColor = Color.White,
                Image = EmbeddedResourceService.LoadBitmap("Assets/SkypeBrandOfficial.png")
            };
            parent.Controls.Add(logo);
        }

        private void BeginInstall(object sender, EventArgs e)
        {
            string target = updateMode
                ? detectedInstallFolder
                : (folder != null ? folder.Text.Trim() : config.ExpandedInstallFolder);
            bool desktop = desktopShortcut != null ? desktopShortcut.Checked : config.CreateDesktopShortcut;

            if (string.IsNullOrWhiteSpace(target))
            {
                MessageBox.Show(this, "Please choose an installation folder.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (UseSkype70Ui && config.ShowOfferPage)
            {
                switch (config.Skype70OfferPage)
                {
                    case Skype70OfferPage.Bing: BuildSkype70BingOffer(target, desktop); break;
                    case Skype70OfferPage.Yandex: BuildSkype70YandexOffer(target, desktop); break;
                    case Skype70OfferPage.Wlm: BuildSkype70WlmPage(target, desktop); break;
                    default: BuildSkype70PluginOffer(target, desktop); break;
                }
                return;
            }

            StartInstall(target, desktop);
        }

        private async void StartInstall(string target, bool desktop)
        {
            BuildInstalling();

            // Demo/test mode: display the authentic indefinite installing page
            // forever without reading, copying, launching, or changing any files.
            if (config.StayOnInstallingForever)
                return;

            try
            {
                var progress = new Progress<Tuple<int, string>>(value =>
                {
                    if (progressBar.Style != ProgressBarStyle.Marquee)
                    {
                        progressBar.Value = Math.Max(0, Math.Min(100, value.Item1));
                    }
                });

                await Task.Run(() => InstallService.Install(config, target, desktop,
                    config.CreateStartMenuShortcut, progress));
                BuildFinished(target);
            }
            catch (Exception ex)
            {
                BuildError(ex);
            }
        }

        private void BuildInstalling()
        {
            if (UseSkype70Ui)
            {
                BuildSkype70Installing();
                return;
            }

            Controls.Clear();
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            Controls.Add(panel);
            AddLogo(panel);

            panel.Controls.Add(CreateLabel(
                updateMode ? "We're updating your " + config.ProductName : "Installing " + config.ProductName,
                new Point(24, 158), new Size(620, 28), 12F, FontStyle.Bold));

            panel.Controls.Add(CreateLabel(
                updateMode
                    ? "This won't take long..."
                    : "Please wait while " + config.ProductName + " is installed. This may take a few minutes.",
                new Point(24, 229), new Size(650, 25), 9F, FontStyle.Regular));

            progressBar = new ProgressBar
            {
                Location = new Point(24, 257),
                Size = new Size(672, 32),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = config.MarqueeAnimationSpeed
            };
            panel.Controls.Add(progressBar);
            AcceptButton = null;
            CancelButton = null;
        }

        private void BuildFinished(string target)
        {
            if (UseSkype70Ui)
            {
                BuildSkype70Finished(target);
                return;
            }

            installationComplete = true;
            Controls.Clear();
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            Controls.Add(panel);
            AddLogo(panel);

            panel.Controls.Add(CreateLabel("Installation complete",
                new Point(24, 158), new Size(620, 30), 12F, FontStyle.Bold));
            panel.Controls.Add(CreateLabel(config.ProductName + " has been installed successfully.",
                new Point(24, 229), new Size(650, 25), 9F, FontStyle.Regular));

            var finish = new Button
            {
                Text = "Finish",
                Location = new Point(598, 446),
                Size = new Size(98, 25),
                FlatStyle = FlatStyle.System
            };
            finish.Click += delegate
            {
                if (config.LaunchAfterInstall)
                {
                    string exe = Path.Combine(target, config.MainExecutable);
                    try { Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = target }); }
                    catch { }
                }
                allowClose = true;
                Close();
            };
            panel.Controls.Add(finish);
            AcceptButton = finish;
        }

        private void BuildError(Exception ex)
        {
            if (UseSkype70Ui)
            {
                BuildSkype70Problems(ex);
                return;
            }

            Controls.Clear();
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            Controls.Add(panel);
            AddLogo(panel);

            panel.Controls.Add(CreateLabel("Installation failed",
                new Point(24, 158), new Size(620, 30), 12F, FontStyle.Bold));
            panel.Controls.Add(CreateLabel("An error occurred while installing " + config.ProductName + ".",
                new Point(24, 205), new Size(650, 25), 9F, FontStyle.Regular));
            panel.Controls.Add(new TextBox
            {
                Location = new Point(24, 240),
                Size = new Size(672, 145),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Text = ex.Message,
                Font = new Font("Segoe UI", 9F)
            });

            var close = new Button
            {
                Text = "Close",
                Location = new Point(598, 446),
                Size = new Size(98, 25),
                FlatStyle = FlatStyle.System
            };
            close.Click += delegate { allowClose = true; Close(); };
            panel.Controls.Add(close);
            AcceptButton = close;
        }

        private void BuildSkype70Main(bool showOptions)
        {
            Controls.Clear();
            root = CreateSkype70Root();
            var page = AddSkype70PageHost();

            var leftPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(322, 199),
                BackColor = Color.White
            };
            page.Controls.Add(leftPanel);

            leftPanel.Controls.Add(CreateSkype70Label(
                "Not far to go now...",
                new Point(0, 0), new Size(315, 24), 11F, FontStyle.Bold, Skype70Accent));
            leftPanel.Controls.Add(CreateSkype70Label(
                "You're just a few steps away from using " + config.ProductName + ".",
                new Point(0, 40), new Size(315, 24), 9F, FontStyle.Regular));
            leftPanel.Controls.Add(CreateSkype70Label(
                "For future updates, " + config.ProductName +
                " may automatically install the latest version to your computer without you having to do anything.",
                new Point(0, 80), new Size(322, 68), 9F, FontStyle.Regular));

            page.Controls.Add(new Panel
            {
                Location = new Point(337, 0),
                Size = new Size(1, 199),
                BackColor = Color.FromArgb(216, 229, 239)
            });

            rightPanel = new Panel
            {
                Location = new Point(361, 0),
                Size = new Size(312, 210),
                BackColor = Color.White
            };
            page.Controls.Add(rightPanel);

            rightPanel.Controls.Add(CreateSkype70Label(
                "Select your language:",
                new Point(0, 0), new Size(300, 20), 9F, FontStyle.Regular));

            language = new ComboBox
            {
                Location = new Point(0, 27),
                Size = new Size(311, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Standard,
                IntegralHeight = false,
                DropDownHeight = 225,
                Font = new Font("Tahoma", 9F)
            };
            language.Items.AddRange(new object[] {
                "English", "العربية", "Български", "Čeština", "Dansk", "Deutsch",
                "Ελληνικά", "Español", "Eesti", "Suomi", "Français", "עברית",
                "हिन्दी", "Hrvatski", "Magyar", "Bahasa Indonesia", "Italiano",
                "日本語", "한국어", "Lietuvių", "Latviešu", "Nederlands", "Norsk",
                "Polski", "Português", "Română", "Русский", "Slovenčina",
                "Slovenščina", "Svenska", "ไทย", "Türkçe", "Українська",
                "Tiếng Việt", "简体中文", "繁體中文"
            });
            language.SelectedIndex = 0;
            rightPanel.Controls.Add(language);

            optionsVisible = !updateMode && showOptions;
            optionsLink = CreateSkype70ToggleLink(showOptions ? "Close Options" : "More Options", new Point(0, 84));
            optionsLink.MouseUp += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                    ToggleOptions();
            };
            optionsLink.Visible = !updateMode;
            rightPanel.Controls.Add(optionsLink);

            var startWithComputer = new CheckBox
            {
                Name = "Skype70StartWithComputer",
                Text = "Run " + config.ProductName + " when the computer starts",
                Location = new Point(0, 57),
                Size = new Size(300, 22),
                Checked = true,
                Font = new Font("Tahoma", 9F),
                UseVisualStyleBackColor = true
            };
            rightPanel.Controls.Add(startWithComputer);

            installPrompt = CreateSkype70Label(
                "Choose where " + config.ProductName + " should be installed:",
                new Point(0, 113), new Size(305, 20), 9F, FontStyle.Regular);
            rightPanel.Controls.Add(installPrompt);

            folder = new TextBox
            {
                Location = new Point(0, 140),
                Size = new Size(225, 22),
                Text = updateMode ? detectedInstallFolder : config.ExpandedInstallFolder,
                Font = new Font("Tahoma", 9F)
            };
            rightPanel.Controls.Add(folder);

            browseButton = new Button
            {
                Text = "Browse...",
                Location = new Point(234, 139),
                Size = new Size(77, 24),
                FlatStyle = FlatStyle.System,
                Font = new Font("Tahoma", 9F)
            };
            browseButton.Click += BrowseClick;
            rightPanel.Controls.Add(browseButton);

            desktopShortcut = new CheckBox
            {
                Text = "Create desktop icon",
                Location = new Point(0, 178),
                Size = new Size(250, 21),
                Checked = config.CreateDesktopShortcut,
                Font = new Font("Tahoma", 9F),
                UseVisualStyleBackColor = true
            };
            rightPanel.Controls.Add(desktopShortcut);

            SetSkype70OptionsVisible(!updateMode && showOptions);
            AddSkype70Footer(
                "By installing this application, you agree you have read and accepted " +
                config.TermsDisplayName + " and " + config.PrivacyDisplayName + ".",
                "I agree - next", BeginInstall, 98);
        }

        private void SetSkype70OptionsVisible(bool visible)
        {
            if (rightPanel == null) return;
            // In the original 7.0 installer this option is always visible.
            // More/Close Options only expands the destination and desktop-icon controls.
            foreach (Control c in rightPanel.Controls)
            {
                if (c.Name == "Skype70StartWithComputer") c.Visible = true;
            }
            if (installPrompt != null) installPrompt.Visible = visible;
            if (folder != null) folder.Visible = visible;
            if (browseButton != null) browseButton.Visible = visible;
            if (desktopShortcut != null) desktopShortcut.Visible = visible;
        }

        private void BuildSkype70Installing()
        {
            Controls.Clear();
            root = CreateSkype70Root();

            root.Controls.Add(CreateSkype70Label(
                updateMode ? "Updating " + config.ProductName : "Installing " + config.ProductName,
                new Point(24, 163), new Size(650, 24), 11F, FontStyle.Bold, Skype70Accent));
            root.Controls.Add(CreateSkype70Label(
                updateMode
                    ? "This won't take long..."
                    : "Please wait while " + config.ProductName + " is installed. This may take a few minutes.",
                new Point(24, 234), new Size(650, 22), 9F, FontStyle.Regular));

            progressBar = new ProgressBar
            {
                Location = new Point(24, 260),
                Size = new Size(672, 32),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = config.MarqueeAnimationSpeed
            };
            root.Controls.Add(progressBar);
            AcceptButton = null;
            CancelButton = null;
        }

        private void BuildSkype70Finished(string target)
        {
            installationComplete = true;
            Controls.Clear();
            root = CreateSkype70Root();
            var page = AddSkype70PageHost();

            page.Controls.Add(CreateSkype70Label("Installation complete",
                new Point(0, 0), new Size(650, 20), 11F, FontStyle.Bold));
            page.Controls.Add(CreateSkype70Label(config.ProductName + " has been successfully installed on your computer.",
                new Point(0, 20), new Size(650, 35), 9F, FontStyle.Regular));
            page.Controls.Add(CreateSkype70Label("Click Finish to exit Skype Setup.",
                new Point(0, 56), new Size(650, 20), 9F, FontStyle.Regular));

            AddSkype70Footer(string.Empty, "Finish", delegate
            {
                if (config.LaunchAfterInstall)
                {
                    string exe = Path.Combine(target, config.MainExecutable);
                    try { Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = target }); } catch { }
                }
                allowClose = true;
                Close();
            }, 75);
        }

        private void BuildSkype70Problems(Exception ex)
        {
            Controls.Clear();
            root = CreateSkype70Root();
            var page = AddSkype70PageHost();

            page.Controls.Add(CreateSkype70Label("We're having some problems",
                new Point(0, 0), new Size(650, 20), 11F, FontStyle.Bold));
            page.Controls.Add(CreateSkype70Label("An error occurred while installing " + config.ProductName + ".",
                new Point(0, 30), new Size(650, 25), 9F, FontStyle.Regular));
            page.Controls.Add(CreateSkype70Label(ex.Message,
                new Point(0, 60), new Size(650, 130), 9F, FontStyle.Regular));

            AddSkype70Footer(string.Empty, "Close", delegate { allowClose = true; Close(); }, 75);
        }

        private void BuildSkype70PluginOffer(string target, bool desktop)
        {
            Controls.Clear();
            root = CreateSkype70Root();
            var page = AddSkype70PageHost();

            var left = new Panel { Location = new Point(0, 0), Size = new Size(320, 245), BackColor = Color.White };
            page.Controls.Add(left);
            left.Controls.Add(CreateSkype70Label("Skype Click to Call", new Point(0, 0), new Size(310, 24), 11F, FontStyle.Bold, Skype70Accent));
            left.Controls.Add(CreateSkype70Label("Make the most of " + config.ProductName + " when browsing the web.", new Point(0, 38), new Size(310, 24), 9F, FontStyle.Regular));
            left.Controls.Add(CreateSkype70FormattedParagraph(
                "Save time. ",
                "Instantly make calls by clicking numbers with a Skype icon – you'll spot them on most websites.",
                new Point(0, 70),
                new Size(310, 58)));
            left.Controls.Add(CreateSkype70FormattedParagraph(
                "Make free calls. ",
                "Next time you are searching for a restaurant, a hotel, anything – just call. Numbers labeled 'free' are no charge.",
                new Point(0, 132),
                new Size(310, 74)));
            left.Controls.Add(new CheckBox
            {
                Text = "Install Skype Click to Call",
                Location = new Point(0, 220),
                Size = new Size(300, 25),
                Checked = true,
                Font = new Font("Tahoma", 9F),
                UseVisualStyleBackColor = true
            });
            AddSkype70Art(page, "Assets/Skype70ClickToCall.png", new Point(360, 6), new Size(312, 272));
            AddSkype70Footer(string.Empty, "Continue", delegate { StartInstall(target, desktop); }, 73);
        }

        private Control CreateSkype70FormattedParagraph(
            string boldLead,
            string body,
            Point location,
            Size size)
        {
            var box = new RichTextBox
            {
                Location = location,
                Size = size,
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = Color.Black,
                ReadOnly = true,
                TabStop = false,
                DetectUrls = false,
                ScrollBars = RichTextBoxScrollBars.None,
                WordWrap = true,
                ShortcutsEnabled = false,
                Font = new Font("Tahoma", 9F, FontStyle.Regular, GraphicsUnit.Point)
            };

            box.Text = boldLead + body;
            box.Select(0, boldLead.Length);
            box.SelectionFont = new Font("Tahoma", 9F, FontStyle.Bold, GraphicsUnit.Point);
            box.Select(boldLead.Length, body.Length);
            box.SelectionFont = new Font("Tahoma", 9F, FontStyle.Regular, GraphicsUnit.Point);
            box.Select(0, 0);
            return box;
        }

        private void BuildSkype70BingOffer(string target, bool desktop)
        {
            Controls.Clear();
            root = CreateSkype70Root();
            var page = AddSkype70PageHost();
            var left = new Panel { Location = new Point(0, 0), Size = new Size(312, 235), BackColor = Color.White };
            page.Controls.Add(left);
            left.Controls.Add(CreateSkype70Label("Make Bing your search engine and MSN your homepage.", new Point(0, 0), new Size(310, 42), 11F, FontStyle.Bold));
            left.Controls.Add(CreateSkype70Label("Get great search results from Bing and stay in the know with MSN about the things that matter most to you.", new Point(0, 44), new Size(310, 58), 9F, FontStyle.Regular));
            left.Controls.Add(new CheckBox { Text = "Make Bing my search engine", Location = new Point(0, 118), Size = new Size(280, 20), Checked = true, Font = new Font("Tahoma", 9F), UseVisualStyleBackColor = true });
            left.Controls.Add(new CheckBox { Text = "Make MSN my homepage", Location = new Point(0, 150), Size = new Size(280, 20), Checked = true, Font = new Font("Tahoma", 9F), UseVisualStyleBackColor = true });
            AddSkype70Art(page, "Assets/Skype70Bing.png", new Point(346, 0), new Size(326, 235));
            AddSkype70Footer("By clicking 'Continue' you agree to Microsoft Service Agreement and Privacy Policy.", "Continue", delegate { StartInstall(target, desktop); }, 75);
        }

        private void BuildSkype70YandexOffer(string target, bool desktop)
        {
            Controls.Clear();
            root = CreateSkype70Root();
            var page = AddSkype70PageHost();
            var left = new Panel { Location = new Point(0, 0), Size = new Size(312, 235), BackColor = Color.White };
            page.Controls.Add(left);
            left.Controls.Add(CreateSkype70Label("Yandex Toolbar", new Point(0, 0), new Size(310, 20), 11F, FontStyle.Bold));
            left.Controls.Add(CreateSkype70Label("Do you want to install Yandex in your browser?", new Point(0, 24), new Size(310, 38), 9F, FontStyle.Regular));
            left.Controls.Add(new CheckBox { Text = "Make Yandex your homepage", Location = new Point(0, 150), Size = new Size(300, 20), Checked = false, Font = new Font("Tahoma", 9F), UseVisualStyleBackColor = true });
            left.Controls.Add(new CheckBox { Text = "Install Yandex search", Location = new Point(0, 185), Size = new Size(300, 20), Checked = false, Font = new Font("Tahoma", 9F), UseVisualStyleBackColor = true });
            AddSkype70Footer("By installing and using this software, you agree to the conditions of the License Agreement.", "Continue", delegate { StartInstall(target, desktop); }, 75);
        }

        private void BuildSkype70WlmPage(string target, bool desktop)
        {
            Controls.Clear();
            root = CreateSkype70Root();
            var page = AddSkype70PageHost();
            var left = new Panel { Location = new Point(0, 0), Size = new Size(312, 235), BackColor = Color.White };
            page.Controls.Add(left);
            left.Controls.Add(CreateSkype70Label("Your Messenger Buddies and IM are now on Skype", new Point(0, 0), new Size(310, 40), 11F, FontStyle.Bold));
            left.Controls.Add(CreateSkype70Label("Microsoft is upgrading your experience to Skype.", new Point(0, 48), new Size(310, 25), 9F, FontStyle.Regular));
            left.Controls.Add(CreateSkype70Label("Continue to IM and video call with your buddies, plus with Skype you get group video calling and more.", new Point(0, 80), new Size(310, 58), 9F, FontStyle.Regular));
            left.Controls.Add(CreateSkype70Label("Please note, as Skype includes IM and your buddies, Messenger will be uninstalled.", new Point(0, 148), new Size(310, 48), 9F, FontStyle.Regular));
            AddSkype70Art(page, "Assets/Skype70Wlm.png", new Point(346, 0), new Size(326, 235));
            AddSkype70Footer(string.Empty, "Continue", delegate { StartInstall(target, desktop); }, 75);
        }

        private Panel CreateSkype70Root()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            Controls.Add(panel);

            var header = new PictureBox
            {
                Location = new Point(0, 0),
                Size = new Size(720, 112),
                BackColor = Color.FromArgb(0, 175, 240),
                SizeMode = PictureBoxSizeMode.Normal,
                Image = EmbeddedResourceService.LoadBitmap("Assets/Skype70Header.png")
            };
            panel.Controls.Add(header);
            header.Controls.Add(new PictureBox
            {
                Location = new Point(24, 31),
                Size = new Size(102, 46),
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.Normal,
                Image = EmbeddedResourceService.LoadBitmap("Assets/Skype70Logo.png")
            });
            return panel;
        }

        private Panel AddSkype70PageHost()
        {
            var page = new Panel
            {
                Location = new Point(24, 155),
                Size = new Size(672, 272),
                BackColor = Color.White
            };
            root.Controls.Add(page);
            return page;
        }

        private void AddSkype70Footer(string legalText, string buttonText, EventHandler click, int buttonWidth)
        {
            var separator = new Panel
            {
                Location = new Point(0, 427),
                Size = new Size(720, 1),
                BackColor = Skype70Separator
            };
            root.Controls.Add(separator);

            var footer = new Panel
            {
                Location = new Point(0, 428),
                Size = new Size(720, 62),
                BackColor = Skype70Footer
            };
            root.Controls.Add(footer);

            if (!string.IsNullOrEmpty(legalText))
            {
                var legal = new LinkLabel
                {
                    Text = legalText,
                    Location = new Point(24, 13),
                    Size = new Size(535, 40),
                    AutoSize = false,
                    Font = new Font("Tahoma", 8.25F, FontStyle.Regular, GraphicsUnit.Point),
                    BackColor = Color.Transparent,
                    ForeColor = Color.FromArgb(32, 32, 32),
                    LinkColor = Color.FromArgb(0, 102, 204),
                    ActiveLinkColor = Color.FromArgb(0, 102, 204),
                    VisitedLinkColor = Color.FromArgb(0, 102, 204),
                    UseCompatibleTextRendering = false
                };
                int termsStart = legalText.IndexOf(config.TermsDisplayName, StringComparison.Ordinal);
                int privacyStart = legalText.IndexOf(config.PrivacyDisplayName, StringComparison.Ordinal);
                if (termsStart >= 0) legal.Links.Add(termsStart, config.TermsDisplayName.Length, config.LicenseUrl);
                if (privacyStart >= 0) legal.Links.Add(privacyStart, config.PrivacyDisplayName.Length, config.PrivacyUrl);
                legal.LinkClicked += delegate(object sender, LinkLabelLinkClickedEventArgs e)
                {
                    string url = e.Link.LinkData as string;
                    if (!string.IsNullOrEmpty(url)) OpenUrl(url);
                };
                footer.Controls.Add(legal);
            }

            actionButton = new Button
            {
                Text = buttonText,
                Location = new Point(696 - buttonWidth, 18),
                Size = new Size(buttonWidth, 25),
                FlatStyle = FlatStyle.System,
                Font = new Font("Tahoma", 9F),
                UseVisualStyleBackColor = true
            };
            actionButton.Click += click;
            footer.Controls.Add(actionButton);
            AcceptButton = actionButton;
        }

        private void AddSkype70Art(Control page, string resourceName, Point location, Size viewport)
        {
            var art = new PictureBox
            {
                Location = location,
                Size = viewport,
                BackColor = Color.White,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = EmbeddedResourceService.LoadBitmap(resourceName)
            };
            page.Controls.Add(art);
        }

        private Label CreateSkype70ToggleLink(string text, Point location)
        {
            var link = new Label
            {
                Text = text,
                Location = location,
                AutoSize = true,
                Cursor = Cursors.Hand,
                Font = new Font("Tahoma", 9F, FontStyle.Underline, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(0, 102, 204),
                BackColor = Color.Transparent
            };
            return link;
        }

        private Label CreateSkype70Label(string text, Point location, Size size, float fontSize, FontStyle style)
        {
            return CreateSkype70Label(text, location, size, fontSize, style, Color.Black);
        }

        private Label CreateSkype70Label(string text, Point location, Size size, float fontSize, FontStyle style, Color color)
        {
            return new Label
            {
                Text = text,
                Location = location,
                Size = size,
                AutoSize = false,
                BackColor = Color.Transparent,
                ForeColor = color,
                Font = new Font("Tahoma", fontSize, style, GraphicsUnit.Point),
                UseCompatibleTextRendering = false
            };
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (allowClose || installationComplete || e.CloseReason == CloseReason.WindowsShutDown)
                return;

            e.Cancel = true;
            using (var dialog = new ExitInstallerDialog(config.ProductName, Icon, config.SetupUiVersion))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    allowClose = true;
                    BeginInvoke(new Action(Close));
                }
            }
        }

        private string DetectExistingInstallFolder()
        {
            if (!config.DetectExistingInstallation)
                return null;

            string registryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + SanitizeRegistryKey(config.ProductName);
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registryPath))
                {
                    string location = key == null ? null : key.GetValue("InstallLocation") as string;
                    if (IsExistingInstallation(location))
                        return Path.GetFullPath(location);
                }
            }
            catch { }

            string fallback = config.ExpandedInstallFolder;
            return IsExistingInstallation(fallback) ? Path.GetFullPath(fallback) : null;
        }

        private bool IsExistingInstallation(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
                return false;

            try
            {
                return File.Exists(Path.Combine(location, config.MainExecutable));
            }
            catch
            {
                return false;
            }
        }

        private static string SanitizeRegistryKey(string value)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return value;
        }

        private void BrowseClick(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Choose where to install " + config.ProductName;
                dialog.SelectedPath = folder.Text;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    folder.Text = dialog.SelectedPath;
            }
        }

        private Label CreateLabel(string text, Point location, Size size, float fontSize, FontStyle style)
        {
            return new Label
            {
                Text = text,
                Location = location,
                Size = size,
                AutoSize = false,
                BackColor = Color.Transparent,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", fontSize, style, GraphicsUnit.Point),
                UseCompatibleTextRendering = false
            };
        }


        private Label CreateToggleLink(string text, Point location)
        {
            return new Label
            {
                Text = text,
                Location = location,
                AutoSize = true,
                ForeColor = linkColor,
                Font = new Font("Segoe UI", 9F, FontStyle.Underline, GraphicsUnit.Point),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                TabStop = false,
                AccessibleRole = AccessibleRole.Link,
                UseCompatibleTextRendering = false
            };
        }

        private LinkLabel CreateLink(string text, Point location)
        {
            return new LinkLabel
            {
                Text = text,
                Location = location,
                AutoSize = true,
                LinkColor = linkColor,
                ActiveLinkColor = linkColor,
                VisitedLinkColor = linkColor,
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.Transparent,
                UseCompatibleTextRendering = false
            };
        }

        private static void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { }
        }
    }
}
