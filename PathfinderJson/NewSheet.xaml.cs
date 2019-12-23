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

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for NewSheet.xaml
    /// </summary>
    public partial class NewSheet : FlatWindow
    {
        public NewSheet()
        {
            InitializeComponent();
        }

        string fileLocation = "";
        UserData ud = new UserData(true);

        private void chkNoLoc_Checked(object sender, RoutedEventArgs e)
        {
            fileLocation = "";
            lblFileloc.Text = "(not set)";
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Set File Location",
                Filter = "JSON Character Sheet|*.json",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (sfd.ShowDialog().GetValueOrDefault(false))
            {
                fileLocation = sfd.FileName;
                lblFileloc.Text = System.IO.Path.GetFileName(sfd.FileName);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            PathfinderSheet ps = PathfinderSheet.CreateNewSheet(txtCharacterName.Text, txtCharacterLevel.Text, ud);
        }
    }
}
