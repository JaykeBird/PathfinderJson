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

        public new bool DialogResult { get; set; } = false;

        private void btnInsert_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

    }
}
