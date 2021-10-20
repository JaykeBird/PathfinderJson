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

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            CounterItem ci = new CounterItem();
            ci.ColorScheme = this.ColorScheme;
            ci.Margin = new Thickness(8, 6, 8, 6);
            ci.RemoveRequested += counter_RemoveRequested;
            grdCounters.Children.Add(ci);
        }

        private void counter_RemoveRequested(object? sender, EventArgs e)
        {
            if (sender is CounterItem ci)
            {
                grdCounters.Children.Remove(ci);
            }
        }
    }
}
