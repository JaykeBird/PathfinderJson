using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using SolidShineUi;
using static PathfinderJson.CoreUtils;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for UpdateDisplay.xaml
    /// </summary>
    public partial class UpdateDisplay : FlatWindow
    {
        public UpdateDisplay()
        {
            InitializeComponent();
            ColorScheme = App.ColorScheme;
        }

        string url = "https://github.com/JaykeBird/PathfinderJson/releases";

        public UpdateDisplay(UpdateData ud)
        {
            InitializeComponent();
            ColorScheme = App.ColorScheme;

            if (ColorScheme.IsHighContrast)
            {
                brdrViewer.Background = ColorScheme.BackgroundColor.ToBrush();
                brdrViewer.BorderBrush = ColorScheme.BorderColor.ToBrush();
                lblMarkdown.Foreground = ColorScheme.ForegroundColor.ToBrush();
            }

            lblTitle.Text = ud.Name;
            lblTag.Text = ud.TagName + " - " + ud.PublishTime.ToString("D");
            lblMarkdown.Markdown = ud.Body;
            if (lblMarkdown.Document != null)
            {
                lblMarkdown.Document.PagePadding = new Thickness(2);

                //foreach (Block item in lblMarkdown.Document.Blocks)
                //{
                //    item.Padding = new Thickness(1);
                //}
            }

            url = ud.Url;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnViewWebsite_Click(object sender, RoutedEventArgs e)
        {
            OpenBrowser(url);
            Close();
        }
    }
}
