using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SkypeStyleInstaller.Services;
using SkypeStyleInstaller.Models;

namespace SkypeStyleInstaller
{
    internal sealed class ExitInstallerDialog : Form
    {
        private readonly Color separatorColor;
        private readonly Color chromeColor;
        private readonly SetupUiVersion setupUiVersion;

        public ExitInstallerDialog(string productName, Icon windowIcon, SetupUiVersion setupUiVersion)
        {
            string displayName = string.IsNullOrWhiteSpace(productName) ? "Skype" : productName;
            this.setupUiVersion = setupUiVersion;
            bool skype70 = setupUiVersion == SetupUiVersion.Skype70;
            chromeColor = skype70 ? Color.FromArgb(235, 235, 235) : Color.FromArgb(249, 251, 253);
            separatorColor = skype70 ? Color.FromArgb(218, 218, 218) : Color.FromArgb(216, 229, 239);

            // Preserve the original 96-DPI geometry, but render at the monitor's
            // native DPI instead of allowing Windows to bitmap-stretch the dialog.
            AutoScaleMode = AutoScaleMode.None;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            Text = displayName + "™ - Exit installer?";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(410, 190);
            BackColor = Color.White;
            DoubleBuffered = true;

            if (windowIcon != null)
                Icon = windowIcon;

            BuildInterface(displayName);
        }

        private void BuildInterface(string productName)
        {
            bool skype70 = setupUiVersion == SetupUiVersion.Skype70;
            int headerHeight = 39;
            int footerTop = skype70 ? 151 : 147;
            int messageY = skype70 ? 59 : 59;
            int questionY = skype70 ? 108 : 108;

            var header = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(410, headerHeight),
                BackColor = chromeColor
            };
            Controls.Add(header);

            var headerLine = new Panel
            {
                Location = new Point(0, headerHeight),
                Size = new Size(410, 1),
                BackColor = separatorColor
            };
            Controls.Add(headerLine);

            var footerLine = new Panel
            {
                Location = new Point(0, footerTop - 1),
                Size = new Size(410, 1),
                BackColor = separatorColor
            };
            Controls.Add(footerLine);

            var footer = new Panel
            {
                Location = new Point(0, footerTop),
                Size = new Size(410, 190 - footerTop),
                BackColor = chromeColor
            };
            Controls.Add(footer);

            var iconBox = new PictureBox
            {
                Location = skype70 ? new Point(9, 3) : new Point(10, 3),
                Size = skype70 ? new Size(54, 75) : new Size(52, 75),
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.Normal,
                Image = LoadExitDialogMark()
            };
            Controls.Add(iconBox);
            iconBox.BringToFront();

            var heading = new Label
            {
                Text = "Exit installer?",
                Location = new Point(72, 9),
                Size = new Size(300, 22),
                AutoSize = false,
                BackColor = chromeColor,
                ForeColor = Color.FromArgb(20, 20, 20),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point),
                UseMnemonic = false,
                UseCompatibleTextRendering = false
            };
            header.Controls.Add(heading);
            heading.BringToFront();

            var message = new Label
            {
                Text = productName + " installation is not complete. Try again later by running the " +
                       productName + " installer.",
                Location = skype70 ? new Point(69, messageY) : new Point(72, messageY),
                Size = new Size(326, 52),
                AutoSize = false,
                BackColor = Color.Transparent,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                UseMnemonic = false,
                UseCompatibleTextRendering = false
            };
            Controls.Add(message);

            var question = new Label
            {
                Text = "Are you sure you want to exit?",
                Location = skype70 ? new Point(69, questionY) : new Point(72, questionY),
                Size = new Size(300, 24),
                AutoSize = false,
                BackColor = Color.Transparent,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                UseMnemonic = false,
                UseCompatibleTextRendering = false
            };
            Controls.Add(question);

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(240, 157),
                Size = new Size(73, 24),
                FlatStyle = FlatStyle.System,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                UseVisualStyleBackColor = true
            };
            footer.Controls.Add(okButton);

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(325, 157),
                Size = new Size(73, 24),
                FlatStyle = FlatStyle.System,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                UseVisualStyleBackColor = true
            };
            footer.Controls.Add(cancelButton);

            // Panel-relative coordinates.
            okButton.Location = new Point(240, 10);
            cancelButton.Location = new Point(325, 10);

            AcceptButton = okButton;
            CancelButton = cancelButton;
            ActiveControl = cancelButton;
        }

        private Image LoadExitDialogMark()
        {
            return EmbeddedResourceService.LoadBitmap(
                setupUiVersion == SetupUiVersion.Skype70
                    ? "Assets/Skype70ExitDialogMark.png"
                    : "Assets/SkypeExitDialogMark.png");
        }
    }
}
