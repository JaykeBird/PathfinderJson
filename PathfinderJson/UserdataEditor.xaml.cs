using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using UiCore;
using static PathfinderJson.CoreUtils;

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

            foreach (UserData.Email item in ud.Emails)
            {
                SelectableItem si = new SelectableItem(item.Value);
                si.AllowTextEditing = true;

                if (!ColorScheme.IsHighContrast)
                {
                    var img = App.GetResourcesImage("Email");
                    si.ImageSource = img;
                    si.ShowImage = true;
                }

                selEmails.AddItem(si);
            }

            foreach (UserData.Photo item in ud.Photos)
            {
                SelectableItem si = new SelectableItem(item.Value ?? "");
                si.AllowTextEditing = true;

                if (!ColorScheme.IsHighContrast)
                {
                    si.ImageSource = App.GetResourcesImage("Link");
                    si.ShowImage = true;
                }

                selPhotos.AddItem(si);
            }
        }

        public UserData GetUserData()
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

            ud.DisplayName = GetStringOrNull(txtName.Text);
            ud.ProfileUrl = GetStringOrNull(txtProfileUrl.Text);
            ud.Id = GetStringOrNull(txtUserId.Text);

            foreach (SelectableItem item in selEmails.GetItemsAsType<SelectableItem>())
            {
                ud.Emails.Add(new UserData.Email { Value = item.Text });
            }

            foreach (SelectableItem item in selPhotos.GetItemsAsType<SelectableItem>())
            {
                ud.Photos.Add(new UserData.Photo { Value = item.Text });
            }

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

        #region Select Panel controls
        private void btnAddEmail_Click(object sender, RoutedEventArgs e)
        {
            SelectableItem si = new SelectableItem();
            si.AllowTextEditing = true;

            if (!ColorScheme.IsHighContrast)
            {
                si.ImageSource = App.GetResourcesImage("Email");
                si.ShowImage = true;
            }

            selEmails.AddItem(si);
            si.DisplayEditText();
        }

        private void btnDeleteEmail_Click(object sender, RoutedEventArgs e)
        {
            selEmails.RemoveSelectedItems();
        }

        private void btnDeselectEmail_Click(object sender, RoutedEventArgs e)
        {
            selEmails.DeselectAll();
        }

        private void btnAddPhoto_Click(object sender, RoutedEventArgs e)
        {
            SelectableItem si = new SelectableItem();
            si.AllowTextEditing = true;

            if (!ColorScheme.IsHighContrast)
            {
                si.ImageSource = App.GetResourcesImage("Link");
                si.ShowImage = true;
            }

            selPhotos.AddItem(si);
            si.DisplayEditText();
        }

        private void btnDeletePhoto_Click(object sender, RoutedEventArgs e)
        {
            selPhotos.RemoveSelectedItems();
        }

        private void btnDeselectPhoto_Click(object sender, RoutedEventArgs e)
        {
            selPhotos.DeselectAll();
        }
        #endregion
    }
}
