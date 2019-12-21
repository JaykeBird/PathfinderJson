using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using UiCore;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for UserdataEditor.xaml
    /// </summary>
    public partial class UserdataEditor : FlatWindow
    {
        public UserdataEditor()
        {
            InitializeComponent();
            ApplyColorScheme(App.ColorScheme);
        }

        public new bool DialogResult { get; set; } = false;

        public void LoadUserData(UserData ud)
        {
            switch (ud.Provider.ToLowerInvariant())
            {
                case "google":
                    cbbProvider.SelectedIndex = 0;
                    break;
                case "github":
                    cbbProvider.SelectedIndex = 1;
                    break;
                case "local":
                    cbbProvider.SelectedIndex = 2;
                    break;
                default:
                    cbbProvider.SelectedIndex = 2;
                    break;
            }

            txtName.Text = ud.DisplayName;
            txtProfileUrl.Text = ud.ProfileUrl;
            txtUserId.Text = ud.Id;
        }

        private UserData GetUserData()
        {
            UserData ud = new UserData();

            switch (cbbProvider.SelectedIndex)
            {
                case 0:
                    ud.Provider = "google";
                    break;
                case 1:
                    ud.Provider = "github";
                    break;
                case 2:
                    ud.Provider = "local";
                    break;
                default:
                    ud.Provider = "local";
                    break;
            }

            ud.DisplayName = txtName.Text;
            ud.ProfileUrl = txtProfileUrl.Text;
            ud.Id = txtUserId.Text;

            return ud;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.Title = "Import Data from File";
            ofd.Filter = "Pathfinder Character Sheet|*.json|All Files|*.*";
            
            if (ofd.ShowDialog() ?? false == true)
            {
                string filename = ofd.FileName;
                PathfinderSheet ps = PathfinderSheet.LoadJsonFile(filename);
                LoadUserData(ps.Player);
            }

        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
