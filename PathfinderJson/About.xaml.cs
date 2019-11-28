using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using UiCore;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : FlatWindow
    {
        public About()
        {
            InitializeComponent();

            ApplyColorScheme(App.ColorScheme);
            brdrDivider.BorderBrush = new SolidColorBrush(App.ColorScheme.BorderColor);

            lblVersion.Text = "Version " + App.AppVersion.ToString();
            //btnClose.ApplyColorScheme(App.ColorScheme);
            //btnThirdParty.ApplyColorScheme(App.ColorScheme);
        }

        private void LinkTextBlock_Click(object sender, EventArgs e)
        {
            OpenBrowser("http://charactersheet.co.uk/pathfinder/");
        }

        private void LinkTextBlock2_Click(object sender, EventArgs e)
        {
            OpenBrowser("https://twitter.com/JaykeBird/");
        }

        private void LinkTextBlock3_Click(object sender, EventArgs e)
        {
            OpenBrowser("https://paizo.com/communityuse");
        }

        private void LinkTextBlock4_Click(object sender, EventArgs e)
        {
            OpenBrowser("https://paizo.com/");
        }

        private void LinkTextBlock5_Click(object sender, EventArgs e)
        {
            OpenBrowser("https://github.com/JaykeBird/PathfinderJson/");
        }

        /// <summary>
        /// Opens the user's default browser to a certain URL. Works on Windows, Linux, and OS X.
        /// </summary>
        /// <param name="url">The URL to open.</param>
        /// <returns></returns>
        public static bool OpenBrowser(string url)
        {
            // See https://github.com/dotnet/corefx/issues/10361
            // This is best-effort only, but should work most of the time.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // See https://stackoverflow.com/a/6040946/44360 for why this is required
                url = System.Text.RegularExpressions.Regex.Replace(url, @"(\\*)" + "\"", @"$1$1\" + "\"");
                url = System.Text.RegularExpressions.Regex.Replace(url, @"(\\+)$", @"$1$1");
                Process.Start(new ProcessStartInfo("cmd", $"/c start \"\" \"{url}\"") { CreateNoWindow = true });
                return true;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
                return true;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
                return true;
            }
            return false;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void BtnThirdParty_Click(object sender, EventArgs e)
        {
            ThirdPartyCredits tpc = new ThirdPartyCredits();
            tpc.Owner = this;
            tpc.ShowDialog();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            OpenBrowser(e.Uri.AbsoluteUri);
        }

        private void BtnPlatformInfo_Click(object sender, EventArgs e)
        {
            PlatformInfo pi = new PlatformInfo();
            pi.Owner = this;
            pi.ShowDialog();
        }
    }
}
