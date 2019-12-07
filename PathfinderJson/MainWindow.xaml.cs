using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;
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
using System.Windows.Threading;
using System.Xml;
using UiCore;
using MenuItem = System.Windows.Controls.MenuItem;

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
        /// <summary>The name of the character. This MUST match txtCharacterName's text</summary>
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
        /// <summary>The search panel associated with the raw JSON editor</summary>
        SearchPanel sp;

        // functions for handling undo/redo
        FixedSizeStack<PathfinderSheet> undoItems = new FixedSizeStack<PathfinderSheet>(20);
        FixedSizeStack<PathfinderSheet> redoItems = new FixedSizeStack<PathfinderSheet>(20);
        TextBox lastEditedBox = null;
        DispatcherTimer undoSetTimer = new DispatcherTimer();

        // these are stored here as the program doesn't display these values to the user
        UserData ud;
        ArmorClass ac;
        string sheetid;

        #region Window/basic functions

        #region Constructor
        public MainWindow()
        {
            //if (Directory.Exists(appDataPath))
            //{
            //    App.Settings = Settings.LoadSettings(Path.Combine(appDataPath, "settings.json"));
            //}
            //else
            //{
            //    Directory.CreateDirectory(appDataPath);
            //    SaveSettings();
            //}

            undoSetTimer.Interval = new TimeSpan(0, 0, 3);
            undoSetTimer.Tick += UndoSetTimer_Tick;

            InitializeComponent();
            App.ColorScheme = new ColorScheme(ColorsHelper.CreateFromHex(App.Settings.ThemeColor));
            UpdateAppearance();

            SetupTabs();
            selTabs.IsEnabled = false;
            //LoadGeneralTab();

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

            SearchPanel p = SearchPanel.Install(txtEditRaw);
            p.FontFamily = SystemFonts.MessageFontFamily; // so it isn't a fixed-width font lol
            sp = p;

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

        #endregion

        #region Other Base Functions

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
            }

            if (isDirty)
            {
                Title += " *";
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

        /// <summary>
        /// Set if the current sheet has unsaved changes. Also updates the title bar and by default sets the tabbed/continuous view as unsynced.
        /// </summary>
        /// <param name="isDirty">Set if the sheet is dirty (has unsaved changes).</param>
        /// <param name="updateInternalValues">Set if the value for the tabbed/continuous view should be marked as unsynced with the text editor.</param>
        void SetIsDirty(bool isDirty = true, bool updateInternalValues = true)
        {
            if (!_sheetLoaded) // if no sheet is loaded, nothing is gonna happen lol
            {
                isDirty = false;
            }

            bool update = isDirty != this.isDirty;

            if (isDirty)
            {
                this.isDirty = true;
                if (updateInternalValues) _isTabsDirty = true;
            }
            else
            {
                this.isDirty = false;
                if (updateInternalValues) _isTabsDirty = false;
            }

            if (update) UpdateTitlebar();
        }

        #endregion

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
            SetIsDirty(false);

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

        private void mnuFeedback_Click(object sender, RoutedEventArgs e)
        {
            About.OpenBrowser("https://github.com/JaykeBird/PathfinderJson/issues/new/choose");
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
                    md.Owner = this;
                    md.ShowDialog("There are no updates available. You're on the latest release!", App.ColorScheme, this, "Check for Updates", false, MessageDialogImage.Hand);
                }
            }
            catch (System.Net.WebException)
            {
                MessageDialog md = new MessageDialog(App.ColorScheme);
                md.Owner = this;
                md.ShowDialog("Could not check for updates. Make sure you're connected to the Internet.", App.ColorScheme, this, "Check for Updates", false, MessageDialogImage.Error);
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

            MenuItem mi = new MenuItem();
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
            string file = (sender as MenuItem).Tag as string;

            if (File.Exists(file))
            {
                await LoadFile((sender as MenuItem).Tag as string, false);
            }
            else
            {
                MessageDialog md = new MessageDialog(App.ColorScheme);
                md.OkButtonText = "Cancel";
                md.ShowDialog("This file does not exist any more. Do you want to remove this file from the list or attempt to open anyway?", App.ColorScheme, this,
                    "File Not Found", false, MessageDialogImage.Error, MessageDialogResult.Cancel, "Remove file from list", "Attempt to open anyway");
                switch (md.DialogResult)
                {
                    case MessageDialogResult.OK:
                        // do nothing
                        break;
                    case MessageDialogResult.Cancel:
                        // not reached?
                        break;
                    case MessageDialogResult.Extra1:
                        // remove file from list

                        List<FrameworkElement> itemsToRemove = new List<FrameworkElement>();

                        foreach (FrameworkElement item in mnuRecent.Items)
                        {
                            if (item == (sender as MenuItem))
                            {
                                itemsToRemove.Add(item);
                            }
                        }

                        foreach (var item in itemsToRemove)
                        {
                            mnuRecent.Items.Remove(item);
                        }

                        App.Settings.RecentFiles.Remove(file);
                        SaveSettings();

                        if (App.Settings.RecentFiles.Count == 0)
                        {
                            mnuRecentEmpty.Visibility = Visibility.Visible;
                        }

                        break;
                    case MessageDialogResult.Extra2:
                        // attempt to open anyway
                        await LoadFile((sender as MenuItem).Tag as string, false);
                        break;
                    case MessageDialogResult.Extra3:
                        // not reached
                        break;
                    default:
                        break;
                }
            }
        }

        private void mnuRecentClear_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.RecentFiles.Clear();
            SaveSettings();

            List<FrameworkElement> itemsToRemove = new List<FrameworkElement>();

            foreach (FrameworkElement item in mnuRecent.Items)
            {

                if (item is MenuItem)
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

        #region Undo/Redo

        // relevant variables are declared at the top of the class

        private void UndoSetTimer_Tick(object sender, EventArgs e)
        {
            CreateUndoItem();
        }

        void CreateUndoItem()
        {

        }

        void PerformUndo()
        {


            if (redoItems.Count > 0)
            {
                redoItems.Clear();
            }
        }
        
        void PerformRedo()
        {

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

            //foreach (SpellEditor item in selSpells.GetItemsAsType<SpellEditor>())
            //{
            //    item.ApplyColorScheme(App.ColorScheme);
            //}
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
                SetAllTabsVisibility(Visibility.Collapsed);

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

            // weed out unintended "view" strings
            if (view != CONTINUOUS_VIEW && view != TABS_VIEW && view != RAWJSON_VIEW)
            {
                //await ChangeView(TABS_VIEW, updateSheet, displayEmptyMessage, saveSettings);
                return;
            }

            if (!_sheetLoaded) // don't update sheet if there is no sheet lol
            {
                updateSheet = false;
            }

            // update back-end settings before actually changing views
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
            if (_sheetLoaded)
            {
                txtEditRaw.Paste();
            }
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

        private void mnuFind_Click(object sender, RoutedEventArgs e)
        {
            if (_sheetLoaded)
            {
                sp.Open();
                if (!(txtEditRaw.TextArea.Selection.IsEmpty || txtEditRaw.TextArea.Selection.IsMultiline))
                    sp.SearchPattern = txtEditRaw.TextArea.Selection.GetText();
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Input, (Action)delegate { sp.Reactivate(); });
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
                MessageBox.Show(this, "The file \"" + filename + "\" does not appear to be a JSON file. Check the file in Notepad or another text editor to make sure it's not corrupted.",
                    "File Format Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            catch (InvalidOperationException e)
            {
                if (e.Message.Contains("error context error is different to requested error"))
                {
                    MessageBox.Show(this, "The file \"" + filename + "\" does not match the JSON format this program is looking for. Check the file in Notepad or another text editor to make sure it's not corrupted.",
                        "File Format Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else {
                    MessageBox.Show(this, "The file \"" + filename + "\" cannot be opened due to this error: \n\n" + e.Message + "\n\n" + 
                        "Check the file in Notepad or another text editor, or report this issue via the \"Send Feedback\" option in the Help menu.");
                }
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show(this, "The file \"" + filename + "\" cannot be found. Make sure the file exists and then try again.",
                    "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (addToRecent) AddRecentFile(filename);
        }

        async Task LoadPathfinderSheetAsync(PathfinderSheet sheet)
        {
            // set this flag so that the program doesn't try to set the sheet as dirty while loading in the file
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
                if (sheet.Player.Photos != null)
                {
                    ImageSource iss = new BitmapImage(new Uri(sheet.Player.Photos[0].Value));
                    imgPlayer.Source = iss;
                }
            }
            catch (IndexOutOfRangeException) { }
            catch (NullReferenceException) { }
            catch (System.Net.WebException) { }

            ud = sheet.Player;
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

            selSpellLikes.Clear();
            foreach (Spell item in sheet.SpellLikeAbilities)
            {
                SpellEditor se = new SpellEditor();
                se.ContentChanged += editor_ContentChanged;
                se.ApplyColorScheme(App.ColorScheme);
                se.LoadSpell(item);
                selSpellLikes.AddItem(se);
            }

            // Equipment tab

            Dictionary<string, string> money = sheet.Money;
            if (sheet.Money == null) // in sheets where the player hasn't given their character money, Mottokrosh's site doesn't add a "money" object to the JSON output
            {
                money = new Dictionary<string, string>();
            }
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

            // Spells tab
            int currentLevel = 0;
            List<Spell> allSpells = new List<Spell>();

            foreach (SpellLevel item in sheet.Spells)
            {
                switch (currentLevel)
                {
                    case 0:
                        txtSpellsBonus0.Text = item.BonusSpells;
                        txtSpellsDC0.Text = item.SaveDC;
                        txtSpellsKnown0.Text = item.TotalKnown;
                        txtSpellsPerDay0.Text = item.TotalPerDay;
                        break;
                    case 1:
                        txtSpellsBonus1.Text = item.BonusSpells;
                        txtSpellsDC1.Text = item.SaveDC;
                        txtSpellsKnown1.Text = item.TotalKnown;
                        txtSpellsPerDay1.Text = item.TotalPerDay;
                        break;
                    case 2:
                        txtSpellsBonus2.Text = item.BonusSpells;
                        txtSpellsDC2.Text = item.SaveDC;
                        txtSpellsKnown2.Text = item.TotalKnown;
                        txtSpellsPerDay2.Text = item.TotalPerDay;
                        break;
                    case 3:
                        txtSpellsBonus3.Text = item.BonusSpells;
                        txtSpellsDC3.Text = item.SaveDC;
                        txtSpellsKnown3.Text = item.TotalKnown;
                        txtSpellsPerDay3.Text = item.TotalPerDay;
                        break;
                    case 4:
                        txtSpellsBonus4.Text = item.BonusSpells;
                        txtSpellsDC4.Text = item.SaveDC;
                        txtSpellsKnown4.Text = item.TotalKnown;
                        txtSpellsPerDay4.Text = item.TotalPerDay;
                        break;
                    case 5:
                        txtSpellsBonus5.Text = item.BonusSpells;
                        txtSpellsDC5.Text = item.SaveDC;
                        txtSpellsKnown5.Text = item.TotalKnown;
                        txtSpellsPerDay5.Text = item.TotalPerDay;
                        break;
                    case 6:
                        txtSpellsBonus6.Text = item.BonusSpells;
                        txtSpellsDC6.Text = item.SaveDC;
                        txtSpellsKnown6.Text = item.TotalKnown;
                        txtSpellsPerDay6.Text = item.TotalPerDay;
                        break;
                    case 7:
                        txtSpellsBonus7.Text = item.BonusSpells;
                        txtSpellsDC7.Text = item.SaveDC;
                        txtSpellsKnown7.Text = item.TotalKnown;
                        txtSpellsPerDay7.Text = item.TotalPerDay;
                        break;
                    case 8:
                        txtSpellsBonus8.Text = item.BonusSpells;
                        txtSpellsDC8.Text = item.SaveDC;
                        txtSpellsKnown8.Text = item.TotalKnown;
                        txtSpellsPerDay8.Text = item.TotalPerDay;
                        break;
                    case 9:
                        txtSpellsBonus9.Text = item.BonusSpells;
                        txtSpellsDC9.Text = item.SaveDC;
                        txtSpellsKnown9.Text = item.TotalKnown;
                        txtSpellsPerDay9.Text = item.TotalPerDay;
                        break;
                    default:
                        break;
                }

                foreach (Spell sp in item.Spells)
                {
                    allSpells.Add(sp);
                }

                currentLevel++;
            } // end foreach

            txtSpellSpecialty.Text = sheet.SpellsSpeciality;
            txtSpellConditionalModifiers.Text = sheet.SpellsConditionalModifiers;

            selSpells.Clear();
            foreach (Spell spell in allSpells)
            {
                SpellEditor se = new SpellEditor();
                se.ContentChanged += editor_ContentChanged;
                se.ApplyColorScheme(App.ColorScheme);
                se.LoadSpell(spell);
                selSpells.AddItem(se);
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
            if (!_sheetLoaded)
            {
                MessageDialog md = new MessageDialog(App.ColorScheme);
                md.ShowDialog("Cannot run calculations when no sheet is opened.", App.ColorScheme, this, "Update Calculations", false, MessageDialogImage.Error);
                return;
            }

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
            bool updateAcItems = mnuUpdateAc.IsChecked;

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

            if (updateAcItems)
            {
                // special calculations for AC items
                int acShield = 0;
                int acArmor = 0;

                int tWeight = 0;
                int tBonus = 0;
                int tSpellcheck = 0;
                int tPenalty = 0;

                foreach (AcItemEditor acItem in selAcItem.GetItemsAsType<AcItemEditor>())
                {
                    AcItem ai = acItem.GetAcItem();
                    if (ai.Name.ToLowerInvariant().Contains("shield") || ai.Type.ToLowerInvariant().Contains("shield"))
                    {
                        // this is a shield
                        try { acShield += int.Parse(ai.Bonus); } catch (FormatException) { }
                    }
                    else
                    {
                        // probably not a shield? consider it armor
                        try { acArmor += int.Parse(ai.Bonus); } catch (FormatException) { }
                    }

                    try { tBonus += int.Parse(ai.Bonus); } catch (FormatException) { }
                    try { tSpellcheck += int.Parse(ai.SpellFailure.Replace("%", "")); } catch (FormatException) { }
                    try { tPenalty += int.Parse(ai.ArmorCheckPenalty); } catch (FormatException) { }
                    try { tWeight += int.Parse(ai.Weight); } catch (FormatException) { }
                }

                txtAcBonus.Text = tBonus.ToString();
                txtAcPenalty.Text = tPenalty.ToString();
                txtAcSpellFailure.Text = tSpellcheck.ToString() + "%";
                txtAcWeight.Text = tWeight.ToString();

                edtAc.UpdateAcItemBonuses(acShield.ToString(), acArmor.ToString());
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

            sheet.Player = ud;
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

            Dictionary<string, string> abilities = new Dictionary<string, string>
            {
                { "str", txtStr.Text },
                { "dex", txtDex.Text },
                { "cha", txtCha.Text },
                { "con", txtCon.Text },
                { "int", txtInt.Text },
                { "wis", txtWis.Text }
            };
            sheet.RawAbilities = abilities;

            Dictionary<string, CompoundModifier> saves = new Dictionary<string, CompoundModifier>
            {
                { "fort", edtFort.GetModifier() },
                { "reflex", edtReflex.GetModifier() },
                { "will", edtWill.GetModifier() }
            };
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

            // feats/abilites
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

            sheet.SpellLikeAbilities = new List<Spell>();
            foreach (SpellEditor item in selSpellLikes.GetItemsAsType<SpellEditor>())
            {
                sheet.SpellLikeAbilities.Add(item.GetSpell());
            }

            // equipment
            sheet.Money = new Dictionary<string, string>
            {
                { "cp", txtMoneyCp.Text },
                { "sp", txtMoneySp.Text },
                { "gp", txtMoneyGp.Text },
                { "pp", txtMoneyPp.Text },
                { "gems", txtGemsArt.Text },
                { "other", txtOtherTreasure.Text }
            };

            sheet.Equipment = new List<Equipment>();
            foreach (ItemEditor item in selEquipment.GetItemsAsType<ItemEditor>())
            {
                sheet.Equipment.Add(item.GetEquipment());
            }

            // skills
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

            // spells

            List<Spell> allspells = new List<Spell>();
            foreach (SpellEditor item in selSpells.GetItemsAsType<SpellEditor>())
            {
                allspells.Add(item.GetSpell());
            }

            sheet.Spells = new List<SpellLevel>(10);
            for (int i = 0; i < 10; i++)
            {
                SpellLevel sl = new SpellLevel();

                sl.TotalKnown = ((TextBox)grdSpells.FindName("txtSpellsKnown" + i)).Text;
                sl.SaveDC = ((TextBox)grdSpells.FindName("txtSpellsDC" + i)).Text;
                sl.TotalPerDay = ((TextBox)grdSpells.FindName("txtSpellsPerDay" + i)).Text;
                sl.BonusSpells = ((TextBox)grdSpells.FindName("txtSpellsBonus" + i)).Text;

                sl.Spells = new List<Spell>();

                foreach (Spell item in allspells)
                {
                    if (item.Level == i) sl.Spells.Add(item);
                }

                if (sl.Spells.Count == 0) sl.Spells = null;

                sheet.Spells.Add(sl);
            }
            sheet.SpellsConditionalModifiers = txtSpellConditionalModifiers.Text;
            sheet.SpellsSpeciality = txtSpellSpecialty.Text;

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

            lastEditedBox = (sender as TextBox);
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

        private void btnEditPlayerData_Click(object sender, EventArgs e)
        {
            UserdataEditor ude = new UserdataEditor();
            ude.LoadUserData(ud);
            ude.Owner = this;

            ude.ShowDialog();
            if (ude.DialogResult)
            {
                // update userdata
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

        private void expAbilities_Expanded(object sender, RoutedEventArgs e)
        {
            if (selAbilities != null) selAbilities.Visibility = Visibility.Visible;
        }

        private void expAbilities_Collapsed(object sender, RoutedEventArgs e)
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

        private void expSpellLikes_Expanded(object sender, RoutedEventArgs e)
        {
            if (selSpellLikes != null) selSpellLikes.Visibility = Visibility.Visible;
        }

        private void expSpellLikes_Collapsed(object sender, RoutedEventArgs e)
        {
            if (selSpellLikes != null) selSpellLikes.Visibility = Visibility.Collapsed;
        }

        private void btnAddSpellLike_Click(object sender, EventArgs e)
        {
            SpellEditor se = new SpellEditor();
            se.ContentChanged += editor_ContentChanged;
            se.ApplyColorScheme(App.ColorScheme);
            selSpellLikes.AddItem(se);

            expSpellLikes.IsExpanded = true;
            se.BringIntoView();
            se.IsSelected = true;

            SetIsDirty();
        }

        private void btnDeleteSpellLike_Click(object sender, EventArgs e)
        {
            selSpellLikes.RemoveSelectedItems();
            SetIsDirty();
        }

        private void btnDeselectSpellLike_Click(object sender, EventArgs e)
        {
            selSpellLikes.DeselectAll();
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

        #region Spell list editors

        private void btnAddSpell_Click(object sender, EventArgs e)
        {
            SpellEditor se = new SpellEditor();
            se.ContentChanged += editor_ContentChanged;
            se.ApplyColorScheme(App.ColorScheme);
            selSpells.AddItem(se);

            expSpells.IsExpanded = true;
            se.BringIntoView();
            se.IsSelected = true;

            SetIsDirty();
        }

        private void btnDeleteSpell_Click(object sender, EventArgs e)
        {
            selSpells.RemoveSelectedItems();
            SetIsDirty();
        }

        private void btnDeselectSpell_Click(object sender, EventArgs e)
        {
            selSpells.DeselectAll();
        }

        private void expSpells_Expanded(object sender, RoutedEventArgs e)
        {
            if (selSpells != null) selSpells.Visibility = Visibility.Visible;
        }

        private void expSpells_Collapsed(object sender, RoutedEventArgs e)
        {
            if (selSpells != null) selSpells.Visibility = Visibility.Collapsed;
        }

        private void mnuSpellFilter_Click(object sender, RoutedEventArgs e)
        {
            List<int> AllowedLevels = new List<int>();

            if (mnuSpellFilter0.IsChecked) AllowedLevels.Add(0);
            if (mnuSpellFilter1.IsChecked) AllowedLevels.Add(1);
            if (mnuSpellFilter2.IsChecked) AllowedLevels.Add(2);
            if (mnuSpellFilter3.IsChecked) AllowedLevels.Add(3);
            if (mnuSpellFilter4.IsChecked) AllowedLevels.Add(4);
            if (mnuSpellFilter5.IsChecked) AllowedLevels.Add(5);
            if (mnuSpellFilter6.IsChecked) AllowedLevels.Add(6);
            if (mnuSpellFilter7.IsChecked) AllowedLevels.Add(7);
            if (mnuSpellFilter8.IsChecked) AllowedLevels.Add(8);
            if (mnuSpellFilter9.IsChecked) AllowedLevels.Add(9);

            foreach (SpellEditor item in selSpells.GetItemsAsType<SpellEditor>())
            {
                if (AllowedLevels.Contains(item.Level))
                {
                    item.Visibility = Visibility.Visible;
                }
                else
                {
                    item.Visibility = Visibility.Collapsed;
                    item.IsSelected = false; // don't have hidden items be selected
                }
            }
        }
        #endregion
    }
}
