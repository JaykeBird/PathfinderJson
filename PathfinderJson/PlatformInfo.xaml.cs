using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
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
            txtPlatform.Text = RuntimeInformation.OSDescription;
            txtVersion.Text = Environment.OSVersion.VersionString;
            txtProcessor.Text = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.Arm => "32-bit (Arm)",
                Architecture.Arm64 => "64-bit (Arm64)",
                Architecture.X64 => "64-bit (x64, Intel-based)",
                Architecture.X86 => "32-bit (x86, Intel-based)",
                _ => RuntimeInformation.ProcessArchitecture.ToString("g")
            };   
            txtProcessArch.Text = Environment.Is64BitProcess ? "64-bit" : "32-bit";

            //if (!txtProcessor.Text.Contains(txtProcessArch.Text))
            //{
            //    txtProcessArch.Text += " (!)";
            //}

            // https://weblog.west-wind.com/posts/2018/Apr/12/Getting-the-NET-Core-Runtime-Version-in-a-Running-Application
            txtTarget.Text = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName ?? "Unknown";
            txtRuntime.Text = RuntimeInformation.FrameworkDescription;
            txtRelease.Text = SettingsIo.IsPortable ? "Portable" : "Installed";
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
