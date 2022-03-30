using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using SolidShineUi;
using static PathfinderJson.CoreUtils;

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

            txtStrm.Text = CalculateModifier(txtStr.Value);
            txtDexm.Text = CalculateModifier(txtDex.Value);
            txtCham.Text = CalculateModifier(txtCha.Value);
            txtConm.Text = CalculateModifier(txtCon.Value);
            txtIntm.Text = CalculateModifier(txtInt.Value);
            txtWism.Text = CalculateModifier(txtWis.Value);

            ColorScheme = App.ColorScheme;

            if (ColorScheme.IsHighContrast)
            {
                txtStrm.BorderBrush = new SolidColorBrush(App.ColorScheme.LightDisabledColor);
                txtDexm.BorderBrush = new SolidColorBrush(App.ColorScheme.LightDisabledColor);
                txtCham.BorderBrush = new SolidColorBrush(App.ColorScheme.LightDisabledColor);
                txtConm.BorderBrush = new SolidColorBrush(App.ColorScheme.LightDisabledColor);
                txtIntm.BorderBrush = new SolidColorBrush(App.ColorScheme.LightDisabledColor);
                txtWism.BorderBrush = new SolidColorBrush(App.ColorScheme.LightDisabledColor);

                txtStrm.Background = new SolidColorBrush(SystemColors.ControlColor);
                txtDexm.Background = new SolidColorBrush(SystemColors.ControlColor);
                txtCham.Background = new SolidColorBrush(SystemColors.ControlColor);
                txtConm.Background = new SolidColorBrush(SystemColors.ControlColor);
                txtIntm.Background = new SolidColorBrush(SystemColors.ControlColor);
                txtWism.Background = new SolidColorBrush(SystemColors.ControlColor);
            }
            else
            {
                txtStrm.BorderBrush = new SolidColorBrush(SystemColors.ControlDarkColor);
                txtDexm.BorderBrush = new SolidColorBrush(SystemColors.ControlDarkColor);
                txtCham.BorderBrush = new SolidColorBrush(SystemColors.ControlDarkColor);
                txtConm.BorderBrush = new SolidColorBrush(SystemColors.ControlDarkColor);
                txtIntm.BorderBrush = new SolidColorBrush(SystemColors.ControlDarkColor);
                txtWism.BorderBrush = new SolidColorBrush(SystemColors.ControlDarkColor);

                txtStrm.Background = App.ColorScheme.SecondHighlightColor.ToBrush();
                txtDexm.Background = App.ColorScheme.SecondHighlightColor.ToBrush();
                txtCham.Background = App.ColorScheme.SecondHighlightColor.ToBrush();
                txtConm.Background = App.ColorScheme.SecondHighlightColor.ToBrush();
                txtIntm.Background = App.ColorScheme.SecondHighlightColor.ToBrush();
                txtWism.Background = App.ColorScheme.SecondHighlightColor.ToBrush();
            }
        }

        private void window_SourceInitialized(object sender, EventArgs e)
        {
            this.DisableMinimizeAndMaximizeActions();
        }

        public string FileLocation { get; private set; } = "";
        UserData ud = new UserData(true);

        public new bool DialogResult { get; set; } = false;
        public PathfinderSheet Sheet { get; private set; } = new PathfinderSheet();

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCharacterName.Text) || string.IsNullOrWhiteSpace(txtCharacterLevel.Text))
            {
                MessageDialog md = new MessageDialog(ColorScheme);
                md.Image = MessageDialogImage.Error;
                md.Message = "Either the character's name or class/level is missing. Please enter those before continuing.";
                md.Title = "Missing Data";
                md.Owner = this;
                md.ShowDialog();
                return;
            }

            // Create Pathfinder sheet
            // Including ability scores
            // (I also set up the RawAbilities property despite it not really being used, in case it may become an issue later on)
            Sheet = PathfinderSheet.CreateNewSheet(txtCharacterName.Text, txtCharacterLevel.Text, ud);
            Sheet.Charisma = txtCha.Value;
            Sheet.Constitution = txtCon.Value;
            Sheet.Dexterity = txtDex.Value;
            Sheet.Intelligence = txtInt.Value;
            Sheet.Strength = txtStr.Value;
            Sheet.Wisdom = txtWis.Value;

            Dictionary<string, string> abilities = new Dictionary<string, string>
            {
                { "str", txtStr.Value.ToString() },
                { "dex", txtDex.Value.ToString() },
                { "cha", txtCha.Value.ToString() },
                { "con", txtCon.Value.ToString() },
                { "int", txtInt.Value.ToString() },
                { "wis", txtWis.Value.ToString() }
            };
            Sheet.RawAbilities = abilities;

            if (!string.IsNullOrEmpty(FileLocation))
            {
                Sheet.SaveJsonFile(FileLocation, App.Settings.IndentJsonData);
            }

            DialogResult = true;
            Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #region UserData / File Location
        private void chkNoLoc_Checked(object sender, RoutedEventArgs e)
        {
            FileLocation = "";
            txtFilename.Text = "(location not set)";
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Set File Location",
                Filter = "JSON Character Sheet|*.json",

            };

            if (string.IsNullOrEmpty(FileLocation))
            {
                sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            else
            {
                sfd.InitialDirectory = System.IO.Directory.GetParent(FileLocation)?.FullName ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            if (sfd.ShowDialog().GetValueOrDefault(false))
            {
                FileLocation = sfd.FileName;
                txtFilename.Text = System.IO.Path.GetFileName(sfd.FileName);
                chkNoLoc.IsChecked = false;
            }
        }

        private void btnImportData_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.Title = "Import Data from File";
            ofd.Filter = "Pathfinder Character Sheet|*.json|All Files|*.*";

            if (ofd.ShowDialog() ?? false == true)
            {
                string filename = ofd.FileName;
                PathfinderSheet ps = PathfinderSheet.LoadJsonFile(filename);
                ud = ps.Player ?? new UserData(true);
                if (!string.IsNullOrEmpty(ud.DisplayName))
                {
                    txtPlayerName.Text = ud.DisplayName;
                }
                else
                {
                    txtPlayerName.Text = "(not set)";
                }
            }
        }

        private void btnEditData_Click(object sender, RoutedEventArgs e)
        {
            UserdataEditor ude = new UserdataEditor();
            ude.Owner = this;
            ude.LoadUserData(ud);

            ude.ShowDialog();
            if (ude.DialogResult)
            {
                ud = ude.GetUserData();
                if (!string.IsNullOrEmpty(ud.DisplayName))
                {
                    txtPlayerName.Text = ud.DisplayName;
                }
                else
                {
                    txtPlayerName.Text = "(not set)";
                }
            }
        }
        #endregion

        #region Ability Scores
        bool _isUpdating = false;

        private void txtStr_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!_isUpdating && (txtStrm != null))
            {
                txtStrm.Text = CalculateModifier(txtStr.Value);
                txtDexm.Text = CalculateModifier(txtDex.Value);
                txtCham.Text = CalculateModifier(txtCha.Value);
                txtConm.Text = CalculateModifier(txtCon.Value);
                txtIntm.Text = CalculateModifier(txtInt.Value);
                txtWism.Text = CalculateModifier(txtWis.Value);
            }
        }
        #endregion
    }
}
