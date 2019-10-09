using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UiCore;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for ThirdPartyCredits.xaml
    /// </summary>
    public partial class ThirdPartyCredits : FlatWindow
    {
        public ThirdPartyCredits()
        {
            InitializeComponent();
            ApplyColorScheme(App.ColorScheme);
            btnClose.ApplyColorScheme(App.ColorScheme);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
