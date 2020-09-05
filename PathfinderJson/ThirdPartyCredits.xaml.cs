using System;
using System.Collections.Generic;
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
    public partial class ThirdPartyCredits : FlatWindow
    {
        public ThirdPartyCredits()
        {
            InitializeComponent();
            ApplyColorScheme(App.ColorScheme);
            btnClose.ApplyColorScheme(App.ColorScheme);

            expSolidShineUi.Background = App.ColorScheme.LightBackgroundColor.ToBrush();
            expSolidShineUi.BorderBrush = App.ColorScheme.ThirdHighlightColor.ToBrush();
            expSolidShineUi.BorderThickness = new Thickness(1);

            expJson.Background = App.ColorScheme.LightBackgroundColor.ToBrush();
            expJson.BorderBrush = App.ColorScheme.ThirdHighlightColor.ToBrush();
            expJson.BorderThickness = new Thickness(1);

            expAvalon.Background = App.ColorScheme.LightBackgroundColor.ToBrush();
            expAvalon.BorderBrush = App.ColorScheme.ThirdHighlightColor.ToBrush();
            expAvalon.BorderThickness = new Thickness(1);

            expMark1.Background = App.ColorScheme.LightBackgroundColor.ToBrush();
            expMark1.BorderBrush = App.ColorScheme.ThirdHighlightColor.ToBrush();
            expMark1.BorderThickness = new Thickness(1);

            expMark2.Background = App.ColorScheme.LightBackgroundColor.ToBrush();
            expMark2.BorderBrush = App.ColorScheme.ThirdHighlightColor.ToBrush();
            expMark2.BorderThickness = new Thickness(1);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
