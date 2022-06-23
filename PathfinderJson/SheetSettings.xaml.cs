using SolidShineUi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for SheetSettings.xaml
    /// </summary>
    public partial class SheetSettings : FlatWindow
    {
        public SheetSettings()
        {
            InitializeComponent();
            ColorScheme = App.ColorScheme;
        }

        private void window_SourceInitialized(object sender, EventArgs e)
        {
            this.DisableMinimizeAction();
        }

        public void UpdateUi()
        {
            foreach (KeyValuePair<string, string?> kvp in SheetSettingsList)
            {
                switch (kvp.Key)
                {
                    case "notesMarkdown":
                        chkMarkdown.IsChecked = (kvp.Value ?? "") == "enabled";
                        break;
                    case "notesNoSpellCheck":
                        chkSpellcheckAll.IsChecked = (kvp.Value ?? "") != "enabled";
                        break;
                    case "skillList":
                        if (System.IO.File.Exists(kvp.Value ?? ""))
                        {
                            fileSelect.SelectedFiles[0] = kvp.Value!;
                            rdoSelectFile.IsChecked = true;
                        }
                        else
                        {
                            MessageDialog md = new MessageDialog();
                            md.ShowDialog("The file \"" + kvp.Value + "\" could not be found to use as a skill list. Defaulting to standard skill list.", App.ColorScheme, this,
                                "Skill List File Not Found", buttonDisplay: MessageDialogButtonDisplay.Auto, image: MessageDialogImage.Error);
                            rdoSkillList.IsChecked = true;
                        }
                        break;
                    default:
                        SelectableItem si = new SelectableItem("Name: \"" + kvp.Key + "\", Value: \"" + (kvp.Value ?? "(empty)") + "\"", null, 5);
                        si.Tag = kvp;
                        selSheetSettings.Items.Add(si);
                        break;
                }
            }
        }

        public Dictionary<string, string?> SheetSettingsList { get; set; } = new Dictionary<string, string?>();

        public new bool DialogResult { get; set; } = false;

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string, string?> newShettings = new Dictionary<string, string?>();

            if (rdoSelectFile.IsChecked.GetValueOrDefault(false) && fileSelect.SelectedFiles.Count > 0)
            {
                newShettings["skillList"] = fileSelect.SelectedFiles[0];
            }

            if (chkMarkdown.IsChecked)
            {
                newShettings["notesMarkdown"] = "enabled";
            }

            if (!chkSpellcheckAll.IsChecked)
            {
                newShettings["notesNoSpellCheck"] = "enabled";
            }

            foreach (SelectableUserControl item in selSheetSettings.Items)
            {
                if (item.Tag is KeyValuePair<string, string?> kvp)
                {
                    newShettings[kvp.Key] = kvp.Value;
                }
            }

            SheetSettingsList = newShettings;

            DialogResult = true;
            Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void fileSelect_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (fileSelect.SelectedFiles.Count == 0)
            {
                rdoSkillList.IsChecked = true;
            }
            else
            {
                rdoSelectFile.IsChecked = true;
            }
        }

        private void btnAddSetting_Click(object sender, RoutedEventArgs e)
        {
            // TO BE ADDED
            AddSettingValueWindow asv = new AddSettingValueWindow();
            asv.ColorScheme = ColorScheme;
            asv.ShowDialog();

            if (asv.DialogResult)
            {
                SelectableItem si = new SelectableItem();
                si.Tag = new KeyValuePair<string, string>(asv.SettingName, asv.SettingValue);
                si.Text = $"Name: \"{asv.SettingName}\", Value: \"{asv.SettingValue}\"";

                selSheetSettings.Items.Add(si);
            }
        }

        private void btnEditSetting_Click(object sender, RoutedEventArgs e)
        {
            if (selSheetSettings.Items.SelectedItems.Count == 0) return;

            SelectableItem si = selSheetSettings.Items.SelectedItems.OfType<SelectableItem>().First();
            if (si.Tag is KeyValuePair<string, string?> kvp)
            {
                StringInputDialog sid = new StringInputDialog(App.ColorScheme, "Set Setting Value", "Set the value for the setting \"" + kvp.Key + "\":", kvp.Value ?? "");
                sid.SelectTextOnFocus = true;
                sid.ShowDialog();

                if (sid.DialogResult)
                {
                    si.Tag = new KeyValuePair<string, string?>(kvp.Key, sid.Value);
                    si.Text = $"Name: \"{kvp.Key}\", Value: \"{sid.Value}\"";
                }
            }
        }

        private void btnRemoveSetting_Click(object sender, RoutedEventArgs e)
        {
            MessageDialog md = new MessageDialog(ColorScheme);
            md.ShowDialog("Are you sure you want to remove the selected setting?", null, this, "Confirm Remove", MessageDialogButtonDisplay.Two, MessageDialogImage.Question);
            if (md.DialogResult == MessageDialogResult.OK)
            {
                selSheetSettings.RemoveSelectedItems();
            }
        }

        private void selSheetSettings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnEditSetting.IsEnabled = selSheetSettings.Items.SelectedItems.Count != 0;
            btnRemoveSetting.IsEnabled = selSheetSettings.Items.SelectedItems.Count != 0;
        }
    }
}
