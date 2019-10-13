using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using UiCore;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FlatWindow
    {
        /// <summary>Path to the AppData folder where settings are stored</summary>
        string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PathfinderJson");
        /// <summary>The path to the currently open file (if a file is loaded but this is empty, this means it is a new unsaved file)</summary>
        string filePath = "";
        /// <summary>Get or set if a file is currently open</summary>
        bool _sheetLoaded = false;
        /// <summary>The name of the character. This MUST match sheet.Name when a PathfinderSheet is loaded</summary>
        string fileTitle = "";
        /// <summary>Get or set if a file has unsaved changes</summary>
        bool isDirty = false;

        /// <summary>Get or set the current view ("tabs", "continuous", or "rawjson")</summary>
        string currentView = TABS_VIEW;

        const string TABS_VIEW = "tabs";
        const string CONTINUOUS_VIEW = "continuous";
        const string RAWJSON_VIEW = "rawjson";

        /// <summary>Get or set if the tabbed/continuous view has changes not synced with text editor</summary>
        bool _isTabsDirty = false;
        /// <summary>Get or set if the text editor has changes not synced with the tabbed/continuous view</summary>
        bool _isEditorDirty = false;
        /// <summary>Get or set if a file is currently being opened (and sheet loaded in)</summary>
        bool _isUpdating = false;
        /// <summary>Generic cancellation token, use for lengthy cancellable processes</summary>
        CancellationTokenSource cts = new CancellationTokenSource();

        // these are stored here as the program doesn't display these values to the user
        UserData ui;
        ArmorClass ac;
        string sheetid;

        #region Window/basic functions
        public MainWindow()
        {
            if (Directory.Exists(appDataPath))
            {
                App.Settings = Settings.LoadSettings(Path.Combine(appDataPath, "settings.json"));
            }
            else
            {
                Directory.CreateDirectory(appDataPath);
                SaveSettings();
            }

            InitializeComponent();
            App.ColorScheme = new ColorScheme(ColorsHelper.CreateFromHex(App.Settings.ThemeColor));
            UpdateAppearance();

            SetupTabs();
            selTabs.IsEnabled = false;
            //LoadGeneralTab();
            // spells tab
            selTabs[3].CanSelect = false;
            selTabs[3].Foreground = Colors.Gray.ToBrush();

            //switch (App.Settings.StartView.ToLowerInvariant())
            //{
            //    case "tabs":
            //        mnuTabs.IsChecked = true;
            //        mnuScroll.IsChecked = false;
            //        mnuRawJson.IsChecked = false;
            //        _useTabs = true;
            //        break;
            //    case "continuous":
            //        mnuTabs.IsChecked = false;
            //        mnuScroll.IsChecked = true;
            //        mnuRawJson.IsChecked = false;
            //        _useTabs = false;
            //        break;
            //    case "rawjson":
            //        mnuTabs.IsChecked = false;
            //        mnuScroll.IsChecked = false;
            //        mnuRawJson.IsChecked = true;
            //        _useTabs = true;
            //        break;
            //    default:
            //        mnuTabs.IsChecked = true;
            //        mnuScroll.IsChecked = false;
            //        mnuRawJson.IsChecked = false;
            //        _useTabs = true;
            //        break;
            //}

            ChangeView(App.Settings.StartView, false, true, false).Wait();

            if (App.Settings.RecentFiles.Count > 20)
            {
                // time to remove some old entries
                App.Settings.RecentFiles.Reverse();
                App.Settings.RecentFiles.RemoveRange(20, App.Settings.RecentFiles.Count - 20);
                App.Settings.RecentFiles.Reverse();
                SaveSettings();
            }

            mnuIndent.IsChecked = App.Settings.IndentJsonData;

            foreach (string file in App.Settings.RecentFiles)//.Reverse<string>())
            {
                AddRecentFile(file, false);
            }

            using (Stream s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("PathfinderJson.Json.xshd"))
            {
                if (s != null)
                {
                    using (XmlReader reader = new XmlTextReader(s))
                    {
                        txtEditRaw.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    }
                }
            }

            //if (App.Settings.UpdateAutoCheck)
            //{
            //    // I have it set to only auto-check for updates daily
            //    // so as to not overload GitHub's servers in the suuuuuuper unlikely chance that this app really takes off
            //    if (DateTime.Today.ToString("yyyy-MM-dd") != App.Settings.UpdateLastCheckDate)
            //    {
            //        // last checked before today
            //        Task<UpdateData> t = UpdateChecker.CheckForUpdatesAsync();
            //        t.Wait();
            //        UpdateData ud = t.Result;

            //        if (ud.HasUpdate)
            //        {
            //            UpdateDisplay uw = new UpdateDisplay(ud);
            //            uw.ShowDialog();
            //        }

            //        App.Settings.UpdateLastCheckDate = DateTime.Today.ToString("yyyy-MM-dd");
            //        SaveSettings();
            //    }
            //}
        }

        void SaveSettings()
        {
            App.Settings.Save(Path.Combine(appDataPath, "settings.json"));
        }

        void UpdateTitlebar()
        {
            if (string.IsNullOrEmpty(filePath))
            {
                if (_sheetLoaded)
                {
                    Title = "Pathfinder JSON - New File";
                }
                else
                {
                    Title = "Pathfinder JSON";
                }
            }
            else
            {
                if (App.Settings.PathInTitleBar)
                {
                    Title = "Pathfinder JSON - " + Path.GetFileName(filePath);
                }
                else
                {
                    Title = "Pathfinder JSON - " + fileTitle;
                }

                if (isDirty)
                {
                    Title += " *";
                }
            }
        }

        private void window_Activated(object sender, EventArgs e)
        {
            menu.Foreground = App.ColorScheme.ForegroundColor.ToBrush();
        }

        private void window_Deactivated(object sender, EventArgs e)
        {
            menu.Foreground = ColorsHelper.CreateFromHex("#404040").ToBrush();
        }

        private void FlatWindow_Closed(object sender, EventArgs e)
        {
            //Application.Current.Shutdown(0);
        }

        void SetIsDirty(bool isDirty = true, bool updateInternalValues = true)
        {
            if (isDirty)
            {
                this.isDirty = true;
                if (updateInternalValues) _isTabsDirty = true;
                UpdateTitlebar();
            }
            else
            {
                this.isDirty = false;
                if (updateInternalValues) _isTabsDirty = false;
                UpdateTitlebar();
            }
        }
        #endregion

        #region File / Help menus

        private async void mnuOpen_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.Title = "Open JSON";
            ofd.Filter = "JSON Sheet|*.json|All Files|*.*";

            if (ofd.ShowDialog() ?? false == true)
            {
                await LoadFile(ofd.FileName);
            }
        }

        private async void mnuSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                await SaveAsFile();
            }
            else
            {
                await SaveFile(filePath);
            }
        }

        private async void mnuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            await SaveAsFile();
        }

        async Task SaveFile(string file)
        {
            if (currentView == RAWJSON_VIEW)
            {
                txtEditRaw.Save(file);
            }
            else
            {
                await SyncEditorFromSheetAsync();
                txtEditRaw.Save(file);
                //PathfinderSheet ps = await CreatePathfinderSheetAsync();
                //ps.SaveJsonFile(file, App.Settings.IndentJsonData);
            }

            _isEditorDirty = false;
            _isTabsDirty = false;
            isDirty = false;
            UpdateTitlebar();
        }

        async Task SaveAsFile()
        {
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
            sfd.Title = "Save JSON Sheet As";
            sfd.Filter = "JSON Sheet|*.json";

            if (sfd.ShowDialog() ?? false == true)
            {
                await SaveFile(sfd.FileName);
                filePath = sfd.FileName;
                AddRecentFile(filePath, true);
                UpdateTitlebar();
            }
        }

        private async void mnuRevert_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                await LoadFile(filePath, false);
            }
        }

        private void mnuIndent_Click(object sender, RoutedEventArgs e)
        {
            if (mnuIndent.IsChecked)
            {
                mnuIndent.IsChecked = false;
                App.Settings.IndentJsonData = false;
            }
            else
            {
                mnuIndent.IsChecked = true;
                App.Settings.IndentJsonData = true;
            }

            SaveSettings();
        }

        private async void btnClose_Click(object sender, RoutedEventArgs e)
        {
            filePath = "";
            fileTitle = "";
            _sheetLoaded = false;
            UpdateTitlebar();

            await ChangeView(App.Settings.StartView, false, true, false);
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void mnuGithub_Click(object sender, RoutedEventArgs e)
        {
            About.OpenBrowser("https://github.com/JaykeBird/PathfinderJson/");
        }

        private void mnuAutoCheck_Click(object sender, RoutedEventArgs e)
        {
            if (mnuAutoCheck.IsChecked)
            {
                mnuAutoCheck.IsChecked = false;
                App.Settings.UpdateAutoCheck = false;
            }
            else
            {
                mnuAutoCheck.IsChecked = true;
                App.Settings.UpdateAutoCheck = false;
            }

            SaveSettings();
        }

        private async void mnuCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            UpdateData ud = await UpdateChecker.CheckForUpdatesAsync();
            if (ud.HasUpdate)
            {
                UpdateDisplay uw = new UpdateDisplay(ud);
                uw.ShowDialog();
            }
        }

        private void mnuAbout_Click(object sender, RoutedEventArgs e)
        {
            About a = new About();
            a.Owner = this;
            a.ShowDialog();
        }

        #endregion

        #region Recent files

        void AddRecentFile(string filename, bool storeInSettings = true)
        {
            if (storeInSettings && App.Settings.RecentFiles.Contains(filename))
            {
                return;
            }

            UiCore.MenuItem mi = new UiCore.MenuItem();
            string name = Path.GetFileName(filename);
            mi.Header = "_" + name;
            mi.ToolTip = filename;
            mi.Tag = filename;
            mi.Click += miRecentFile_Click;
            mnuRecent.Items.Insert(0, mi);

            if (storeInSettings)
            {
                App.Settings.RecentFiles.Add(filename);
                SaveSettings();
            }

            mnuRecentEmpty.Visibility = Visibility.Collapsed;
        }

        private async void miRecentFile_Click(object sender, RoutedEventArgs e)
        {
            await LoadFile((sender as UiCore.MenuItem).Tag as string, false);
        }

        private void mnuRecentClear_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.RecentFiles.Clear();
            SaveSettings();

            List<FrameworkElement> itemsToRemove = new List<FrameworkElement>();

            foreach (FrameworkElement item in mnuRecent.Items)
            {

                if (item is UiCore.MenuItem)
                {
                    if (item.Tag != null)
                    {
                        itemsToRemove.Add(item);
                    }
                }
            }

            foreach (var item in itemsToRemove)
            {
                mnuRecent.Items.Remove(item);
            }

            mnuRecentEmpty.Visibility = Visibility.Visible;
        }

        #endregion

        #region Tab bar / visuals

        void UpdateAppearance()
        {
            ApplyColorScheme(App.ColorScheme);
            menu.ApplyColorScheme(App.ColorScheme);
            toolbar.HighlightBrush = App.ColorScheme.HighlightColor.ToBrush();
            toolbar.SelectionBrush = App.ColorScheme.SelectionColor.ToBrush();
            toolbar.Background = App.ColorScheme.MainColor.ToBrush();

            selTabs.ApplyColorScheme(App.ColorScheme);
            (txtEditRaw.ContextMenu as UiCore.ContextMenu).ApplyColorScheme(App.ColorScheme);

            expUser.Background = App.ColorScheme.LightBackgroundColor.ToBrush();
            expUser.BorderBrush = App.ColorScheme.ThirdHighlightColor.ToBrush();
            expUser.BorderThickness = new Thickness(1);

            expPhysical.Background = App.ColorScheme.LightBackgroundColor.ToBrush();
            expPhysical.BorderBrush = App.ColorScheme.ThirdHighlightColor.ToBrush();
            expPhysical.BorderThickness = new Thickness(1);

            txtStrm.Background = App.ColorScheme.SecondHighlightColor.ToBrush();
            txtDexm.Background = App.ColorScheme.SecondHighlightColor.ToBrush();
            txtCham.Background = App.ColorScheme.SecondHighlightColor.ToBrush();
            txtConm.Background = App.ColorScheme.SecondHighlightColor.ToBrush();
            txtIntm.Background = App.ColorScheme.SecondHighlightColor.ToBrush();
            txtWism.Background = App.ColorScheme.SecondHighlightColor.ToBrush();

            edtFort.UpdateAppearance();
            edtReflex.UpdateAppearance();
            edtWill.UpdateAppearance();

            edtCmb.UpdateAppearance();
            edtCmd.UpdateAppearance();
            edtInit.UpdateAppearance();

            foreach (SkillEditor item in stkSkills.Children)
            {
                item.UpdateAppearance();
            }
        }

        void SetupTabs()
        {
            selTabs.AddItem(CreateTab("General"));
            selTabs.AddItem(CreateTab("Skills"));
            selTabs.AddItem(CreateTab("Combat"));
            selTabs.AddItem(CreateTab("Spells"));
            selTabs.AddItem(CreateTab("Feats/Abilities"));
            selTabs.AddItem(CreateTab("Items"));
            selTabs.AddItem(CreateTab("Notes"));

            SetAllTabsVisibility(Visibility.Collapsed);
        }

        SelectableItem CreateTab(string name, ImageSource image = null)
        {
            SelectableItem si = new SelectableItem();
            si.Height = 36;
            si.Text = name;
            si.Indent = 6;

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

        void LoadGeneralTab()
        {
            selTabs[0].IsSelected = true;
            LoadTab("General");
        }

        void LoadTab(string text)
        {
            if (currentView == TABS_VIEW)
            {
                grdGeneral.Visibility = Visibility.Collapsed;
                grdSkills.Visibility = Visibility.Collapsed;
                grdCombat.Visibility = Visibility.Collapsed;
                grdSpells.Visibility = Visibility.Collapsed;
                grdFeats.Visibility = Visibility.Collapsed;
                grdItems.Visibility = Visibility.Collapsed;
                grdNotes.Visibility = Visibility.Collapsed;

                //txtLoc.Text = text;
                switch (text)
                {
                    case "General":
                        grdGeneral.Visibility = Visibility.Visible;
                        break;
                    case "Skills":
                        grdSkills.Visibility = Visibility.Visible;
                        break;
                    case "Combat":
                        grdCombat.Visibility = Visibility.Visible;
                        break;
                    case "Spells":
                        grdSpells.Visibility = Visibility.Visible;
                        break;
                    case "Feats/Abilities":
                        grdFeats.Visibility = Visibility.Visible;
                        break;
                    case "Items":
                        grdItems.Visibility = Visibility.Visible;
                        break;
                    case "Notes":
                        grdNotes.Visibility = Visibility.Visible;
                        break;
                    default:
                        break;
                }
                scrSheet.ScrollToVerticalOffset(0);
            }
            else
            {
                SetAllTabsVisibility();
                //txtLoc.Text = "All Tabs";

                TextBlock control = titGeneral;

                switch (text)
                {
                    case "General":
                        control = titGeneral;
                        break;
                    case "Skills":
                        control = titSkills;
                        break;
                    case "Combat":
                        control = titCombat;
                        break;
                    case "Spells":
                        control = titSpells;
                        break;
                    case "Feats/Abilities":
                        control = titFeats;
                        break;
                    case "Items":
                        control = titItems;
                        break;
                    case "Notes":
                        control = titNotes;
                        break;
                    default:
                        break;
                }

                Point relativeLocation = control.TranslatePoint(new Point(0, 0), stkSheet);
                scrSheet.ScrollToVerticalOffset(relativeLocation.Y - 5);
            }
        }

        private void tabItem_Click(object sender, EventArgs e)
        {
            SelectableItem si = (SelectableItem)sender;

            if (si.CanSelect)
            {
                LoadTab(si.Text);
            }
        }

        void SetAllTabsVisibility(Visibility visibility = Visibility.Visible)
        {
            grdGeneral.Visibility = visibility;
            grdSkills.Visibility = visibility;
            grdCombat.Visibility = visibility;
            grdSpells.Visibility = visibility;
            grdFeats.Visibility = visibility;
            grdItems.Visibility = visibility;
            grdNotes.Visibility = visibility;
        }

        private void mnuColors_Click(object sender, RoutedEventArgs e)
        {
            ColorPickerDialog cpd = new ColorPickerDialog(App.ColorScheme, App.ColorScheme.MainColor);
            cpd.Owner = this;
            cpd.ShowDialog();

            if (cpd.DialogResult)
            {
                App.ColorScheme = new ColorScheme(cpd.SelectedColor);
                App.Settings.ThemeColor = cpd.SelectedColor.GetHexString();
                App.Settings.Save(Path.Combine(appDataPath, "settings.json"));
                UpdateAppearance();
            }
        }

        #endregion

        #region View options

        async Task ChangeView(string view, bool updateSheet = true, bool displayEmptyMessage = false, bool saveSettings = true)
        {
            view = view.ToLowerInvariant();

            if (view != CONTINUOUS_VIEW && view != TABS_VIEW && view != RAWJSON_VIEW)
            {
                //await ChangeView(TABS_VIEW, updateSheet, displayEmptyMessage, saveSettings);
                return;
            }

            currentView = view;

            if (saveSettings)
            {
                App.Settings.StartView = view;
                SaveSettings();
            }

            switch (view)
            {
                case CONTINUOUS_VIEW:
                    SetAllTabsVisibility();
                    LoadGeneralTab();

                    txtEditRaw.Visibility = Visibility.Collapsed;
                    mnuEdit.Visibility = Visibility.Collapsed;
                    colTabs.Width = new GridLength(120);

                    mnuTabs.IsChecked = false;
                    mnuScroll.IsChecked = true;
                    mnuRawJson.IsChecked = false;

                    if (_isEditorDirty && updateSheet)
                    {
                        // update sheet from editor
                        await SyncSheetFromEditorAsync();
                        _isEditorDirty = false;
                    }
                    break;
                case TABS_VIEW:
                    LoadGeneralTab();

                    txtEditRaw.Visibility = Visibility.Collapsed;
                    mnuEdit.Visibility = Visibility.Collapsed;
                    colTabs.Width = new GridLength(120);

                    mnuTabs.IsChecked = true;
                    mnuScroll.IsChecked = false;
                    mnuRawJson.IsChecked = false;

                    if (_isEditorDirty && updateSheet)
                    {
                        // update sheet from editor
                        await SyncSheetFromEditorAsync();
                        _isEditorDirty = false;
                    }
                    break;
                case RAWJSON_VIEW:
                    txtEditRaw.Visibility = Visibility.Visible;
                    mnuEdit.Visibility = Visibility.Visible;
                    colTabs.Width = new GridLength(0);

                    mnuTabs.IsChecked = false;
                    mnuScroll.IsChecked = false;
                    mnuRawJson.IsChecked = true;

                    if (_isTabsDirty && updateSheet)
                    {
                        await SyncEditorFromSheetAsync();
                        _isTabsDirty = false;
                    }
                    break;
                default:
                    await ChangeView(TABS_VIEW, updateSheet, true, saveSettings);
                    break;
            }

            if (displayEmptyMessage)
            {
                lblNoSheet.Visibility = Visibility.Visible;
                selTabs.IsEnabled = false;
                txtEditRaw.IsEnabled = false;
                SetAllTabsVisibility(Visibility.Collapsed);
            }
            else
            {
                lblNoSheet.Visibility = Visibility.Collapsed;
                selTabs.IsEnabled = true;
                txtEditRaw.IsEnabled = true;
            }
        }

        private async void mnuScroll_Click(object sender, RoutedEventArgs e)
        {
            await ChangeView(CONTINUOUS_VIEW, true, !_sheetLoaded);
        }

        private async void mnuTabs_Click(object sender, RoutedEventArgs e)
        {
            await ChangeView(TABS_VIEW, true, !_sheetLoaded);
        }

        private async void mnuRawJson_Click(object sender, RoutedEventArgs e)
        {
            await ChangeView(RAWJSON_VIEW, true, !_sheetLoaded);
        }

        private void MnuFilename_Click(object sender, RoutedEventArgs e)
        {
            if (mnuFilename.IsChecked)
            {
                mnuFilename.IsChecked = false;
            }
            else
            {
                mnuFilename.IsChecked = true;
            }

            App.Settings.PathInTitleBar = mnuFilename.IsChecked;
            UpdateTitlebar();
        }

        #endregion

        #region JSON Editor

        private void mnuUndo_Click(object sender, RoutedEventArgs e)
        {
            txtEditRaw.Undo();
        }

        private void mnuRedo_Click(object sender, RoutedEventArgs e)
        {
            txtEditRaw.Redo();
        }

        private void mnuCopy_Click(object sender, RoutedEventArgs e)
        {
            txtEditRaw.Copy();
        }

        private void mnuCut_Click(object sender, RoutedEventArgs e)
        {
            txtEditRaw.Cut();
        }

        private void mnuPaste_Click(object sender, RoutedEventArgs e)
        {
            txtEditRaw.Paste();
        }

        private void mnuSelectAll_Click(object sender, RoutedEventArgs e)
        {
            txtEditRaw.SelectAll();
        }

        private void mnuDelete_Click(object sender, RoutedEventArgs e)
        {
            txtEditRaw.Delete();
        }

        private void mnuWordWrap_Click(object sender, RoutedEventArgs e)
        {
            if (mnuWordWrap.IsChecked)
            {
                mnuWordWrap.IsChecked = false;
                txtEditRaw.WordWrap = false;
            }
            else
            {
                mnuWordWrap.IsChecked = true;
                txtEditRaw.WordWrap = true;
            }
        }

        private void txtEditRaw_TextChanged(object sender, EventArgs e)
        {
            _isEditorDirty = true;
        }

        #endregion

        #region Load File
        async Task LoadFile(string filename, bool addToRecent = true)
        {
            try
            {
                PathfinderSheet ps = PathfinderSheet.LoadJsonFile(filename);
                filePath = filename;
                fileTitle = ps.Name;
                _sheetLoaded = true;

                isDirty = false;
                _isEditorDirty = false;
                _isTabsDirty = false;

                UpdateTitlebar();

                txtEditRaw.Load(filename);
                await ChangeView(App.Settings.StartView, false, false);
                await LoadPathfinderSheetAsync(ps);
            }
            catch (FileFormatException)
            {
                MessageBox.Show("The file \"" + filename + "\" does not appear to be a JSON file. Check the file in Notepad or another text editor to make sure it's not corrupted.",
                    "File Format Error", MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }
            catch (InvalidOperationException e)
            {
                if (!e.Message.Contains("error context error is different to requested error"))
                {
                    MessageBox.Show("The file \"" + filename + "\" does not appear to be a JSON file. Check the file in Notepad or another text editor to make sure it's not corrupted.",
                        "File Format Error", MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                else { throw; }
            }

            if (addToRecent) AddRecentFile(filename);
        }

        async Task LoadPathfinderSheetAsync(PathfinderSheet sheet)
        {
            //lblNoSheet.Visibility = Visibility.Collapsed;
            //selTabs.IsEnabled = true;
            //txtEditRaw.IsEnabled = true;

            //switch (App.Settings.StartView.ToLowerInvariant())
            //{
            //    case "tabs":
            //        mnuTabs.IsChecked = true;
            //        mnuScroll.IsChecked = false;
            //        mnuRawJson.IsChecked = false;
            //        _useTabs = true;

            //        LoadGeneralTab();
            //        break;
            //    case "continuous":
            //        mnuTabs.IsChecked = false;
            //        mnuScroll.IsChecked = true;
            //        mnuRawJson.IsChecked = false;
            //        _useTabs = false;

            //        SetAllTabsVisibility();
            //        break;
            //    case "rawjson":
            //        mnuTabs.IsChecked = false;
            //        mnuScroll.IsChecked = false;
            //        mnuRawJson.IsChecked = true;
            //        _useTabs = true;

            //        txtEditRaw.Visibility = Visibility.Visible;
            //        mnuEdit.Visibility = Visibility.Visible;
            //        colTabs.Width = new GridLength(0);
            //        break;
            //    default:
            //        mnuTabs.IsChecked = true;
            //        mnuScroll.IsChecked = false;
            //        mnuRawJson.IsChecked = false;
            //        _useTabs = true;

            //        LoadGeneralTab();
            //        break;
            //}

            _isUpdating = true;

            // General tab
            txtPlayerName.Text = sheet.Player.DisplayName;

            string email = "";

            foreach (UserData.Email item in sheet.Player.Emails)
            {
                if (item.Type == "account")
                {
                    email = item.Value;
                }
            }
            if (string.IsNullOrEmpty(email))
            {
                try
                {
                    email = sheet.Player.Emails[0].Value;
                }
                catch (IndexOutOfRangeException) { }
            }
            txtPlayerEmail.Text = email;

            try
            {
                ImageSource iss = new BitmapImage(new Uri(sheet.Player.Photos[0].Value));
                imgPlayer.Source = iss;
            }
            catch (IndexOutOfRangeException) { }

            ui = sheet.Player;
            ac = sheet.AC;
            sheetid = sheet.Id;

            txtCharacter.Text = sheet.Name;
            txtLevel.Text = sheet.Level;
            txtAlignment.Text = sheet.Alignment;
            txtHomeland.Text = sheet.Homeland;
            txtDeity.Text = sheet.Deity;

            txtPhyRace.Text = sheet.Race;
            txtPhyGender.Text = sheet.Gender;
            txtPhySize.Text = sheet.Size;
            txtPhyAge.Text = sheet.Age;
            txtPhyHeight.Text = sheet.Height;
            txtPhyWeight.Text = sheet.Weight;
            txtPhyHair.Text = sheet.Hair;
            txtPhyEyes.Text = sheet.Eyes;

            txtStr.Text = sheet.Strength.ToString();
            txtDex.Text = sheet.Dexterity.ToString();
            txtCha.Text = sheet.Charisma.ToString();
            txtCon.Text = sheet.Constitution.ToString();
            txtInt.Text = sheet.Intelligence.ToString();
            txtWis.Text = sheet.Wisdom.ToString();

            txtStrm.Text = CalculateModifier(sheet.Strength);
            txtDexm.Text = CalculateModifier(sheet.Dexterity);
            txtCham.Text = CalculateModifier(sheet.Charisma);
            txtConm.Text = CalculateModifier(sheet.Constitution);
            txtIntm.Text = CalculateModifier(sheet.Intelligence);
            txtWism.Text = CalculateModifier(sheet.Wisdom);

            if (sheet.Saves.ContainsKey("fort")) edtFort.LoadModifier(sheet.Saves["fort"], txtConm.Text);
            else edtFort.LoadModifier(new CompoundModifier(), txtConm.Text);

            if (sheet.Saves.ContainsKey("reflex")) edtReflex.LoadModifier(sheet.Saves["reflex"], txtDexm.Text);
            else edtReflex.LoadModifier(new CompoundModifier(), txtDexm.Text);

            if (sheet.Saves.ContainsKey("will")) edtWill.LoadModifier(sheet.Saves["will"], txtWism.Text);
            else edtWill.LoadModifier(new CompoundModifier(), txtWism.Text);

            txtHpTotal.Text = sheet.HP.Total;
            txtHpWounds.Text = sheet.HP.Wounds;
            txtHpNl.Text = sheet.HP.NonLethal;

            txtLanguages.Text = sheet.Languages;

            // Combat tab
            edtAc.LoadArmorClass(sheet.AC, txtDexm.Text);
            edtInit.LoadModifier(sheet.Initiative, txtDexm.Text);
            txtBab.Text = sheet.BaseAttackBonus;
            edtCmb.LoadModifier(sheet.CombatManeuverBonus, txtStrm.Text, "", txtBab.Text);
            edtCmd.LoadModifier(sheet.CombatManeuverDefense, txtStrm.Text, txtDexm.Text, txtBab.Text);
            txtDr.Text = sheet.DamageReduction;
            txtResist.Text = sheet.Resistances;

            selMelee.Clear();
            foreach (Weapon item in sheet.MeleeWeapons)
            {
                WeaponEditor we = new WeaponEditor();
                we.ContentChanged += editor_ContentChanged;
                we.LoadWeapon(item);
                selMelee.AddItem(we);
            }

            selRanged.Clear();
            foreach (Weapon item in sheet.RangedWeapons)
            {
                WeaponEditor we = new WeaponEditor();
                we.ContentChanged += editor_ContentChanged;
                we.LoadWeapon(item);
                selRanged.AddItem(we);
            }

            selAcItem.Clear();
            foreach (AcItem item in sheet.AC.Items)
            {
                AcItemEditor ae = new AcItemEditor();
                ae.ContentChanged += editor_ContentChanged;
                ae.LoadAcItem(item);
                selAcItem.AddItem(ae);
            }

            AcItem total = sheet.AC.ItemTotals;
            txtAcBonus.Text = total.Bonus;
            txtAcPenalty.Text = total.ArmorCheckPenalty;
            txtAcSpellFailure.Text = total.SpellFailure;
            txtAcWeight.Text = total.Weight;

            // Feats/Abilities tab
            selFeats.Clear();
            foreach (Feat item in sheet.Feats)
            {
                FeatEditor fe = new FeatEditor();
                fe.ContentChanged += editor_ContentChanged;
                fe.LoadFeat(item);
                selFeats.AddItem(fe);
            }

            selAbilities.Clear();
            foreach (SpecialAbility item in sheet.SpecialAbilities)
            {
                AbilityEditor ae = new AbilityEditor();
                ae.ContentChanged += editor_ContentChanged;
                ae.LoadAbility(item);
                selAbilities.AddItem(ae);
            }

            selTraits.Clear();
            foreach (SpecialAbility item in sheet.Traits)
            {
                AbilityEditor ae = new AbilityEditor();
                ae.ContentChanged += editor_ContentChanged;
                ae.LoadAbility(item);
                selTraits.AddItem(ae);
            }

            // Equipment tab

            Dictionary<string, string> money = sheet.Money;
            txtMoneyCp.Text = money.ContainsKey("cp") ? money["cp"] : "0";
            txtMoneySp.Text = money.ContainsKey("sp") ? money["sp"] : "0";
            txtMoneyGp.Text = money.ContainsKey("gp") ? money["gp"] : "0";
            txtMoneyPp.Text = money.ContainsKey("pp") ? money["pp"] : "0";
            txtGemsArt.Text = money.ContainsKey("gems") ? money["gems"] : "";
            txtOtherTreasure.Text = money.ContainsKey("other") ? money["other"] : "";

            selEquipment.Clear();
            foreach (Equipment item in sheet.Equipment)
            {
                ItemEditor ie = new ItemEditor();
                ie.ContentChanged += editor_ContentChanged;
                ie.LoadEquipment(item);
                selEquipment.AddItem(ie);
            }

            // Skills tab
            txtSkillModifiers.Text = sheet.SkillConditionalModifiers;

            // trying to do this asynchronously... key word being "trying"
            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            var ses = await Task<List<SkillEditor>>.Factory.StartNew(() => SkillEditorFactory.CreateEditors(sheet), cts.Token, TaskCreationOptions.None, scheduler);

            stkSkills.Children.Clear();

            foreach (SkillEditor item in ses)
            {

                string modifier = "";

                switch (item.SkillAbility)
                {
                    case "DEX":
                        modifier = txtDexm.Text;
                        break;
                    case "INT":
                        modifier = txtIntm.Text;
                        break;
                    case "CHA":
                        modifier = txtCham.Text;
                        break;
                    case "STR":
                        modifier = txtStrm.Text;
                        break;
                    case "WIS":
                        modifier = txtWism.Text;
                        break;
                    case "CON":
                        modifier = txtConm.Text;
                        break;
                    default:
                        break;
                }
                item.LoadModifier(modifier);

                item.ContentChanged += editor_ContentChanged;

                stkSkills.Children.Add(item);
                item.UpdateAppearance();
            }

            // Notes tab
            txtNotes.Text = sheet.Notes;

            _isUpdating = false;
        }

        string AttemptCalculateModifier(string score)
        {
            try
            {
                return CalculateModifier(int.Parse(score));
            }
            catch (FormatException)
            {
                return "0";
            }
        }

        string CalculateModifier(int score)
        {
            int r = (int)Math.Floor((score - 10) / 2d);
            if (r >= 0) return "+" + r.ToString(); else return r.ToString();
        }
        #endregion

        #region Sync Editors / update sheet / CreatePathfinderSheetAsync

        private void mnuUpdate_Click(object sender, RoutedEventArgs e)
        {
            _isUpdating = true;

            txtStrm.Text = AttemptCalculateModifier(txtStr.Text);
            txtDexm.Text = AttemptCalculateModifier(txtDex.Text);
            txtCham.Text = AttemptCalculateModifier(txtCha.Text);
            txtConm.Text = AttemptCalculateModifier(txtCon.Text);
            txtIntm.Text = AttemptCalculateModifier(txtInt.Text);
            txtWism.Text = AttemptCalculateModifier(txtWis.Text);

            edtFort.UpdateCoreModifier(txtConm.Text);
            edtReflex.UpdateCoreModifier(txtDexm.Text);
            edtWill.UpdateCoreModifier(txtWism.Text);

            edtAc.UpdateCoreModifier(txtDexm.Text);
            edtInit.UpdateCoreModifier(txtDexm.Text);
            edtCmb.UpdateCoreModifier(txtStrm.Text, "", txtBab.Text);
            edtCmd.UpdateCoreModifier(txtStrm.Text, txtDexm.Text, txtBab.Text);

            bool updateTotals = mnuUpdateTotals.IsChecked;

            foreach (SkillEditor item in stkSkills.Children)
            {

                string modifier = "";

                switch (item.SkillAbility)
                {
                    case "DEX":
                        modifier = txtDexm.Text;
                        break;
                    case "INT":
                        modifier = txtIntm.Text;
                        break;
                    case "CHA":
                        modifier = txtCham.Text;
                        break;
                    case "STR":
                        modifier = txtStrm.Text;
                        break;
                    case "WIS":
                        modifier = txtWism.Text;
                        break;
                    case "CON":
                        modifier = txtConm.Text;
                        break;
                    default:
                        break;
                }
                item.LoadModifier(modifier);

                if (updateTotals)
                {
                    item.UpdateTotals();
                }
            }

            if (updateTotals)
            {
                edtFort.UpdateTotal();
                edtReflex.UpdateTotal();
                edtWill.UpdateTotal();

                edtAc.UpdateTotal();
                edtInit.UpdateTotal();
                edtCmb.UpdateTotal();
                edtCmd.UpdateTotal();
            }

            _isUpdating = false;

            SetIsDirty();
        }

        async Task SyncSheetFromEditorAsync()
        {
            PathfinderSheet ps = PathfinderSheet.LoadJsonText(txtEditRaw.Text);
            await LoadPathfinderSheetAsync(ps);
        }

        async Task SyncEditorFromSheetAsync()
        {
            PathfinderSheet ps = await CreatePathfinderSheetAsync();
            txtEditRaw.Text = ps.SaveJsonText(App.Settings.IndentJsonData);
        }

        private async void mnuRefresh_Click(object sender, RoutedEventArgs e)
        {
            await SyncSheetFromEditorAsync();
        }

        private async void mnuRefreshEditor_Click(object sender, RoutedEventArgs e)
        {
            await SyncEditorFromSheetAsync();
        }

        private async Task<PathfinderSheet> CreatePathfinderSheetAsync()
        {
            await Task.Delay(10);
            //return await Task.Run(() => {
            PathfinderSheet sheet = new PathfinderSheet();

            sheet.Player = ui;
            sheet.Id = sheetid;

            sheet.Notes = txtNotes.Text;

            sheet.Name = txtCharacter.Text;
            sheet.Level = txtLevel.Text;
            sheet.Alignment = txtAlignment.Text;
            sheet.Homeland = txtHomeland.Text;
            sheet.Deity = txtDeity.Text;

            sheet.Race = txtPhyRace.Text;
            sheet.Gender = txtPhyGender.Text;
            sheet.Size = txtPhySize.Text;
            sheet.Age = txtPhyAge.Text;
            sheet.Height = txtPhyHeight.Text;
            sheet.Weight = txtPhyWeight.Text;
            sheet.Hair = txtPhyHair.Text;
            sheet.Eyes = txtPhyEyes.Text;

            Dictionary<string, string> abilities = new Dictionary<string, string>();
            abilities.Add("str", txtStr.Text);
            abilities.Add("dex", txtDex.Text);
            abilities.Add("cha", txtCha.Text);
            abilities.Add("con", txtCon.Text);
            abilities.Add("int", txtInt.Text);
            abilities.Add("wis", txtWis.Text);
            sheet.RawAbilities = abilities;

            Dictionary<string, CompoundModifier> saves = new Dictionary<string, CompoundModifier>();
            saves.Add("fort", edtFort.GetModifier());
            saves.Add("reflex", edtReflex.GetModifier());
            saves.Add("will", edtWill.GetModifier());
            sheet.Saves = saves;

            sheet.HP = new HP();
            sheet.HP.Total = txtHpTotal.Text;
            sheet.HP.Wounds = txtHpWounds.Text;
            sheet.HP.NonLethal = txtHpNl.Text;

            sheet.Languages = txtLanguages.Text;

            sheet.AC = ac;
            sheet.Initiative = edtInit.GetModifier();
            sheet.BaseAttackBonus = txtBab.Text;
            sheet.CombatManeuverBonus = edtCmb.GetModifier();
            sheet.CombatManeuverDefense = edtCmd.GetModifier();
            sheet.DamageReduction = txtDr.Text;
            sheet.Resistances = txtResist.Text;

            sheet.MeleeWeapons = new List<Weapon>();
            foreach (WeaponEditor item in selMelee.GetItemsAsType<WeaponEditor>())
            {
                sheet.MeleeWeapons.Add(item.GetWeapon());
            }

            sheet.RangedWeapons = new List<Weapon>();
            foreach (WeaponEditor item in selRanged.GetItemsAsType<WeaponEditor>())
            {
                sheet.RangedWeapons.Add(item.GetWeapon());
            }

            sheet.AC.Items = new List<AcItem>();
            foreach (AcItemEditor item in selAcItem.GetItemsAsType<AcItemEditor>())
            {
                sheet.AC.Items.Add(item.GetAcItem());
            }

            AcItem totals = new AcItem();
            totals.Bonus = txtAcBonus.Text;
            totals.ArmorCheckPenalty = txtAcPenalty.Text;
            totals.SpellFailure = txtAcSpellFailure.Text;
            totals.Weight = txtAcWeight.Text;
            sheet.AC.ItemTotals = totals;

            sheet.Feats = new List<Feat>();
            foreach (FeatEditor item in selFeats.GetItemsAsType<FeatEditor>())
            {
                sheet.Feats.Add(item.GetFeat());
            }

            sheet.SpecialAbilities = new List<SpecialAbility>();
            foreach (AbilityEditor item in selAbilities.GetItemsAsType<AbilityEditor>())
            {
                sheet.SpecialAbilities.Add(item.GetAbility());
            }

            sheet.Traits = new List<SpecialAbility>();
            foreach (AbilityEditor item in selTraits.GetItemsAsType<AbilityEditor>())
            {
                sheet.Traits.Add(item.GetAbility());
            }

            sheet.Money = new Dictionary<string, string>();
            sheet.Money.Add("cp", txtMoneyCp.Text);
            sheet.Money.Add("sp", txtMoneySp.Text);
            sheet.Money.Add("gp", txtMoneyGp.Text);
            sheet.Money.Add("pp", txtMoneyPp.Text);
            sheet.Money.Add("gems", txtGemsArt.Text);
            sheet.Money.Add("other", txtOtherTreasure.Text);

            sheet.Equipment = new List<Equipment>();
            foreach (ItemEditor item in selEquipment.GetItemsAsType<ItemEditor>())
            {
                sheet.Equipment.Add(item.GetEquipment());
            }

            Dictionary<string, Skill> skills = new Dictionary<string, Skill>();
            foreach (SkillEditor item in stkSkills.Children)
            {
                skills.Add(item.SkillInternalName, item.GetSkillData());
            }

            if (!string.IsNullOrWhiteSpace(txtSkillModifiers.Text))
            {
                sheet.SkillConditionalModifiers = txtSkillModifiers.Text;
                skills.Add("conditionalModifiers", new Skill("conditionalModifiers", txtSkillModifiers.Text));
            }

            sheet.Skills = skills;

            return sheet;
            //});
        }

        #endregion

        #region General sheet event handlers

        private void textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isUpdating)
            {
                SetIsDirty();
            }
        }

        private void editor_ContentChanged(object sender, EventArgs e)
        {
            if (!_isUpdating)
            {
                SetIsDirty();
            }
        }

        private void txtStr_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isUpdating)
            {
                txtStrm.Text = AttemptCalculateModifier(txtStr.Text);
                txtDexm.Text = AttemptCalculateModifier(txtDex.Text);
                txtCham.Text = AttemptCalculateModifier(txtCha.Text);
                txtConm.Text = AttemptCalculateModifier(txtCon.Text);
                txtIntm.Text = AttemptCalculateModifier(txtInt.Text);
                txtWism.Text = AttemptCalculateModifier(txtWis.Text);

                SetIsDirty();
            }
        }

        private void txtCharacter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isUpdating)
            {
                if (fileTitle != txtCharacter.Text)
                {
                    fileTitle = txtCharacter.Text;
                }

                SetIsDirty(); // <-- this includes updating the title bar
            }
        }

        #endregion

        #region Feats/Abilities editors

        private void btnAddFeat_Click(object sender, EventArgs e)
        {
            FeatEditor fe = new FeatEditor();
            fe.ContentChanged += editor_ContentChanged;
            selFeats.AddItem(fe);

            expFeats.IsExpanded = true;
            fe.BringIntoView();
            fe.IsSelected = true;

            SetIsDirty();
        }

        private void btnDeleteFeat_Click(object sender, EventArgs e)
        {
            selFeats.RemoveSelectedItems();
            SetIsDirty();
        }

        private void btnDeselectFeat_Click(object sender, EventArgs e)
        {
            selFeats.DeselectAll();
        }

        private void expFeats_Expanded(object sender, RoutedEventArgs e)
        {
            if (selFeats != null) selFeats.Visibility = Visibility.Visible;
        }

        private void expFeats_Collapsed(object sender, RoutedEventArgs e)
        {
            if (selFeats != null) selFeats.Visibility = Visibility.Collapsed;
        }


        private void btnAddAbility_Click(object sender, EventArgs e)
        {
            AbilityEditor ae = new AbilityEditor();
            ae.ContentChanged += editor_ContentChanged;
            selAbilities.AddItem(ae);

            expabilities.IsExpanded = true;
            ae.BringIntoView();
            ae.IsSelected = true;

            SetIsDirty();
        }

        private void btnDeleteAbility_Click(object sender, EventArgs e)
        {
            selAbilities.RemoveSelectedItems();
            SetIsDirty();
        }

        private void btnDeselectAbility_Click(object sender, EventArgs e)
        {
            selAbilities.DeselectAll();
        }

        private void expabilities_Expanded(object sender, RoutedEventArgs e)
        {
            if (selAbilities != null) selAbilities.Visibility = Visibility.Visible;
        }

        private void expabilities_Collapsed(object sender, RoutedEventArgs e)
        {
            if (selAbilities != null) selAbilities.Visibility = Visibility.Collapsed;
        }


        private void btnAddTrait_Click(object sender, EventArgs e)
        {
            AbilityEditor ae = new AbilityEditor();
            ae.ContentChanged += editor_ContentChanged;
            selTraits.AddItem(ae);

            expTraits.IsExpanded = true;
            ae.BringIntoView();
            ae.IsSelected = true;

            SetIsDirty();
        }

        private void btnDeleteTrait_Click(object sender, EventArgs e)
        {
            selTraits.RemoveSelectedItems();
            SetIsDirty();
        }

        private void btnDeselectTrait_Click(object sender, EventArgs e)
        {
            selTraits.DeselectAll();
        }

        private void expTraits_Expanded(object sender, RoutedEventArgs e)
        {
            if (selTraits != null) selTraits.Visibility = Visibility.Visible;
        }

        private void expTraits_Collapsed(object sender, RoutedEventArgs e)
        {
            if (selTraits != null) selTraits.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Equipment/combat item list editors

        private void ExpMelee_Expanded(object sender, RoutedEventArgs e)
        {
            if (selMelee != null) selMelee.Visibility = Visibility.Visible;
        }

        private void ExpMelee_Collapsed(object sender, RoutedEventArgs e)
        {
            if (selMelee != null) selMelee.Visibility = Visibility.Collapsed;
        }

        private void BtnAddMelee_Click(object sender, EventArgs e)
        {
            WeaponEditor we = new WeaponEditor();
            we.ContentChanged += editor_ContentChanged;
            selMelee.AddItem(we);

            expMelee.IsExpanded = true;
            we.BringIntoView();
            we.IsSelected = true;

            SetIsDirty();
        }

        private void BtnDeleteMelee_Click(object sender, EventArgs e)
        {
            selMelee.RemoveSelectedItems();
            SetIsDirty();
        }

        private void BtnDeselectMelee_Click(object sender, EventArgs e)
        {
            selMelee.DeselectAll();
        }


        private void ExpRanged_Expanded(object sender, RoutedEventArgs e)
        {
            if (selRanged != null) selRanged.Visibility = Visibility.Visible;
        }

        private void ExpRanged_Collapsed(object sender, RoutedEventArgs e)
        {
            if (selRanged != null) selRanged.Visibility = Visibility.Collapsed;
        }

        private void BtnAddRanged_Click(object sender, EventArgs e)
        {
            WeaponEditor we = new WeaponEditor();
            we.ContentChanged += editor_ContentChanged;
            selRanged.AddItem(we);

            expRanged.IsExpanded = true;
            we.BringIntoView();
            we.IsSelected = true;

            SetIsDirty();
        }

        private void BtnDeleteRanged_Click(object sender, EventArgs e)
        {
            selRanged.RemoveSelectedItems();
            SetIsDirty();
        }

        private void BtnDeselectRanged_Click(object sender, EventArgs e)
        {
            selRanged.DeselectAll();
        }

        private void expEquipment_Expanded(object sender, RoutedEventArgs e)
        {
            if (selEquipment != null) selEquipment.Visibility = Visibility.Visible;
        }

        private void expEquipment_Collapsed(object sender, RoutedEventArgs e)
        {
            if (selEquipment != null) selEquipment.Visibility = Visibility.Collapsed;
        }

        private void btnAddEquipment_Click(object sender, EventArgs e)
        {
            ItemEditor ie = new ItemEditor();
            ie.ContentChanged += editor_ContentChanged;
            selEquipment.AddItem(ie);

            expEquipment.IsExpanded = true;
            ie.BringIntoView();
            ie.IsSelected = true;

            SetIsDirty();
        }

        private void btnDeleteEquipment_Click(object sender, EventArgs e)
        {
            selEquipment.RemoveSelectedItems();
            SetIsDirty();
        }

        private void btnDeselectEquipment_Click(object sender, EventArgs e)
        {
            selEquipment.DeselectAll();
        }

        private void expAcItem_Expanded(object sender, RoutedEventArgs e)
        {
            if (selAcItem != null) selAcItem.Visibility = Visibility.Visible;
        }

        private void expAcItem_Collapsed(object sender, RoutedEventArgs e)
        {
            if (selAcItem != null) selAcItem.Visibility = Visibility.Collapsed;
        }

        private void btnAddAcItem_Click(object sender, EventArgs e)
        {
            AcItemEditor ae = new AcItemEditor();
            ae.ContentChanged += editor_ContentChanged;
            selAcItem.AddItem(ae);

            expAcItem.IsExpanded = true;
            ae.BringIntoView();
            ae.IsSelected = true;

            SetIsDirty();
        }

        private void btnDeleteAcItem_Click(object sender, EventArgs e)
        {
            selAcItem.RemoveSelectedItems();
            SetIsDirty();
        }

        private void btnDeselectAcItem_Click(object sender, EventArgs e)
        {
            selAcItem.DeselectAll();
        }

        #endregion

    }
}
