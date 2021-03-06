﻿using SolidShineUi;
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
            this.DisableMinimizeAndMaximizeActions();
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
                            fileSelect.SelectedFile = kvp.Value!;
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
                        selSheetSettings.AddItem(si);
                        break;
                }
            }
        }

        public Dictionary<string, string?> SheetSettingsList { get; set; } = new Dictionary<string, string?>();

        public new bool DialogResult { get; set; } = false;

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string, string?> newShettings = new Dictionary<string, string?>();

            if (rdoSelectFile.IsChecked.GetValueOrDefault(false) && fileSelect.SelectedFilesCount > 0)
            {
                newShettings["skillList"] = fileSelect.SelectedFile;
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
            if (fileSelect.SelectedFilesCount == 0)
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
        }

        private void btnEditSetting_Click(object sender, RoutedEventArgs e)
        {
            SelectableItem si = selSheetSettings.GetSelectedItemsOfType<SelectableItem>().First();
            if (si.Tag is KeyValuePair<string, string?> kvp)
            {
                StringInputDialog sid = new StringInputDialog(App.ColorScheme, "Set Setting Value", "Set the value for the setting \"" + kvp.Key + "\":", kvp.Value ?? "");
                sid.SelectTextOnFocus = true;
                sid.ShowDialog();

                if (sid.DialogResult)
                {
                    si.Tag = new KeyValuePair<string, string?>(kvp.Key, sid.Value);
                    si.Text = "Name: \"" + kvp.Key + "\", Value: \"" + sid.Value + "\"";
                }
            }
        }

        private void btnRemoveSetting_Click(object sender, RoutedEventArgs e)
        {
            selSheetSettings.RemoveSelectedItems();
        }

        private void selSheetSettings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnEditSetting.IsEnabled = selSheetSettings.SelectionCount != 0;
            btnRemoveSetting.IsEnabled = selSheetSettings.SelectionCount != 0;
        }
    }
}
