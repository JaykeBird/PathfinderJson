using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SolidShineUi;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for ThirdPartyCredits.xaml
    /// </summary>
    public partial class PlatformInfo : FlatWindow
    {
        public PlatformInfo()
        {
            InitializeComponent();
            ApplyColorScheme(App.ColorScheme);
            btnClose.ApplyColorScheme(App.ColorScheme);
            LoadInfo();
        }

        void LoadInfo()
        {
            txtPlatform.Text = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
            txtVersion.Text = Environment.OSVersion.VersionString;
            txtProcessor.Text = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
            txtProcessArch.Text = Environment.Is64BitProcess ? "64-bit" : "32-bit";

            if (txtProcessArch.Text != txtProcessor.Text)
            {
                txtProcessArch.Text += " (!)";
            }

            // https://weblog.west-wind.com/posts/2018/Apr/12/Getting-the-NET-Core-Runtime-Version-in-a-Running-Application
            txtTarget.Text = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName ?? "Unknown";
            txtRuntime.Text = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
