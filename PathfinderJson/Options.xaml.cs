using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SolidShineUi;
using System.Diagnostics;
using static PathfinderJson.CoreUtils;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : FlatWindow
    {
        /// <summary>Path to the AppData folder where settings are stored</summary>
        string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PathfinderJson");

        // temp settings
        bool clearRecentList = false;
        Color uiColor = App.ColorScheme.MainColor;

        public Options()
        {
            InitializeComponent();

            SetupTabs();
            LoadTab("General");
            selTabs[0].IsSelected = true;

            LoadSettings();
        }

        //public Options(string tab)
        //{
        //    InitializeComponent();

        //    SetupTabs();
        //    LoadTab(tab);
        //}

        #region Tab bar
        void SetupTabs()
        {
            selTabs.AddItem(CreateTab("General"));
            //selTabs.AddItem(CreateTab("Saving"));
            selTabs.AddItem(CreateTab("Interface"));
            selTabs.AddItem(CreateTab("JSON Editor"));
            selTabs.AddItem(CreateTab("Feedback"));
            selTabs.AddItem(CreateTab("Advanced"));

            SelectableItem CreateTab(string name, ImageSource? image = null)
            {
                SelectableItem si = new SelectableItem
                {
                    Height = 36,
                    Text = name,
                    Indent = 6
                };

                if (image == null)
                {
                    si.ShowImage = false;
                }
                else
                {
                    si.ImageSource = image;
                    si.ShowImage = true;
                }

                si.Click += tabItem_Click;
                return si;
            }
        }

        private void tabItem_Click(object? sender, EventArgs e)
        {
            if (sender == null) return;
            SelectableItem si = (SelectableItem)sender;

            if (si.CanSelect)
            {
                LoadTab(si.Text);
            }
        }

        void LoadTab(string name)
        {
            foreach (UIElement? item in grdHolder.Children)
            {
                if (item != null)
                {
                    item.Visibility = Visibility.Collapsed;
                }
            }

            switch (name)
            {
                case "General":
                    tabGeneral.Visibility = Visibility.Visible;
                    break;
                case "Saving":
                    tabSaving.Visibility = Visibility.Visible;
                    break;
                case "Interface":
                    tabInterface.Visibility = Visibility.Visible;
                    break;
                case "JSON Editor":
                    tabJsonEditor.Visibility = Visibility.Visible;
                    break;
                case "Feedback":
                    tabFeedback.Visibility = Visibility.Visible;
                    break;
                case "Advanced":
                    tabAdvanced.Visibility = Visibility.Visible;
                    break;
            }
        }
        #endregion

        #region Settings IO

        void LoadSettings()
        {
            Settings s = App.Settings;

            // General options
            chkRecentDisplayMenus.IsChecked = s.DisplayRecentActionsAsSubmenu;

            chkAutoUpdate.IsChecked = s.UpdateAutoCheck;

            if (s.AutoSave <= 0)
            {
                chkAutoSave.IsChecked = false;
                nudAutoSave.Value = 5;
                nudAutoSave.IsEnabled = false;
            }
            else if (s.AutoSave > 30)
            {
                chkAutoSave.IsChecked = true;
                nudAutoSave.Value = 30;
            }
            else
            {
                chkAutoSave.IsChecked = true;
                nudAutoSave.Value = s.AutoSave;
            }

            // Interface options
            uiColor = ColorsHelper.CreateFromHex(App.Settings.ThemeColor);
            chkHighContrast.IsChecked = !(s.HighContrastTheme == App.NO_HIGH_CONTRAST);
            cbbHighContrast.IsEnabled = chkHighContrast.IsChecked;
            switch (s.HighContrastTheme)
            {
                case "1": // white on black
                    cbbHighContrast.SelectedIndex = 0;
                    break;
                case "2": // green on black
                    cbbHighContrast.SelectedIndex = 1;
                    break;
                case "3": // black on white
                    cbbHighContrast.SelectedIndex = 2;
                    break;
                default:
                    cbbHighContrast.SelectedIndex = 0;
                    break;
            }

            chkToolbar.IsChecked = s.ShowToolbar;
            chkFilename.IsChecked = s.PathInTitleBar;

            // Text editor options
            LoadEditorFontSettings(s);
            chkSyntaxHighlight.IsChecked = s.EditorSyntaxHighlighting;
            chkIndentEditor.IsChecked = s.IndentJsonData;
            chkLineNumbers.IsChecked = s.EditorLineNumbers;
            chkWordWrap.IsChecked = s.EditorWordWrap;

            // Advanced options
            chkUseOptimization.IsChecked = s.UseStartupOptimization;
        }

        void SaveSettings()
        {
            // General options
            App.Settings.UpdateAutoCheck = chkAutoUpdate.IsChecked;
            if (clearRecentList) App.Settings.RecentFiles.Clear();

            App.Settings.DisplayRecentActionsAsSubmenu = chkRecentDisplayMenus.IsChecked;

            App.Settings.AutoSave = chkAutoSave.IsChecked ? nudAutoSave.Value : 0;

            // Interface options
            App.ColorScheme = new ColorScheme(uiColor);
            App.Settings.ThemeColor = uiColor.GetHexString();
            App.Settings.ShowToolbar = chkToolbar.IsChecked;
            App.Settings.PathInTitleBar = chkFilename.IsChecked;

            if (chkHighContrast.IsChecked)
            {
                switch (cbbHighContrast.SelectedIndex)
                {
                    case 0:
                        App.Settings.HighContrastTheme = "1";
                        break;
                    case 1:
                        App.Settings.HighContrastTheme = "2";
                        break;
                    case 2:
                        App.Settings.HighContrastTheme = "3";
                        break;
                    default:
                        App.Settings.HighContrastTheme = "1";
                        break;
                }
            }
            else
            {
                App.Settings.HighContrastTheme = App.NO_HIGH_CONTRAST;
            }

            // Text editor options
            string ff = (txtFont1.FontFamily.Source).Replace(", Consolas", "");

            App.Settings.EditorFontFamily = ff;
            App.Settings.EditorFontSize = txtFont1.FontSize.ToString();

            // because the ToString() method for FontStyle uses CurrentCulture rather than InvariantCulture, I need to convert it to string myself.
            if (txtFont1.FontStyle == FontStyles.Italic)
            {
                App.Settings.EditorFontStyle = "Italic";
            }
            else if (txtFont1.FontStyle == FontStyles.Oblique)
            {
                App.Settings.EditorFontStyle = "Oblique";
            }
            else
            {
                App.Settings.EditorFontStyle = "Normal";
            }

            App.Settings.EditorSyntaxHighlighting = chkSyntaxHighlight.IsChecked;
            App.Settings.EditorWordWrap = chkWordWrap.IsChecked;
            App.Settings.IndentJsonData = chkIndentEditor.IsChecked;
            App.Settings.EditorLineNumbers = chkLineNumbers.IsChecked;

            // Advanced options
            App.Settings.UseStartupOptimization = chkUseOptimization.IsChecked;

            // ----------------------------------------
            // finally, save the settings to a file
            try
            {
                App.Settings.Save(Path.Combine(appDataPath, "settings.json"));
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("The settings file for PathfinderJson could not be saved. Please check the permissions for your AppData folder.",
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (System.Security.SecurityException)
            {
                MessageBox.Show("The settings file for PathfinderJson could not be saved. Please check the permissions for your AppData folder.",
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (IOException)
            {
                MessageBox.Show("The settings file for PathfinderJson could not be saved. Please check the permissions for your AppData folder.",
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void LoadEditorFontSettings(Settings s)
        {
            string family = s.EditorFontFamily;
            string size = s.EditorFontSize;
            string style = s.EditorFontStyle;
            string weight = s.EditorFontWeight.Replace("w", "").Replace(".", "");

            // sanitizing user input
            if (string.IsNullOrEmpty(family))
            {
                family = "Consolas";
            }
            if (string.IsNullOrEmpty(size))
            {
                size = "12";
            }
            if (string.IsNullOrEmpty(style))
            {
                style = "Normal";
            }
            if (string.IsNullOrEmpty(weight))
            {
                weight = "400";
            }

            if (style == "None")
            {
                style = "Normal";
            }

            // check if weight is an integer value or not; if not, try to convert it
            if (!int.TryParse(weight, out _))
            {
                // converter of common fontweight values
                // taken from https://docs.microsoft.com/en-us/dotnet/api/system.windows.fontweights
                if (weight.ToLowerInvariant() == "thin")
                {
                    weight = "100";
                }
                else if (weight.ToLowerInvariant() == "extralight" || weight.ToLowerInvariant() == "ultralight")
                {
                    weight = "200";
                }
                else if (weight.ToLowerInvariant() == "light")
                {
                    weight = "300";
                }
                else if (weight.ToLowerInvariant() == "normal" || weight.ToLowerInvariant() == "regular")
                {
                    weight = "400";
                }
                else if (weight.ToLowerInvariant() == "medium")
                {
                    weight = "500";
                }
                else if (weight.ToLowerInvariant() == "demibold" || weight.ToLowerInvariant() == "semibold")
                {
                    weight = "600";
                }
                else if (weight.ToLowerInvariant() == "bold")
                {
                    weight = "700";
                }
                else if (weight.ToLowerInvariant() == "extrabold" || weight.ToLowerInvariant() == "ultrabold")
                {
                    weight = "800";
                }
                else if (weight.ToLowerInvariant() == "black" || weight.ToLowerInvariant() == "heavy")
                {
                    weight = "900";
                }
                else if (weight.ToLowerInvariant() == "extrablack" || weight.ToLowerInvariant() == "ultrablack")
                {
                    weight = "950";
                }
                else
                {
                    // don't know what the heck they put in there, but it's not a font weight; set it to normal
                    weight = "400";
                }
            }

            FontFamily ff = new FontFamily(family + ", Consolas"); // use Consolas as fallback in case that font doesn't exist or the font doesn't contain proper glyphs

            double dsz = 12;

            try
            {
                dsz = double.Parse(size.Replace("p", "").Replace("d", "").Replace("x", "").Replace("t", ""));
            }
            catch (FormatException) { } // if "size" is a string that isn't actually a double, just keep it as 12

            FontStyle fs = FontStyles.Normal;
            try
            {
                fs = (FontStyle)new FontStyleConverter().ConvertFromInvariantString(style);
            }
            catch (NotSupportedException) { } // if "style" is a string that isn't actually a FontStyle, just keep it as normal
            catch (FormatException) { }

            int w = int.Parse(weight);
            if (w > 999)
            {
                w = 999;
            }
            else if (w < 1)
            {
                w = 1;
            }
            FontWeight fw = FontWeight.FromOpenTypeWeight(w);

            UpdateFontForTextBlock(txtFont1);
            UpdateFontForTextBlock(txtFont2);
            UpdateFontForTextBlock(txtFont3);
            UpdateFontForTextBlock(txtNum1);
            UpdateFontForTextBlock(txtNum2);
            UpdateFontForTextBlock(txtNum3);

            void UpdateFontForTextBlock(TextBlock tb)
            {
                tb.FontFamily = ff;
                tb.FontSize = dsz;
                tb.FontStyle = fs;
                tb.FontWeight = fw;
            }
        }

        #endregion

        #region Button actions

        private async void btnUpdateNow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateData ud = await UpdateChecker.CheckForUpdatesAsync();
                if (ud.HasUpdate)
                {
                    UpdateDisplay uw = new UpdateDisplay(ud);
                    uw.Owner = this;
                    uw.ShowDialog();
                }
                else
                {
                    MessageDialog md = new MessageDialog(App.ColorScheme);
                    md.ShowDialog("There are no updates available. You're on the latest release!", App.ColorScheme, this, "Check for Updates", MessageDialogButtonDisplay.Auto, image: MessageDialogImage.Hand);
                }
            }
            catch (System.Net.WebException)
            {
                MessageDialog md = new MessageDialog(App.ColorScheme);
                md.ShowDialog("Could not check for updates. Make sure you're connected to the Internet.", App.ColorScheme, this, "Check for Updates", MessageDialogButtonDisplay.Auto, image: MessageDialogImage.Error);
            }
        }

        private void btnClearList_Click(object sender, RoutedEventArgs e)
        {
            if (AskClearRecentList())
            {
                clearRecentList = true;
                lblRecentFiles.Visibility = Visibility.Visible;
            }

            bool AskClearRecentList()
            {
                MessageDialog md = new MessageDialog(App.ColorScheme);
                md.ShowDialog("Are you sure you want to remove all files from the Recent Files list?", App.ColorScheme, this, "Clear Recent Files List", MessageDialogButtonDisplay.Two, MessageDialogImage.Question, MessageDialogResult.Cancel,
                    "Yes", "Cancel");

                if (md.DialogResult == MessageDialogResult.OK)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private void chkAutoSave_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (nudAutoSave == null) return;
            nudAutoSave.IsEnabled = chkAutoSave.IsChecked;
        }

        private void btnFontOptions_Click(object sender, RoutedEventArgs e)
        {
            FontSelectDialog fds = new FontSelectDialog
            {
                ShowDecorations = false,
                ColorScheme = App.ColorScheme,

                SelectedFontFamily = txtFont1.FontFamily,
                SelectedFontSize = txtFont1.FontSize,
                SelectedFontStyle = txtFont1.FontStyle,
                SelectedFontWeight = txtFont1.FontWeight
            };

            fds.ShowDialog();

            if (fds.DialogResult)
            {
                UpdateFontForTextBlock(txtFont1);
                UpdateFontForTextBlock(txtFont2);
                UpdateFontForTextBlock(txtFont3);
                UpdateFontForTextBlock(txtNum1);
                UpdateFontForTextBlock(txtNum2);
                UpdateFontForTextBlock(txtNum3);

                void UpdateFontForTextBlock(TextBlock tb)
                {
                    tb.FontFamily = fds.SelectedFontFamily;
                    tb.FontSize = fds.SelectedFontSize;
                    tb.FontStyle = fds.SelectedFontStyle;
                    tb.FontWeight = fds.SelectedFontWeight;
                }
            }
        }

        private void chkWordWrap_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (txtFont2 != null)
            {
                txtFont1.TextWrapping = chkWordWrap.IsChecked ? TextWrapping.Wrap : TextWrapping.NoWrap;
                txtFont2.TextWrapping = chkWordWrap.IsChecked ? TextWrapping.Wrap : TextWrapping.NoWrap;
                txtFont3.TextWrapping = chkWordWrap.IsChecked ? TextWrapping.Wrap : TextWrapping.NoWrap;
            }
        }

        private void chkLineNumbers_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (chkLineNumbers.IsChecked)
            {
                // <ColumnDefinition x:Name="colNum" Width="Auto" MinWidth="10" />
                // <ColumnDefinition x: Name = "colBuff" Width = "2" />

                colNum.Width = new GridLength(0, GridUnitType.Auto);
                colNum.MinWidth = 10;
                colBuff.Width = new GridLength(2);
            }
            else
            {
                colNum.Width = new GridLength(0, GridUnitType.Pixel);
                colNum.MinWidth = 0;
                colBuff.Width = new GridLength(0);
            }
        }

        private void btnCrashLogs_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(Path.Combine(appDataPath, "ErrorLogs")))
            {
                Directory.CreateDirectory(Path.Combine(appDataPath, "ErrorLogs"));
            }

            Process.Start("explorer.exe", Path.Combine(appDataPath, "ErrorLogs"));
        }

        private void btnFeedback_Click(object sender, RoutedEventArgs e)
        {
            OpenBrowser("https://github.com/JaykeBird/PathfinderJson/issues/new/choose");
        }


        #endregion

        private void btnPlatformInfo_Click(object sender, RoutedEventArgs e)
        {
            PlatformInfo pi = new PlatformInfo();
            pi.Owner = this;
            pi.ShowDialog();
        }

        public new bool DialogResult { get; set; } = false;

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            SaveSettings();
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void chkHighContrast_CheckChanged(object sender, RoutedEventArgs e)
        {
            cbbHighContrast.IsEnabled = chkHighContrast.IsChecked;
            chkSyntaxHighlight.IsEnabled = !chkHighContrast.IsChecked;
        }

        private void btnChangeColors_Click(object sender, RoutedEventArgs e)
        {
            ColorPickerDialog cpd = new ColorPickerDialog(App.ColorScheme, App.ColorScheme.MainColor);
            cpd.Owner = this;

            cpd.ShowDialog();

            if (cpd.DialogResult)
            {
                uiColor = cpd.SelectedColor;
            }
        }
    }
}
