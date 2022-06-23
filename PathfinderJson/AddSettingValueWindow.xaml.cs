using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using SolidShineUi;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for AddSettingValueWindow.xaml
    /// </summary>
    public partial class AddSettingValueWindow : FlatWindow
    {
        public AddSettingValueWindow()
        {
            InitializeComponent();
        }

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            if (CanEditName)
            {
                txtSettingName.Focus();
            }
            else
            {
                txtSettingValue.Focus();
            }
        }

        private void window_SourceInitialized(object sender, EventArgs e)
        {
            this.DisableMinimizeAndMaximizeActions();
        }

        public string Description
        {
            get => lblDescription.Text;
            set => lblDescription.Text = value;
        }

        public bool CanEditName
        {
            get => txtSettingName.IsEnabled;
            set => txtSettingName.IsEnabled = value;
        }

        public string SettingName
        {
            get => txtSettingName.Text;
            set => txtSettingName.Text = value;
        }

        public string SettingValue
        {
            get => txtSettingValue.Text;
            set => txtSettingValue.Text = value;
        }

        public string OKButtonText
        {
            get => btnInsert.Content.ToString() ?? "";
            set => btnInsert.Content = value;
        }

        public new bool DialogResult { get; set; } = false;

        private void btnInsert_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtSettingName.Text))
            {
                MessageDialog md = new MessageDialog();
                md.ShowDialog("The setting name is empty. Please enter a name for this setting before it can be added.", ColorScheme, this, "Name Empty",
                    MessageDialogButtonDisplay.Auto, MessageDialogImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void txtSettingName_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                btnInsert_Click(this, e);
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                btnClose_Click(this, e);
            }
        }

        private void txtSettingValue_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                btnInsert_Click(this, e);
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                btnClose_Click(this, e);
            }
        }
    }
}
