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
using UiCore;

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

            lblTitle.Text = ud.Name;
            lblTag.Text = ud.TagName + " - " + ud.PublishTime.ToString("D");
            lblMarkdown.Markdown = ud.Body;
            lblMarkdown.Document.PagePadding = new Thickness(2);

            url = ud.Url;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnViewWebsite_Click(object sender, EventArgs e)
        {
            About.OpenBrowser(url);
            Close();
        }
    }
}
