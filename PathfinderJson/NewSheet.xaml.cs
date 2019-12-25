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

            ColorScheme = App.ColorScheme;
        }

        private void window_SourceInitialized(object sender, EventArgs e)
        {
            DisableMinimizeAndMaximizeActions();
        }

        public string FileLocation { get; private set; } = "";
        UserData ud = new UserData(true);

        public new bool DialogResult { get; set; } = false;
        public PathfinderSheet Sheet { get; private set; } = null;

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
                sfd.InitialDirectory = System.IO.Directory.GetParent(FileLocation).FullName;
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
                ud = ps.Player;
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
            if (!_isUpdating)
            {
                txtStrm.Text = CalculateModifier(txtStr.Value);
                txtDexm.Text = CalculateModifier(txtDex.Value);
                txtCham.Text = CalculateModifier(txtCha.Value);
                txtConm.Text = CalculateModifier(txtCon.Value);
                txtIntm.Text = CalculateModifier(txtInt.Value);
                txtWism.Text = CalculateModifier(txtWis.Value);
            }
        }

        string CalculateModifier(int score)
        {
            int r = (int)Math.Floor((score - 10) / 2d);
            if (r >= 0) return "+" + r.ToString(); else return r.ToString();
        }
        #endregion
    }
}
