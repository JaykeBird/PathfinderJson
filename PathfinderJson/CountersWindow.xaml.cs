using SolidShineUi;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for CountersWindow.xaml
    /// </summary>
    public partial class CountersWindow : FlatWindow
    {
        public CountersWindow()
        {
            InitializeComponent();
            ColorScheme = App.ColorScheme;
        }
    }
}
