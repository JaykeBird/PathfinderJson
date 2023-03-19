using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using SolidShineUi;
using SolidShineUi.KeyboardShortcuts;

using static PathfinderJson.CoreUtils;
using static PathfinderJson.App;
using System.Windows.Shell;
using System.ComponentModel;
using System.Linq;

//using Markdig;
//using Markdig.Wpf;
//using Markdig.Renderers.Wpf;

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
        /// <summary>The title displayed in the title bar</summary>
        string displayedTitle = "";
        /// <summary>Get or set if a file has unsaved changes</summary>
        bool isDirty = false;
        /// <summary>Get or set if the app should automatically check for updates when you open it</summary>
        bool autoCheckUpdates = true;
        /// <summary>Get or set the date the app last auto checked for updates; only auto check once per day</summary>
        string lastAutoCheckDate = "2020-03-26";

        /// <summary>Get or set the current view ("tabs", "continuous", or "rawjson")</summary>
        string currentView = TABS_VIEW;

        /// <summary>Get or set if the tabbed/continuous view has changes not synced with text editor</summary>
        bool _isTabsDirty = false;
        /// <summary>Get or set if the text editor has changes not synced with the tabbed/continuous view</summary>
        bool _isEditorDirty = false;
        /// <summary>Get or set if a file is currently being opened (and sheet loaded in)</summary>
        bool _isUpdating = false;
        /// <summary>Generic cancellation token, use for lengthy cancellable processes</summary>
        CancellationTokenSource cts = new CancellationTokenSource();
        /// <summary>The search panel associated with the raw JSON editor</summary>
        SearchPanel.SearchPanel sp;
        /// <summary>Get or set if the sheet view is currently running calculations</summary>
        bool _isCalculating = false;
        /// <summary>The timer for the auto save feature. When it ticks, save the file.</summary>
        DispatcherTimer autoSaveTimer = new DispatcherTimer();
        /// <summary>The timer for displaying the "Saved" text in the top.</summary>
        DispatcherTimer saveDisplayTimer = new DispatcherTimer();

        /// <summary>set if the Notes tab is in Edit mode (true) or View mode (false) (if Markdown support is disabled, it is always in Edit mode)</summary>
        bool notesEdit = false;

        // functions for handling undo/redo
        // these aren't actually used for anything at the current time as I've not properly introduced undo/redo yet
        UndoStack<PathfinderSheet> undoStack = new UndoStack<PathfinderSheet>();
        UIElement? lastEditedItem = null;
        DispatcherTimer undoSetTimer = new DispatcherTimer();

        // these are stored here as the program doesn't display these values to the user directly
        UserData ud;
        string sheetid;
        Dictionary<string, string> abilities = new Dictionary<string, string>();
        Dictionary<string, int> abilityMods = new Dictionary<string, int>();
        Dictionary<string, string?> sheetSettings = new Dictionary<string, string?>();
        Dictionary<string, string> skillModSubs = new Dictionary<string, string>();
        string? version;
        int babCalc = 0;

        // keyboard/method data
        KeyActionList mr = new KeyActionList();
        KeyboardShortcutHandler ksh;

        #region Constructor/ window events/ basic functions

        public MainWindow()
        {
            ud = new UserData(false);
            sheetid = "-1";
            bool isHighContrast = false;

            // if true, run SaveSettings at the end of this function, to avoid calling SaveSettings like 5 times at once
            bool updateSettings = false;

            // set timer delay to 2 seconds
            // perhaps in the future I'll add a setting to allow users to set the undo timer delay
            undoSetTimer.Interval = new TimeSpan(0, 0, 2);
            undoSetTimer.Tick += UndoSetTimer_Tick;

            autoCheckUpdates = App.Settings.UpdateAutoCheck;
            lastAutoCheckDate = App.Settings.UpdateLastCheckDate;
            DateTime today = DateTime.Today;

            if (autoCheckUpdates && lastAutoCheckDate != today.Year + "-" + today.Month + "-" + today.Day)
            {
                lastAutoCheckDate = today.Year + "-" + today.Month + "-" + today.Day;
                App.Settings.UpdateLastCheckDate = lastAutoCheckDate;
                updateSettings = true;
            }
            else
            {
                autoCheckUpdates = false;
            }

            InitializeComponent();
            ksh = new KeyboardShortcutHandler(this);
            mr = RoutedEventKeyAction.CreateListFromMenu(menu);

            if (File.Exists(Path.Combine(appDataPath, "keyboard.xml")))
            {
                try
                {
                    ksh.LoadShortcutsFromFile(Path.Combine(appDataPath, "keyboard.xml"), mr);
                }
                catch (ArgumentException)
                {
                    ksh.LoadShortcutsFromList(DefaultKeySettings.CreateDefaultShortcuts(mr));
                }
            }
            else
            {
                ksh.LoadShortcutsFromList(DefaultKeySettings.CreateDefaultShortcuts(mr));
            }

            if (App.Settings.HighContrastTheme == NO_HIGH_CONTRAST)
            {
                App.ColorScheme = new ColorScheme(ColorsHelper.CreateFromHex(App.Settings.ThemeColor));
            }
            else
            {
                switch (App.Settings.HighContrastTheme)
                {
                    case "1":
                        App.ColorScheme = ColorScheme.GetHighContrastScheme(HighContrastOption.WhiteOnBlack);
                        isHighContrast = true;
                        break;
                    case "2":
                        App.ColorScheme = ColorScheme.GetHighContrastScheme(HighContrastOption.GreenOnBlack);
                        isHighContrast = true;
                        break;
                    case "3":
                        App.ColorScheme = ColorScheme.GetHighContrastScheme(HighContrastOption.BlackOnWhite);
                        isHighContrast = true;
                        break;
                    case "4":
                        App.ColorScheme = ColorScheme.CreateLightTheme();
                        break;
                    case "5":
                        App.ColorScheme = ColorScheme.CreateDarkTheme();
                        break;
                    default:
                        App.Settings.HighContrastTheme = NO_HIGH_CONTRAST;
                        App.ColorScheme = new ColorScheme(ColorsHelper.CreateFromHex(App.Settings.ThemeColor));
                        updateSettings = true;
                        break;
                }
            }

            SetupTabs();
            selTabs.IsEnabled = false;
            foreach (SelectableItem item in selTabs.Items.SelectedItems.OfType<SelectableItem>())
            {
                item.IsEnabled = false;
            }

            UpdateAppearance();

            ChangeView(App.Settings.StartView, false, true, false);

            if (App.Settings.RecentFiles.Count > 20)
            {
                // time to remove some old entries
                App.Settings.RecentFiles.Reverse();
                App.Settings.RecentFiles.RemoveRange(20, App.Settings.RecentFiles.Count - 20);
                App.Settings.RecentFiles.Reverse();
                updateSettings = true;
            }

            mnuIndent.IsChecked = App.Settings.IndentJsonData;
            mnuAutoCheck.IsChecked = App.Settings.UpdateAutoCheck;

            mnuFilename.IsChecked = App.Settings.PathInTitleBar;

            foreach (string file in App.Settings.RecentFiles)//.Reverse<string>())
            {
                AddRecentFile(file, false);
            }
            mnuRecentActions.IsChecked = App.Settings.DisplayRecentActionsAsSubmenu;

            ShowHideToolbar(App.Settings.ShowToolbar);

            int autoSave = App.Settings.AutoSave;
            if (autoSave <= 0)
            {
                autoSaveTimer.IsEnabled = false;
            }
            else if (autoSave > 30)
            {
                autoSaveTimer.IsEnabled = false;
                autoSaveTimer.Interval = new TimeSpan(0, 30, 0);
                autoSaveTimer.IsEnabled = true;
            }
            else
            {
                autoSaveTimer.IsEnabled = false;
                autoSaveTimer.Interval = new TimeSpan(0, autoSave, 0);
                autoSaveTimer.IsEnabled = true;
            }

            saveDisplayTimer.Interval = new TimeSpan(0, 0, 15);

            autoSaveTimer.Tick += autoSaveTimer_Tick;
            saveDisplayTimer.Tick += saveDisplayTimer_Tick;

            // setup up raw JSON editor
            if (App.Settings.EditorSyntaxHighlighting && !isHighContrast)
            {
                using (Stream? s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("PathfinderJson.Json.xshd"))
                {
                    if (s != null)
                    {
                        using XmlReader reader = new XmlTextReader(s);
                        txtEditRaw.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    }
                }
            }
            else
            {
                using (Stream? s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("PathfinderJson.None.xshd"))
                {
                    if (s != null)
                    {
                        using XmlReader reader = new XmlTextReader(s);
                        txtEditRaw.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    }
                }
            }

            LoadEditorFontSettings();

            txtEditRaw.ShowLineNumbers = App.Settings.EditorLineNumbers;

            txtEditRaw.WordWrap = App.Settings.EditorWordWrap;
            mnuWordWrap.IsChecked = App.Settings.EditorWordWrap;

            edtAc.ShowShieldGlyph = App.Settings.ShowGlyphs && !isHighContrast;
            eleStr.ShowBannerGlyph = App.Settings.ShowGlyphs && !isHighContrast;
            eleDex.ShowBannerGlyph = App.Settings.ShowGlyphs && !isHighContrast;
            eleCon.ShowBannerGlyph = App.Settings.ShowGlyphs && !isHighContrast;
            eleInt.ShowBannerGlyph = App.Settings.ShowGlyphs && !isHighContrast;
            eleWis.ShowBannerGlyph = App.Settings.ShowGlyphs && !isHighContrast;
            eleCha.ShowBannerGlyph = App.Settings.ShowGlyphs && !isHighContrast;

            SearchPanel.SearchPanel p = SearchPanel.SearchPanel.Install(txtEditRaw);
            p.FontFamily = SystemFonts.MessageFontFamily; // so it isn't a fixed-width font lol
            sp = p;
            sp.ColorScheme = App.ColorScheme;

            txtEditRaw.Encoding = new System.Text.UTF8Encoding(false);

            if (updateSettings)
            {
                SaveSettings();
            }

#if DEBUG
            //mnuTestUndo.IsEnabled = true;
            //mnuTestUndo.Visibility = Visibility.Visible;
            mnuShowUndo.IsEnabled = true;
            mnuShowUndo.Visibility = Visibility.Visible;
#endif

            btnUndoS.IsEnabled = undoStack.CanUndo;
            mnuUndoS.IsEnabled = undoStack.CanUndo;
            btnRedoS.IsEnabled = undoStack.CanRedo;
            mnuRedoS.IsEnabled = undoStack.CanRedo;
        }

        #region Other Base Functions

        /// <summary>
        /// Open a file into the editor. This is intended for when opening files from the command-line arguments.
        /// </summary>
        /// <param name="filename">The path to the file to open.</param>
        public void OpenFile(string filename)
        {
            LoadFile(filename, true);
        }

        /// <summary>
        /// Save the currently used settings to the settings.json file.
        /// </summary>
        /// <param name="updateFonts">If true, will also save the current editor font settings. This takes additional processing which may not be always needed.</param>
        void SaveSettings(bool updateFonts = false)
        {
            if (updateFonts)
            {
                SaveEditorFontSettings();
            }

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

        void ReloadSettings()
        {
            bool updateSettings = false;

            bool isHighContrast = false;

            if (App.Settings.HighContrastTheme == NO_HIGH_CONTRAST)
            {
                App.ColorScheme = new ColorScheme(ColorsHelper.CreateFromHex(App.Settings.ThemeColor));
            }
            else
            {
                switch (App.Settings.HighContrastTheme)
                {
                    case "1":
                        App.ColorScheme = ColorScheme.GetHighContrastScheme(HighContrastOption.WhiteOnBlack);
                        isHighContrast = true;
                        break;
                    case "2":
                        App.ColorScheme = ColorScheme.GetHighContrastScheme(HighContrastOption.GreenOnBlack);
                        isHighContrast = true;
                        break;
                    case "3":
                        App.ColorScheme = ColorScheme.GetHighContrastScheme(HighContrastOption.BlackOnWhite);
                        isHighContrast = true;
                        break;
                    case "4":
                        App.ColorScheme = ColorScheme.CreateLightTheme();
                        break;
                    case "5":
                        App.ColorScheme = ColorScheme.CreateDarkTheme();
                        break;
                    default:
                        App.Settings.HighContrastTheme = NO_HIGH_CONTRAST;
                        App.ColorScheme = new ColorScheme(ColorsHelper.CreateFromHex(App.Settings.ThemeColor));
                        updateSettings = true;
                        break;
                }
            }

            UpdateAppearance();
            UpdateTitlebar();

            mnuIndent.IsChecked = App.Settings.IndentJsonData;
            mnuAutoCheck.IsChecked = App.Settings.UpdateAutoCheck;

            mnuFilename.IsChecked = App.Settings.PathInTitleBar;

            if (App.Settings.RecentFiles.Count > 20)
            {
                // time to remove some old entries
                App.Settings.RecentFiles.Reverse();
                App.Settings.RecentFiles.RemoveRange(20, App.Settings.RecentFiles.Count - 20);
                App.Settings.RecentFiles.Reverse();
                updateSettings = true;
            }

            // clear recent files list in UI (not in backend)
            RebuildRecentMenu();

            ShowHideToolbar(App.Settings.ShowToolbar);

            int autoSave = App.Settings.AutoSave;
            if (autoSave <= 0)
            {
                autoSaveTimer.IsEnabled = false;
            }
            else if (autoSave > 30)
            {
                autoSaveTimer.IsEnabled = false;
                autoSaveTimer.Interval = new TimeSpan(0, 30, 0);
                autoSaveTimer.IsEnabled = true;
            }
            else
            {
                autoSaveTimer.IsEnabled = false;
                autoSaveTimer.Interval = new TimeSpan(0, autoSave, 0);
                autoSaveTimer.IsEnabled = true;
            }

            // setup up raw JSON editor
            if (App.Settings.EditorSyntaxHighlighting && !isHighContrast)
            {
                using (Stream? s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("PathfinderJson.Json.xshd"))
                {
                    if (s != null)
                    {
                        using XmlReader reader = new XmlTextReader(s);
                        txtEditRaw.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    }
                }
            }
            else
            {
                using (Stream? s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("PathfinderJson.None.xshd"))
                {
                    if (s != null)
                    {
                        using XmlReader reader = new XmlTextReader(s);
                        txtEditRaw.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    }
                }
            }

            LoadEditorFontSettings();

            txtEditRaw.ShowLineNumbers = App.Settings.EditorLineNumbers;

            txtEditRaw.WordWrap = App.Settings.EditorWordWrap;
            mnuWordWrap.IsChecked = App.Settings.EditorWordWrap;

            edtAc.ShowShieldGlyph = App.Settings.ShowGlyphs && !isHighContrast;
            eleStr.ShowBannerGlyph = App.Settings.ShowGlyphs && !isHighContrast;
            eleDex.ShowBannerGlyph = App.Settings.ShowGlyphs && !isHighContrast;
            eleCon.ShowBannerGlyph = App.Settings.ShowGlyphs && !isHighContrast;
            eleInt.ShowBannerGlyph = App.Settings.ShowGlyphs && !isHighContrast;
            eleWis.ShowBannerGlyph = App.Settings.ShowGlyphs && !isHighContrast;
            eleCha.ShowBannerGlyph = App.Settings.ShowGlyphs && !isHighContrast;

            if (updateSettings)
            {
                SaveSettings();
            }
        }

        void UpdateTitlebar()
        {
            if (!_sheetLoaded)
            {
                Title = "Pathfinder JSON";
                displayedTitle = "";
            }
            else
            {
                if (App.Settings.PathInTitleBar)
                {
                    if (string.IsNullOrEmpty(filePath))
                    {
                        Title = "(new file) - Pathfinder JSON";
                        displayedTitle = "New File";
                    }
                    else
                    {
                        Title = Path.GetFileName(filePath) + " - Pathfinder JSON";
                        displayedTitle = Path.GetFileName(filePath);
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(fileTitle))
                    {
                        Title = "(unnamed character) - Pathfinder JSON";
                        displayedTitle = fileTitle;
                    }
                    else
                    {
                        Title = fileTitle + " - Pathfinder JSON";
                        displayedTitle = fileTitle;
                    }
                }
            }

            if (isDirty)
            {
                Title += " *";
            }
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

            if (update || fileTitle != displayedTitle) UpdateTitlebar();
        }

        private void autoSaveTimer_Tick(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    // do not auto save if there is no file path already set up
                    //SaveAsFile();
                }
                else
                {
                    SaveFile(filePath);
                }
            });
        }

        private void saveDisplayTimer_Tick(object? sender, EventArgs e)
        {
            brdrSaved.Visibility = Visibility.Collapsed;
        }

        private void brdrSaved_MouseDown(object sender, MouseButtonEventArgs e)
        {
            brdrSaved.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Other Window event handlers

        private async void window_Loaded(object sender, RoutedEventArgs e)
        {
            if (autoCheckUpdates)
            {
                await CheckForUpdates(false);
            }
        }

        private void window_Activated(object sender, EventArgs e)
        {
            menu.Foreground = App.ColorScheme.ForegroundColor.ToBrush();
        }

        private void window_Deactivated(object sender, EventArgs e)
        {
            if (App.Settings.HighContrastTheme == NO_HIGH_CONTRAST)
            {
                menu.Foreground = ColorsHelper.CreateFromHex("#404040").ToBrush();
            }
        }

        private void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!SaveDirtyChanges() || CheckCalculating())
            {
                e.Cancel = true;
            }
        }

        private void window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        #endregion

        #endregion

        #region File Menu

        private async void mnuNew_Click(object sender, RoutedEventArgs e)
        {
            if (!SaveDirtyChanges() || CheckCalculating())
            {
                return;
            }

            NewSheet ns = new NewSheet();
            ns.Owner = this;
            ns.ShowDialog();

            if (ns.DialogResult)
            {
                PathfinderSheet ps = ns.Sheet;

                filePath = ns.FileLocation;
                fileTitle = ps.Name;
                _sheetLoaded = true;

                isDirty = false;
                _isEditorDirty = false;
                _isTabsDirty = false;

                UpdateTitlebar();

                txtEditRaw.Text = ps.SaveJsonText(App.Settings.IndentJsonData);
                ChangeView(App.Settings.StartView, false, false);
                LoadPathfinderSheet(ps);
                await UpdateCalculations();
            }
        }

        private void mnuNewWindow_Click(object sender, RoutedEventArgs e)
        {
            string? filename = Process.GetCurrentProcess().MainModule?.FileName;
            if (filename == null)
            {
                MessageBox.Show("Cannot open another instance automatically.");
                return;
            }

            Process.Start(filename);
        }

        private void mnuOpen_Click(object sender, RoutedEventArgs e)
        {
            if (!SaveDirtyChanges() || CheckCalculating())
            {
                return;
            }

            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.Title = "Open JSON";
            ofd.Filter = "JSON Sheet|*.json|All Files|*.*";

            if (ofd.ShowDialog() ?? false == true)
            {
                LoadFile(ofd.FileName);
            }
        }

        private void mnuSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                SaveAsFile();
            }
            else
            {
                SaveFile(filePath);
            }
        }

        private void mnuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveAsFile();
        }

        void SaveFile(string file)
        {
            if (CheckCalculating()) return;

            if (currentView == RAWJSON_VIEW)
            {
                // check if text is valid JSON first
                bool validJson = false;

                if (!string.IsNullOrEmpty(txtEditRaw.Text))
                {
                    try
                    {
                        PathfinderSheet ps = PathfinderSheet.LoadJsonText(txtEditRaw.Text);
                        validJson = true;
                    }
                    catch (Newtonsoft.Json.JsonReaderException) { }
                    catch (FormatException) { }
                }

                if (!validJson)
                {
                    MessageDialog md = new MessageDialog(App.ColorScheme);
                    md.ShowDialog("The file's text doesn't seem to be valid JSON. Saving the file as it is may result in lost data or the file not being openable with this program in the future. Do you want to continue?",
                        null, this, "Invalid JSON Detected", MessageDialogButtonDisplay.Auto, image: MessageDialogImage.Warning, customOkButtonText: "Save anyway", customCancelButtonText: "Cancel");

                    if (md.DialogResult == MessageDialogResult.Cancel)
                    {
                        return;
                    }
                }
                txtEditRaw.Save(file);
                SyncSheetFromEditor();
            }
            else
            {
                SyncEditorFromSheet();
                txtEditRaw.Save(file);
                //PathfinderSheet ps = await CreatePathfinderSheetAsync();
                //ps.SaveJsonFile(file, App.Settings.IndentJsonData);
            }

            _isEditorDirty = false;
            _isTabsDirty = false;
            isDirty = false;
            UpdateTitlebar();

            brdrSaved.Visibility = Visibility.Visible;
            saveDisplayTimer.Start();
        }

        bool SaveAsFile()
        {
            if (!_sheetLoaded)
            {
                MessageDialog md = new MessageDialog(App.ColorScheme);
                md.ShowDialog("No sheet is currently open, so saving is not possible.", null, this, "No Sheet Open", MessageDialogButtonDisplay.Auto, image: MessageDialogImage.Error);
                return false;
            }

            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
            sfd.Title = "Save JSON Sheet As";
            sfd.Filter = "JSON Sheet|*.json";

            if (sfd.ShowDialog() ?? false == true)
            {
                SaveFile(sfd.FileName);
                filePath = sfd.FileName;
                AddRecentFile(filePath, true);
                UpdateTitlebar();
                return true;
            }
            else
            {
                return false;
            }
        }

        void CloseFile()
        {
            if (!SaveDirtyChanges() || CheckCalculating())
            {
                return;
            }

            filePath = "";
            fileTitle = "";
            _sheetLoaded = false;
            SetIsDirty(false);
            txtEditRaw.Text = "";
            undoStack.Clear();

            brdrSaved.Visibility = Visibility.Collapsed;
            saveDisplayTimer.Stop();

            ChangeView(App.Settings.StartView, false, true, false);
        }

        bool CheckCalculating()
        {
            if (_isCalculating)
            {
                MessageDialog md = new MessageDialog();
                md.ShowDialog("Cannot perform this function while the sheet is calculating. Please try again in just a moment.", App.ColorScheme, this, "Currently Calculating", MessageDialogButtonDisplay.Auto, image: MessageDialogImage.Error);
                return true;
            }
            else
            {
                return false;
            }
        }

        bool SaveDirtyChanges()
        {
            // if there's no sheet loaded, then there should be no dirty changes to save
            if (!_sheetLoaded)
            {
                return true;
            }

            if (isDirty)
            {
                MessageDialog md = new MessageDialog(App.ColorScheme);
                md.ShowDialog("The file has some unsaved changes. Do you want to save them first?", null, this, "Unsaved Changes", MessageDialogButtonDisplay.Three, MessageDialogImage.Question,
                    MessageDialogResult.Cancel, "Save", "Cancel", "Discard");

                if (md.DialogResult == MessageDialogResult.OK)
                {
                    if (string.IsNullOrEmpty(filePath))
                    {
                        return SaveAsFile();
                    }
                    else
                    {
                        SaveFile(filePath);
                        return true;
                    }
                }
                else if (md.DialogResult == MessageDialogResult.Discard)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        bool AskDiscard()
        {
            if (isDirty)
            {
                MessageDialog md = new MessageDialog(App.ColorScheme);
                md.ShowDialog("The file has some unsaved changes. Are you sure you want to discard them?", App.ColorScheme, this, "Unsaved Changes", MessageDialogButtonDisplay.Auto,
                    image: MessageDialogImage.Question, customOkButtonText: "Discard", customCancelButtonText: "Cancel");

                if (md.DialogResult == MessageDialogResult.OK || md.DialogResult == MessageDialogResult.Discard)
                {
                    // Discard and continue
                    return true;
                }
                else
                {
                    // Cancel the operation
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        async Task CheckForUpdates(bool dialogIfNone = true)
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
                    if (dialogIfNone)
                    {
                        MessageDialog md = new MessageDialog(App.ColorScheme);
                        md.ShowDialog("There are no updates available. You're on the latest release!", App.ColorScheme, this, "Check for Updates", MessageDialogButtonDisplay.Auto, image: MessageDialogImage.Hand);
                    }
                }
            }
            catch (System.Net.WebException)
            {
                if (dialogIfNone)
                {
                    MessageDialog md = new MessageDialog(App.ColorScheme);
                    md.ShowDialog("Could not check for updates. Make sure you're connected to the Internet.", App.ColorScheme, this, "Check for Updates", MessageDialogButtonDisplay.Auto, image: MessageDialogImage.Error);
                }
            }
        }

        private void mnuRevert_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                if (AskDiscard())
                {
                    LoadFile(filePath, false);
                }
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

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            CloseFile();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Recent files

        void AddRecentFile(string filename, bool storeInSettings = true)
        {
            bool submenu = App.Settings.DisplayRecentActionsAsSubmenu;

            if (storeInSettings && App.Settings.RecentFiles.Contains(filename))
            {
                JumpList.AddToRecentCategory(filename);
                return;
            }

            MenuItem mi = new MenuItem();
            string name = Path.GetFileName(filename);
            mi.Header = "_" + name;
            ToolTip tt = new ToolTip
            {
                Content = filename,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Right,
                PlacementTarget = mi
            };
            mi.ToolTip = tt;
            mi.Tag = filename;
            mi.Click += MainRecentFileClick;
            mi.ContextMenuOpening += miRecentContext_Opening;
            mnuRecent.Items.Insert(0, mi);

            SolidShineUi.ContextMenu cm = new SolidShineUi.ContextMenu();
            if (!submenu)
            {
                cm.PlacementTarget = mi;
                cm.Width = 180;
            }
            else
            {
                tt.VerticalOffset = -25;
                tt.HorizontalOffset = 3;
            }

            CreateMenuItem("Open", miRecentOpen_Click);
            CreateMenuItem("Open in New Window", miRecentOpenNew_Click);
            CreateMenuItem("Copy Path", miRecentCopy_Click);
            CreateMenuItem("View in Explorer", miRecentView_Click);
            CreateMenuItem("Remove", miRecentRemove_Click);

            if (!submenu)
            {
                mi.ContextMenu = cm;
            }

            if (storeInSettings)
            {
                App.Settings.RecentFiles.Add(filename);
                JumpList.AddToRecentCategory(filename);
                SaveSettings();
            }

            mnuRecentEmpty.Visibility = Visibility.Collapsed;

            MenuItem CreateMenuItem(string header, RoutedEventHandler handler)
            {
                MenuItem mii = new MenuItem();
                mii.Header = header;
                mii.Tag = mi;
                mii.Click += handler;
                if (submenu) mi.Items.Add(mii); else cm.Items.Add(mii);
                return mi;
            }
        }

        private void miRecentContext_Opening(object sender, ContextMenuEventArgs e)
        {
            if (sender is MenuItem m)
            {
                if (m.ContextMenu is SolidShineUi.ContextMenu cm)
                {
                    cm.ApplyColorScheme(App.ColorScheme);
                }
            }
        }

        private void MainRecentFileClick(object sender, RoutedEventArgs e)
        {
            if (App.Settings.DisplayRecentActionsAsSubmenu) return;

            miRecentFile_Click(sender, e);
        }

        private void miRecentFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender == null)
            {
                return;
            }

            if (sender is MenuItem m)
            {
                if (m.Tag is string file)
                {
                    if (File.Exists(file))
                    {
                        if (!SaveDirtyChanges() || CheckCalculating())
                        {
                            return;
                        }

                        LoadFile(file, false);
                    }
                    else
                    {
                        MessageDialog md = new MessageDialog(App.ColorScheme);
                        md.OkButtonText = "Cancel";
                        md.ShowDialog("This file does not exist any more. Do you want to remove this file from the list or attempt to open anyway?", App.ColorScheme, this,
                            "File Not Found", MessageDialogButtonDisplay.Auto, MessageDialogImage.Error, MessageDialogResult.Cancel, "Remove file from list", "Attempt to open anyway");
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

                                foreach (FrameworkElement? item in mnuRecent.Items)
                                {
                                    if (item != null && item == (sender as MenuItem))
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
                                if (!SaveDirtyChanges() || CheckCalculating())
                                {
                                    return;
                                }

                                LoadFile(file, false);
                                break;
                            case MessageDialogResult.Extra3:
                                // not reached
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        private void mnuRecentClear_Click(object sender, RoutedEventArgs e)
        {
            if (AskClearRecentList())
            {
                App.Settings.RecentFiles.Clear();
                SaveSettings();

                List<FrameworkElement> itemsToRemove = new List<FrameworkElement>();

                foreach (FrameworkElement? item in mnuRecent.Items)
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
        }

        private void mnuRecentActions_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.DisplayRecentActionsAsSubmenu = !App.Settings.DisplayRecentActionsAsSubmenu;

            SaveSettings();
            RebuildRecentMenu();
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

        void RebuildRecentMenu()
        {
            List<FrameworkElement> itemsToRemove = new List<FrameworkElement>();

            foreach (FrameworkElement? item in mnuRecent.Items)
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

            foreach (string file in App.Settings.RecentFiles)//.Reverse<string>())
            {
                AddRecentFile(file, false);
            }

            mnuRecentActions.IsChecked = App.Settings.DisplayRecentActionsAsSubmenu;
        }

        private void miRecentOpen_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                if (mi.Tag is MenuItem parent)
                {
                    miRecentFile_Click(parent, e);
                }
            }
        }

        private void miRecentOpenNew_Click(object sender, RoutedEventArgs e)
        {
            // Process.GetCurrentProcess().MainModule?.FileName

            if (sender is MenuItem mi)
            {
                if (mi.Tag is MenuItem parent)
                {
                    if (parent.Tag is string file)
                    {
                        string? filename = Process.GetCurrentProcess().MainModule?.FileName;
                        if (filename == null)
                        {
                            // TODO: ask user if they want to open in this instance instead
                            MessageBox.Show("Cannot open another instance automatically.");
                            return;
                        }

                        Process.Start(filename, "\"" + file + "\"");
                    }
                }
            }
        }

        private void miRecentCopy_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                if (mi.Tag is MenuItem parent)
                {
                    if (parent.Tag is string file)
                    {
                        Clipboard.SetText(file);
                    }
                }
            }
        }

        private void miRecentRemove_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                if (mi.Tag is MenuItem parent)
                {
                    List<FrameworkElement> itemsToRemove = new List<FrameworkElement>();

                    foreach (FrameworkElement? item in mnuRecent.Items)
                    {
                        if (item != null && item == parent)
                        {
                            itemsToRemove.Add(item);
                        }
                    }

                    foreach (var item in itemsToRemove)
                    {
                        mnuRecent.Items.Remove(item);
                    }

                    if (parent.Tag is string file)
                    {
                        App.Settings.RecentFiles.Remove(file);
                        SaveSettings();

                        if (App.Settings.RecentFiles.Count == 0)
                        {
                            mnuRecentEmpty.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
        }

        private void miRecentView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                if (mi.Tag is MenuItem parent)
                {
                    if (parent.Tag is string file)
                    {
                        Process.Start("explorer.exe", "/select,\"" + file + "\"");
                    }
                }
            }
        }

        #endregion

        #region Undo/Redo

        // relevant variables are declared at the top of the class

        void StartUndoTimer(UIElement sender)
        {
            if (sender != lastEditedItem)
            {
                CreateUndoState();
                PostUndoStateUpdate("New element state / timer restart");

                undoSetTimer.Stop();
                undoSetTimer.Start();
            }
            else
            {
                //CreateUndoState();
                if (undoSetTimer.IsEnabled)
                {
                    PostUndoStateUpdate("Timer restart");

                    undoSetTimer.Stop();
                    undoSetTimer.Start();
                }
                else
                {
                    undoSetTimer.Start();
                    PostUndoStateUpdate("Timer start");
                }
            }
        }

        private void UndoSetTimer_Tick(object? sender, EventArgs e)
        {
            CreateUndoState();
            undoSetTimer.IsEnabled = false;
            PostUndoStateUpdate("Tick new state");
        }

        private void mnuTestUndo_Click(object sender, RoutedEventArgs e)
        {
            UndoStackTest ust = new UndoStackTest();
            ust.Show();
        }

        private void CreateUndoState()
        {
            PathfinderSheet ps = CreatePathfinderSheet();
            undoStack.StoreState(ps);
            PostUndoStateUpdate("New state");

            btnUndoS.IsEnabled = undoStack.CanUndo;
            mnuUndoS.IsEnabled = undoStack.CanUndo;
            btnRedoS.IsEnabled = undoStack.CanRedo;
            mnuRedoS.IsEnabled = undoStack.CanRedo;
        }

        private void mnuUndoS_Click(object sender, RoutedEventArgs e)
        {
            if (undoStack.CanUndo)
            {
                CoreLoadPathfinderSheet(undoStack.Undo());
                PostUndoStateUpdate("Undo done");
            }

            btnUndoS.IsEnabled = undoStack.CanUndo;
            mnuUndoS.IsEnabled = undoStack.CanUndo;
            btnRedoS.IsEnabled = undoStack.CanRedo;
            mnuRedoS.IsEnabled = undoStack.CanRedo;
        }

        private void mnuRedoS_Click(object sender, RoutedEventArgs e)
        {
            if (undoStack.CanRedo)
            {
                CoreLoadPathfinderSheet(undoStack.Redo());
                PostUndoStateUpdate("Redo done");
            }

            btnUndoS.IsEnabled = undoStack.CanUndo;
            mnuUndoS.IsEnabled = undoStack.CanUndo;
            btnRedoS.IsEnabled = undoStack.CanRedo;
            mnuRedoS.IsEnabled = undoStack.CanRedo;
        }

        void PostUndoStateUpdate(string text)
        {
            if (mnuShowUndo.IsChecked)
            {
                brdrUndoState.Visibility = Visibility.Visible;
                txtUndoState.Text = text;
            }
            else
            {
                brdrUndoState.Visibility = Visibility.Collapsed;
            }
        }

        private void mnuShowUndo_Click(object sender, RoutedEventArgs e)
        {
            mnuShowUndo.IsChecked = !mnuShowUndo.IsChecked;
        }

        #endregion

        #region Tab bar / visuals / appearance

        void UpdateAppearance()
        {
            ApplyColorScheme(App.ColorScheme);
            menu.ApplyColorScheme(App.ColorScheme);
            toolbar.Background = App.ColorScheme.MainColor.ToBrush();
            if (sp != null) sp.ColorScheme = App.ColorScheme;

            if (App.ColorScheme.IsHighContrast)
            {
                menu.Background = App.ColorScheme.BackgroundColor.ToBrush();
                toolbar.Background = App.ColorScheme.BackgroundColor.ToBrush();
            }

            selTabs.ApplyColorScheme(App.ColorScheme);

            // quick fix until I make a better system post-1.0
            if (App.ColorScheme.IsHighContrast)
            {
                //foreach (SelectableItem item in selTabs.GetItemsAsType<SelectableItem>())
                //{
                //    item.ApplyColorScheme(App.ColorScheme);
                //    //if (selTabs.IsEnabled)
                //    //{
                //    //    item.ApplyColorScheme(App.ColorScheme);
                //    //}
                //    //else
                //    //{
                //    //    item.Foreground = App.ColorScheme.ForegroundColor.ToBrush();
                //    //}
                //}

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
                foreach (SelectableUserControl item in selTabs.Items)
                {
                    if (item.IsEnabled)
                    {
                        item.ApplyColorScheme(App.ColorScheme);
                    }
                    else
                    {
                        item.Foreground = App.ColorScheme.DarkDisabledColor.ToBrush();
                    }
                }

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

            brdrCalculating.Background = App.ColorScheme.SecondaryColor.ToBrush();
            brdrCalculating.BorderBrush = App.ColorScheme.HighlightColor.ToBrush();

            (txtEditRaw.ContextMenu as SolidShineUi.ContextMenu)!.ApplyColorScheme(App.ColorScheme);

            expUser.Background = App.ColorScheme.LightBackgroundColor.ToBrush();
            expUser.BorderBrush = App.ColorScheme.ThirdHighlightColor.ToBrush();
            expUser.BorderThickness = new Thickness(1);

            expPhysical.Background = App.ColorScheme.LightBackgroundColor.ToBrush();
            expPhysical.BorderBrush = App.ColorScheme.ThirdHighlightColor.ToBrush();
            expPhysical.BorderThickness = new Thickness(1);

            edtFort.UpdateAppearance();
            edtReflex.UpdateAppearance();
            edtWill.UpdateAppearance();

            edtCmb.UpdateAppearance();
            edtCmd.UpdateAppearance();
            edtInit.UpdateAppearance();

            edtAc.UpdateAppearance();

            foreach (SkillEditor? item in stkSkills.Children)
            {
                if (item == null) continue;
                item.ColorScheme = ColorScheme;
                //item.UpdateAppearance();
            }

            btnNotesEdit.Background = Color.FromArgb(1, 0, 0, 0).ToBrush();
            btnNotesView.Background = Color.FromArgb(1, 0, 0, 0).ToBrush();

            //foreach (SpellEditor item in selSpells.GetItemsAsType<SpellEditor>())
            //{
            //    item.ApplyColorScheme(App.ColorScheme);
            //}
        }

        void SetupTabs()
        {
            selTabs.Items.Add(CreateTab("General"));
            selTabs.Items.Add(CreateTab("Skills"));
            selTabs.Items.Add(CreateTab("Combat"));
            selTabs.Items.Add(CreateTab("Spells"));
            selTabs.Items.Add(CreateTab("Feats/Abilities"));
            selTabs.Items.Add(CreateTab("Items"));
            selTabs.Items.Add(CreateTab("Notes"));

            SetAllTabsVisibility(Visibility.Collapsed);
        }

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

        void LoadGeneralTab()
        {
            selTabs.Items[0].IsSelected = true;
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

        private void tabItem_Click(object? sender, EventArgs e)
        {
            if (sender == null) return;
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
            ChangeTheme.ColorSchemeDialog csd = new ChangeTheme.ColorSchemeDialog();
            csd.ColorScheme = this.ColorScheme;

            csd.ShowDialog();

            if (csd.DialogResult)
            {
                App.ColorScheme = csd.SelectedColorScheme;

                if (csd.InternalColorSchemeValue != 0)
                {
                    ColorScheme cs = csd.SelectedColorScheme;

                    App.Settings.HighContrastTheme = csd.InternalColorSchemeValue.ToString();
                }
                else
                {
                    App.Settings.ThemeColor = csd.SelectedColorScheme.MainColor.GetHexString();
                    App.Settings.HighContrastTheme = NO_HIGH_CONTRAST;
                }

                SaveSettings();
                UpdateAppearance();

                // change settings if high contrast is changed
                bool isHighContrast = false;
                if (csd.InternalColorSchemeValue >= 1 && csd.InternalColorSchemeValue <= 3) isHighContrast = true;

                if (App.Settings.EditorSyntaxHighlighting && !isHighContrast)
                {
                    using (Stream? s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("PathfinderJson.Json.xshd"))
                    {
                        if (s != null)
                        {
                            using XmlReader reader = new XmlTextReader(s);
                            txtEditRaw.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, HighlightingManager.Instance);
                        }
                    }
                }
                else
                {
                    using (Stream? s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("PathfinderJson.None.xshd"))
                    {
                        if (s != null)
                        {
                            using XmlReader reader = new XmlTextReader(s);
                            txtEditRaw.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, HighlightingManager.Instance);
                        }
                    }
                }

                edtAc.ShowShieldGlyph = App.Settings.ShowGlyphs && !isHighContrast;
                eleStr.ShowBannerGlyph = App.Settings.ShowGlyphs && !isHighContrast;
                eleDex.ShowBannerGlyph = App.Settings.ShowGlyphs && !isHighContrast;
                eleCon.ShowBannerGlyph = App.Settings.ShowGlyphs && !isHighContrast;
                eleInt.ShowBannerGlyph = App.Settings.ShowGlyphs && !isHighContrast;
                eleWis.ShowBannerGlyph = App.Settings.ShowGlyphs && !isHighContrast;
                eleCha.ShowBannerGlyph = App.Settings.ShowGlyphs && !isHighContrast;
            }
        }

        void ShowHideToolbar(bool show)
        {
            if (show)
            {
                rowToolbar.Height = new GridLength(1, GridUnitType.Auto);
                rowToolbar.MinHeight = 28;
                toolbar.IsEnabled = true;
                mnuToolbar.IsChecked = true;
            }
            else
            {
                rowToolbar.Height = new GridLength(0);
                rowToolbar.MinHeight = 0;
                toolbar.IsEnabled = false;
                mnuToolbar.IsChecked = false;
            }
        }

        #endregion

        #region View options

        void ChangeView(string view, bool updateSheet = true, bool displayEmptyMessage = false, bool saveSettings = true)
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
                    mnuEditS.Visibility = Visibility.Visible;
                    colTabs.Width = new GridLength(120, GridUnitType.Auto);
                    colTabs.MinWidth = 120;
                    stkEditToolbar.Visibility = Visibility.Collapsed;
                    stkSheetEditToolbar.Visibility = Visibility.Visible;
                    if (sp != null) if (!sp.IsClosed) sp.Close();

                    mnuTabs.IsChecked = false;
                    mnuScroll.IsChecked = true;
                    mnuRawJson.IsChecked = false;

                    if (_isEditorDirty && updateSheet)
                    {
                        // update sheet from editor
                        SyncSheetFromEditor();
                    }
                    break;
                case TABS_VIEW:
                    LoadGeneralTab();

                    txtEditRaw.Visibility = Visibility.Collapsed;
                    mnuEdit.Visibility = Visibility.Collapsed;
                    mnuEditS.Visibility = Visibility.Visible;
                    colTabs.Width = new GridLength(120, GridUnitType.Auto);
                    colTabs.MinWidth = 120;
                    stkEditToolbar.Visibility = Visibility.Collapsed;
                    stkSheetEditToolbar.Visibility = Visibility.Visible;
                    if (sp != null) if (!sp.IsClosed) sp.Close();

                    mnuTabs.IsChecked = true;
                    mnuScroll.IsChecked = false;
                    mnuRawJson.IsChecked = false;

                    if (_isEditorDirty && updateSheet)
                    {
                        // update sheet from editor
                        SyncSheetFromEditor();
                    }
                    break;
                case RAWJSON_VIEW:
                    txtEditRaw.Visibility = Visibility.Visible;
                    mnuEdit.Visibility = Visibility.Visible;
                    mnuEditS.Visibility = Visibility.Collapsed;
                    colTabs.Width = new GridLength(0);
                    colTabs.MinWidth = 0;
                    stkEditToolbar.Visibility = Visibility.Visible;
                    stkSheetEditToolbar.Visibility = Visibility.Collapsed;

                    mnuTabs.IsChecked = false;
                    mnuScroll.IsChecked = false;
                    mnuRawJson.IsChecked = true;

                    if (_isTabsDirty && updateSheet)
                    {
                        SyncEditorFromSheet();
                    }
                    break;
                default:
                    ChangeView(TABS_VIEW, updateSheet, true, saveSettings);
                    break;
            }

            Visibility v = lblNoSheet.Visibility;

            if (displayEmptyMessage)
            {
                lblNoSheet.Visibility = Visibility.Visible;
                selTabs.IsEnabled = false;
                txtEditRaw.IsEnabled = false;
                grdEditDrop.Visibility = Visibility.Visible;
                SetAllTabsVisibility(Visibility.Collapsed);
                UpdateAppearance();
                foreach (SelectableItem item in selTabs.Items.SelectedItems.OfType<SelectableItem>().ToList())
                {
                    item.IsSelected = false;
                }
            }
            else
            {
                lblNoSheet.Visibility = Visibility.Collapsed;
                selTabs.IsEnabled = true;
                txtEditRaw.IsEnabled = true;
                grdEditDrop.Visibility = Visibility.Collapsed;
                if (lblNoSheet.Visibility != v)
                {
                    UpdateAppearance();
                }
            }
        }

        private void mnuScroll_Click(object sender, RoutedEventArgs e)
        {
            ChangeView(CONTINUOUS_VIEW, true, !_sheetLoaded);
        }

        private void mnuTabs_Click(object sender, RoutedEventArgs e)
        {
            ChangeView(TABS_VIEW, true, !_sheetLoaded);
        }

        private void mnuRawJson_Click(object sender, RoutedEventArgs e)
        {
            ChangeView(RAWJSON_VIEW, true, !_sheetLoaded);
        }

        private void mnuToolbar_Click(object sender, RoutedEventArgs e)
        {
            if (rowToolbar.Height == new GridLength(0))
            {
                ShowHideToolbar(true);
            }
            else
            {
                ShowHideToolbar(false);
            }

            App.Settings.ShowToolbar = toolbar.IsEnabled;
            SaveSettings();
        }

        private void mnuFilename_Click(object sender, RoutedEventArgs e)
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
            SaveSettings();
            UpdateTitlebar();
        }

        #endregion

        #region Tools menu

        DiceRollerWindow? drw = null;

        private void mnuDiceRoll_Click(object sender, RoutedEventArgs e)
        {
            if (drw != null)
            {
                drw.Show();
                drw.Focus();
            }
            else
            {
                drw = new DiceRollerWindow();
                drw.ColorScheme = App.ColorScheme;
                drw.Closed += DiceRollerWindow_Closed;
                drw.Show();
            }


        }

        private void DiceRollerWindow_Closed(object? sender, EventArgs e)
        {
            if (drw != null)
            {
                drw.Closed -= DiceRollerWindow_Closed;
                drw = null;
            }
        }

        private void mnuEditorFont_Click(object sender, RoutedEventArgs e)
        {
            FontSelectDialog fds = new FontSelectDialog();
            fds.ShowDecorations = false;
            fds.ColorScheme = App.ColorScheme;

            fds.SelectedFontFamily = txtEditRaw.FontFamily;
            fds.SelectedFontSize = txtEditRaw.FontSize;
            fds.SelectedFontStyle = txtEditRaw.FontStyle;
            fds.SelectedFontWeight = txtEditRaw.FontWeight;

            fds.ShowDialog();

            if (fds.DialogResult)
            {
                txtEditRaw.FontFamily = fds.SelectedFontFamily;
                txtEditRaw.FontSize = fds.SelectedFontSize;
                txtEditRaw.FontStyle = fds.SelectedFontStyle;
                txtEditRaw.FontWeight = fds.SelectedFontWeight;
            }

            SaveSettings(true);
        }


        private void mnuOptions_Click(object sender, RoutedEventArgs e)
        {
            Options o = new Options();
            o.Owner = this;
            o.ColorScheme = App.ColorScheme;

            o.ShowDialog();

            if (o.DialogResult)
            {
                ReloadSettings();
            }
        }

        #endregion

        #region Help menu

        private void mnuGithub_Click(object sender, RoutedEventArgs e)
        {
            OpenBrowser("https://github.com/JaykeBird/PathfinderJson/");
        }

        private void mnuFeedback_Click(object sender, RoutedEventArgs e)
        {
            OpenBrowser("https://github.com/JaykeBird/PathfinderJson/issues/new/choose");
        }

        private void mnuKeyboard_Click(object sender, RoutedEventArgs e)
        {
            OpenBrowser("https://github.com/JaykeBird/PathfinderJson/wiki/Keyboard-Shortcuts");
        }

        private void mnuMarkdown_Click(object sender, RoutedEventArgs e)
        {
            OpenBrowser("https://commonmark.org/help/");
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
            await CheckForUpdates();
        }

        private void mnuAbout_Click(object sender, RoutedEventArgs e)
        {
            About a = new About();
            a.Owner = this;
            a.ShowDialog();
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
                App.Settings.EditorWordWrap = false;
            }
            else
            {
                mnuWordWrap.IsChecked = true;
                txtEditRaw.WordWrap = true;
                App.Settings.EditorWordWrap = true;
            }

            SaveSettings();
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
            if (!_isUpdating)
            {
                _isEditorDirty = true;
                SetIsDirty(true, false);
            }
        }

        private void txtEditRaw_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop)!;

                if (!SaveDirtyChanges() || CheckCalculating())
                {
                    return;
                }

                LoadFile(files[0]);
            }
        }

        void SetSyntaxHighlighting(bool value)
        {
            if (value)
            {
                using (Stream? s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("PathfinderJson.Json.xshd"))
                {
                    if (s != null)
                    {
                        using XmlReader reader = new XmlTextReader(s);
                        txtEditRaw.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    }
                }
            }
            else
            {
                using (Stream? s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("PathfinderJson.None.xshd"))
                {
                    if (s != null)
                    {
                        using XmlReader reader = new XmlTextReader(s);
                        txtEditRaw.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    }
                }
            }

            App.Settings.EditorSyntaxHighlighting = value;
            SaveSettings();
        }

        #region Font Settings
        void LoadEditorFontSettings()
        {
            string family = App.Settings.EditorFontFamily;
            string size = App.Settings.EditorFontSize;
            string style = App.Settings.EditorFontStyle;
            string weight = App.Settings.EditorFontWeight.Replace("w", "").Replace(".", "");

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
                fs = (FontStyle?)new FontStyleConverter().ConvertFromInvariantString(style) ?? FontStyles.Normal;
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

            txtEditRaw.FontFamily = ff;
            txtEditRaw.FontSize = dsz;
            txtEditRaw.FontStyle = fs;
            txtEditRaw.FontWeight = fw;
        }

        void SaveEditorFontSettings()
        {
            string ff = (txtEditRaw.FontFamily.Source).Replace(", Consolas", "");

            App.Settings.EditorFontFamily = ff;
            App.Settings.EditorFontSize = txtEditRaw.FontSize.ToString();

            // because the ToString() method for FontStyle uses CurrentCulture rather than InvariantCulture, I need to convert it to string myself.
            if (txtEditRaw.FontStyle == FontStyles.Italic)
            {
                App.Settings.EditorFontStyle = "Italic";
            }
            else if (txtEditRaw.FontStyle == FontStyles.Oblique)
            {
                App.Settings.EditorFontStyle = "Oblique";
            }
            else
            {
                App.Settings.EditorFontStyle = "Normal";
            }
        }
        #endregion

        #endregion

        #region Load File
        void LoadFile(string filename, bool addToRecent = true)
        {
            //if (filename == null)
            //{
            //    MessageBox.Show(this, "The filename provided is not valid. No file can be opened.",
            //        "Filename Null Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //    return;
            //}

            // Prepare message dialog in case an error occurs
            MessageDialog md = new MessageDialog(App.ColorScheme)
            {
                Title = "File Format Error",
                Image = MessageDialogImage.Error,
            };

            if (IsVisible)
            {
                md.Owner = this;
            }

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

                _isUpdating = true;
                txtEditRaw.Load(filename);
                _isUpdating = false;
                ChangeView(App.Settings.StartView, false, false);
                LoadPathfinderSheet(ps);
            }
            catch (FileFormatException)
            {
                md.Message = "The file \"" + filename + "\" does not appear to be a JSON file. Check the file in Notepad or another text editor to make sure it's not corrupted.";
                md.ShowDialog();
                return;
            }
            catch (InvalidDataException)
            {
                md.Message = "The file \"" + filename + "\" does not appear to be a JSON file. Check the file in Notepad or another text editor to make sure it's not corrupted.";
                md.ShowDialog();
                return;
            }
            catch (InvalidOperationException e)
            {
                if (e.Message.Contains("error context error is different to requested error"))
                {
                    md.Message = "The file \"" + filename + "\" does not match the JSON format this program is looking for. Check the file in Notepad or another text editor to make sure it's not corrupted.";
                    md.ShowDialog();
                    return;
                }
                else
                {
                    md.Message = "The file \"" + filename + "\" cannot be opened due to this error: \n\n" + e.Message + "\n\n" +
                        "Check the file in Notepad or another text editor, or report this issue via the \"Send Feedback\" option in the Help menu.";
                    md.ShowDialog();
                    return;
                }
            }
            catch (FileNotFoundException)
            {
                md.Message = "The file \"" + filename + "\" cannot be found. Make sure the file exists and then try again.";
                md.ShowDialog();
                return;
            }

            if (addToRecent) AddRecentFile(filename);
        }

        void LoadPathfinderSheet(PathfinderSheet sheet)
        {
            // check if the userdata structure is present
            bool _userDataCheck = false;

            // first, some sanity checks and null checks

            // General tab
            if (sheet.Player != null)
            {
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
                    catch (ArgumentOutOfRangeException) { }
                }
                txtPlayerEmail.Text = email;

                try
                {
                    if (sheet.Player.Photos != null)
                    {
                        ImageSource iss = new BitmapImage(new Uri(sheet.Player.Photos[0].Value ?? ""));
                        imgPlayer.Source = iss;
                    }
                    else
                    {
                        imgPlayer.Source = null;
                    }
                }
                catch (IndexOutOfRangeException) { imgPlayer.Source = null; }
                catch (NullReferenceException) { imgPlayer.Source = null; }
                catch (ArgumentOutOfRangeException) { imgPlayer.Source = null; }
                catch (System.Net.WebException) { imgPlayer.Source = null; }

                _userDataCheck = false;
            }
            else
            {
                _userDataCheck = true;
                sheet.Player = new UserData(true);
            }

            // Equipment tab
            if (sheet.Money == null) // in sheets where the player hasn't given their character money, Mottokrosh's site doesn't add a "money" object to the JSON output
            {
                sheet.Money = new Dictionary<string, string?>();
            }

            // let's actually load the sheet now!

            CoreLoadPathfinderSheet(sheet);

            // this is a check to determine if this JSON file looks like a character sheet file or not
            // the program will happily open and work with the file, but if the user saves the file the existing data in the file will be deleted
            // thus, I'm most concerned about data loss for the user, in case the user accidentally opened the wrong file
            // the check looks at two of three things being missing:
            // 1. the character's name (name attribute)
            // 2. the player's info (user data structure)
            // 3. the character's base abilities structure (abilities structure)
            // if two of them are missing, it displays a warning dialog but continues otherwise
            if ((string.IsNullOrEmpty(sheet.Name) && (!sheet.AbilitiesPresent || _userDataCheck)) || (!sheet.AbilitiesPresent && _userDataCheck))
            {
                MessageDialog md = new MessageDialog(App.ColorScheme);
                md.Message = "This JSON file doesn't seem to look like it's a character sheet at all. " +
                    "It may be good to open the Raw JSON view to check that the file matches what you're expecting.\n\n" +
                    "PathfinderJSON will continue, but if you save any changes, any non-character sheet data may be deleted.";
                md.Title = "File Check Warning";
                md.Owner = this;
                md.Image = MessageDialogImage.Hand;
                md.ShowDialog();
            }
        }

        private void CoreLoadPathfinderSheet(PathfinderSheet sheet)
        {
            // set this flag so that the program doesn't try to set the sheet as dirty while loading in the file
            _isUpdating = true;

            if (sheet.SheetSettings != null)
            {
                sheetSettings = sheet.SheetSettings;
            }
            else
            {
                sheetSettings = new Dictionary<string, string?>();
            }

            // General tab
            ud = sheet.Player ?? new UserData();
            //ac = sheet.AC;
            sheetid = sheet.Id ?? "-1";
            version = sheet.Version;

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

            Speed? spd = sheet.Speed;
            if (spd != null)
            {
                txtSpeedBase.Text = spd.Base;
                txtSpeedArmor.Text = spd.WithArmor;
                txtSpeedBurrow.Text = spd.Burrow;
                txtSpeedClimb.Text = spd.Climb;
                txtSpeedFly.Text = spd.Fly;
                txtSpeedSwim.Text = spd.Swim;
                txtSpeedTemp.Text = spd.TempModifier;
            }

            abilities = sheet.RawAbilities;

            txtStr.Value = sheet.Strength;
            txtDex.Value = sheet.Dexterity;
            txtCha.Value = sheet.Charisma;
            txtCon.Value = sheet.Constitution;
            txtInt.Value = sheet.Intelligence;
            txtWis.Value = sheet.Wisdom;

            eleStr.Value = sheet.Strength;
            eleDex.Value = sheet.Dexterity;
            eleCha.Value = sheet.Charisma;
            eleCon.Value = sheet.Constitution;
            eleInt.Value = sheet.Intelligence;
            eleWis.Value = sheet.Wisdom;

            LoadTempStat("tempStr", grdTempStr, btnTempStr, eleStr, stkTempStr);
            LoadTempStat("tempDex", grdTempDex, btnTempDex, eleDex, stkTempDex);
            LoadTempStat("tempCha", grdTempCha, btnTempCha, eleCha, stkTempCha);
            LoadTempStat("tempCon", grdTempCon, btnTempCon, eleCon, stkTempCon);
            LoadTempStat("tempInt", grdTempInt, btnTempInt, eleInt, stkTempInt);
            LoadTempStat("tempWis", grdTempWis, btnTempWis, eleWis, stkTempWis);

            void LoadTempStat(string statName, TempAbilityDisplay control, FlatButton button, AbilityScoreIconEditor iconControl, StackPanel control2)
            {
                if (abilities.ContainsKey(statName))
                {
                    control.Visibility = Visibility.Visible;
                    control2.Visibility = Visibility.Visible;
                    button.Visibility = Visibility.Collapsed;
                    control.Value = ParseStringAsInt(abilities[statName], 10);
                    ((TempAbilityDisplay)control2.Children[1]).Value = ParseStringAsInt(abilities[statName], 10);
                    iconControl.HideTempButton();
                }
                else
                {
                    control.Visibility = Visibility.Collapsed;
                    control2.Visibility = Visibility.Collapsed;
                    button.Visibility = Visibility.Visible;
                    iconControl.ShowTempButton();
                }
            }

            abilityMods.Clear();
            abilityMods["STR"] = CalculateModifierInt(sheet.Strength);
            abilityMods["DEX"] = CalculateModifierInt(sheet.Dexterity);
            abilityMods["CHA"] = CalculateModifierInt(sheet.Charisma);
            abilityMods["CON"] = CalculateModifierInt(sheet.Constitution);
            abilityMods["INT"] = CalculateModifierInt(sheet.Intelligence);
            abilityMods["WIS"] = CalculateModifierInt(sheet.Wisdom);

            txtStrm.Text = DisplayModifier(abilityMods["STR"]);
            txtDexm.Text = DisplayModifier(abilityMods["DEX"]);
            txtCham.Text = DisplayModifier(abilityMods["CHA"]);
            txtConm.Text = DisplayModifier(abilityMods["CON"]);
            txtIntm.Text = DisplayModifier(abilityMods["INT"]);
            txtWism.Text = DisplayModifier(abilityMods["WIS"]);

            edtFort.LoadModifier(sheet.Saves.ContainsKey("fort") ? sheet.Saves["fort"] : new CompoundModifier(), txtConm.Text);
            edtReflex.LoadModifier(sheet.Saves.ContainsKey("reflex") ? sheet.Saves["reflex"] : new CompoundModifier(), txtDexm.Text);
            edtWill.LoadModifier(sheet.Saves.ContainsKey("will") ? sheet.Saves["will"] : new CompoundModifier(), txtWism.Text);

            //if (sheet.Saves.ContainsKey("fort")) edtFort.LoadModifier(sheet.Saves["fort"], txtConm.Text);
            //else edtFort.LoadModifier(new CompoundModifier(), txtConm.Text);

            //if (sheet.Saves.ContainsKey("reflex")) edtReflex.LoadModifier(sheet.Saves["reflex"], txtDexm.Text);
            //else edtReflex.LoadModifier(new CompoundModifier(), txtDexm.Text);

            //if (sheet.Saves.ContainsKey("will")) edtWill.LoadModifier(sheet.Saves["will"], txtWism.Text);
            //else edtWill.LoadModifier(new CompoundModifier(), txtWism.Text);

            txtHpTotal.ValueString = sheet.HP.Total ?? "";
            txtHpWounds.ValueString = sheet.HP.Wounds ?? "";
            txtHpNl.Text = sheet.HP.NonLethal;

            Dictionary<string, string?> xp = sheet.Xp ?? new Dictionary<string, string?>();
            txtXpNl.ValueString = xp.ContainsKey("toNextLevel") ? xp["toNextLevel"] ?? "" : "";
            txtXpTotal.ValueString = xp.ContainsKey("total") ? xp["total"] ?? "" : "";

            txtLanguages.Text = sheet.Languages;

            // Combat tab
            edtAc.LoadArmorClass(sheet.AC, txtDexm.Text);
            edtInit.LoadModifier(sheet.Initiative, txtDexm.Text);
            txtBab.Text = sheet.BaseAttackBonus;
            edtCmb.LoadModifier(sheet.CombatManeuverBonus, txtStrm.Text, "", txtBab.Text);
            edtCmd.LoadModifier(sheet.CombatManeuverDefense, txtStrm.Text, txtDexm.Text, txtBab.Text);
            txtDr.Text = sheet.DamageReduction;
            txtResist.Text = sheet.Resistances;

            UpdateInternalBab();

            selMelee.LoadList(sheet.MeleeWeapons);
            selRanged.LoadList(sheet.RangedWeapons);

            selAcItem.Items.Clear();
            foreach (AcItem item in sheet.AC.Items)
            {
                AcItemEditor ae = new AcItemEditor();
                ae.ContentChanged += editor_ContentChanged;
                ae.LoadAcItem(item);
                selAcItem.Items.Add(ae);
            }

            AcItem total = sheet.AC.ItemTotals;
            txtAcBonus.Text = total.Bonus;
            txtAcPenalty.Text = total.ArmorCheckPenalty;
            txtAcSpellFailure.Text = total.SpellFailure;
            txtAcWeight.Text = total.Weight;

            // Feats/Abilities tab
            selFeats.LoadList(sheet.Feats);

            selAbilities.LoadList(sheet.SpecialAbilities);

            selTraits.LoadList(sheet.Traits);

            selSpellLikes.Items.Clear();
            foreach (Spell item in sheet.SpellLikeAbilities)
            {
                SpellEditor se = new SpellEditor();
                se.ContentChanged += editor_ContentChanged;
                se.ApplyColorScheme(App.ColorScheme);
                se.LoadSpell(item);
                selSpellLikes.Items.Add(se);
            }

            // Equipment tab
            Dictionary<string, string?> money = sheet.Money ?? new Dictionary<string, string?>();

            txtMoneyCp.ValueString = money.ContainsKey("cp") ? money["cp"] ?? "0" : "0";
            txtMoneySp.ValueString = money.ContainsKey("sp") ? money["sp"] ?? "0" : "0";
            txtMoneyGp.ValueString = money.ContainsKey("gp") ? money["gp"] ?? "0" : "0";
            txtMoneyPp.ValueString = money.ContainsKey("pp") ? money["pp"] ?? "0" : "0";
            txtGemsArt.Text = money.ContainsKey("gems") ? money["gems"] : "";
            txtOtherTreasure.Text = money.ContainsKey("other") ? money["other"] : "";

            selEquipment.LoadList(sheet.Equipment);

            // Skills tab
            txtSkillModifiers.Text = sheet.SkillConditionalModifiers;

            // trying to do this asynchronously... key word being "trying"
            // (update: it didn't really do anything lol)
            //var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            //var ses = await Task<List<SkillEditor>>.Factory.StartNew(() => SkillEditorFactory.CreateEditors(sheet, this), cts.Token, TaskCreationOptions.None, scheduler);

            var ses = SkillEditorFactory.CreateEditors(sheet, this);

            stkSkills.Children.Clear();
            foreach (SkillEditor item in ses)
            {
                item.ModifierValue = abilityMods[item.ModifierName];
                item.UpdateCalculations();

                item.ContentChanged += editor_ContentChanged;
                item.ModifierChanged += editor_ModifierChanged;

                stkSkills.Children.Add(item);
                item.ColorScheme = ColorScheme;
                //item.UpdateAppearance();
            }

            if (sheetSettings.ContainsKey("skillModSet"))
            {
                LoadSkillModSubstitutions(sheetSettings["skillModSet"] ?? "");
            }

            // Spells tab
            int currentLevel = 0;
            List<Spell> allSpells = new List<Spell>();

            foreach (SpellLevel item in sheet.Spells)
            {
                switch (currentLevel)
                {
                    case 0:
                        txtSpellsBonus0.ValueString = item.BonusSpells ?? "";
                        txtSpellsDC0.Value = ParseStringAsInt(item.SaveDC);
                        txtSpellsKnown0.Value = ParseStringAsInt(item.TotalKnown);
                        txtSpellsPerDay0.Value = ParseStringAsInt(item.TotalPerDay);
                        break;
                    case 1:
                        txtSpellsBonus1.ValueString = item.BonusSpells ?? "";
                        txtSpellsDC1.Value = ParseStringAsInt(item.SaveDC);
                        txtSpellsKnown1.Value = ParseStringAsInt(item.TotalKnown);
                        txtSpellsPerDay1.Value = ParseStringAsInt(item.TotalPerDay);
                        break;
                    case 2:
                        txtSpellsBonus2.ValueString = item.BonusSpells ?? "";
                        txtSpellsDC2.Value = ParseStringAsInt(item.SaveDC);
                        txtSpellsKnown2.Value = ParseStringAsInt(item.TotalKnown);
                        txtSpellsPerDay2.Value = ParseStringAsInt(item.TotalPerDay);
                        break;
                    case 3:
                        txtSpellsBonus3.ValueString = item.BonusSpells ?? "";
                        txtSpellsDC3.Value = ParseStringAsInt(item.SaveDC);
                        txtSpellsKnown3.Value = ParseStringAsInt(item.TotalKnown);
                        txtSpellsPerDay3.Value = ParseStringAsInt(item.TotalPerDay);
                        break;
                    case 4:
                        txtSpellsBonus4.ValueString = item.BonusSpells ?? "";
                        txtSpellsDC4.Value = ParseStringAsInt(item.SaveDC);
                        txtSpellsKnown4.Value = ParseStringAsInt(item.TotalKnown);
                        txtSpellsPerDay4.Value = ParseStringAsInt(item.TotalPerDay);
                        break;
                    case 5:
                        txtSpellsBonus5.ValueString = item.BonusSpells ?? "";
                        txtSpellsDC5.Value = ParseStringAsInt(item.SaveDC);
                        txtSpellsKnown5.Value = ParseStringAsInt(item.TotalKnown);
                        txtSpellsPerDay5.Value = ParseStringAsInt(item.TotalPerDay);
                        break;
                    case 6:
                        txtSpellsBonus6.ValueString = item.BonusSpells ?? "";
                        txtSpellsDC6.Value = ParseStringAsInt(item.SaveDC);
                        txtSpellsKnown6.Value = ParseStringAsInt(item.TotalKnown);
                        txtSpellsPerDay6.Value = ParseStringAsInt(item.TotalPerDay);
                        break;
                    case 7:
                        txtSpellsBonus7.ValueString = item.BonusSpells ?? "";
                        txtSpellsDC7.Value = ParseStringAsInt(item.SaveDC);
                        txtSpellsKnown7.Value = ParseStringAsInt(item.TotalKnown);
                        txtSpellsPerDay7.Value = ParseStringAsInt(item.TotalPerDay);
                        break;
                    case 8:
                        txtSpellsBonus8.ValueString = item.BonusSpells ?? "";
                        txtSpellsDC8.Value = ParseStringAsInt(item.SaveDC);
                        txtSpellsKnown8.Value = ParseStringAsInt(item.TotalKnown);
                        txtSpellsPerDay8.Value = ParseStringAsInt(item.TotalPerDay);
                        break;
                    case 9:
                        txtSpellsBonus9.ValueString = item.BonusSpells ?? "";
                        txtSpellsDC9.Value = ParseStringAsInt(item.SaveDC);
                        txtSpellsKnown9.Value = ParseStringAsInt(item.TotalKnown);
                        txtSpellsPerDay9.Value = ParseStringAsInt(item.TotalPerDay);
                        break;
                    default:
                        break;
                }

                if (item.Spells != null)
                {
                    foreach (Spell sp in item.Spells)
                    {
                        allSpells.Add(sp);
                    }
                }

                currentLevel++;
            } // end foreach

            txtSpellSpecialty.Text = sheet.SpellsSpeciality;
            txtSpellConditionalModifiers.Text = sheet.SpellsConditionalModifiers;

            selSpells.Items.Clear();
            foreach (Spell spell in allSpells)
            {
                SpellEditor se = new SpellEditor();
                se.ContentChanged += editor_ContentChanged;
                se.ApplyColorScheme(App.ColorScheme);
                se.LoadSpell(spell);
                selSpells.Items.Add(se);
            }

            // Notes tab / Calculations
            LoadSheetSettings();

            txtNotes.Text = sheet.Notes;
            UpdateMarkdownViewerVisuals();

            _isUpdating = false;
        }
        #endregion

        #region Sync Editors / update sheet / CreatePathfinderSheetAsync

        #region Update UI (Calculate menu)

        private async void mnuUpdate_Click(object sender, RoutedEventArgs e)
        {
            await UpdateCalculations(true, mnuUpdateTotals.IsChecked, mnuUpdateAc.IsChecked);
        }

        private void mnuUpdateAc_Click(object sender, RoutedEventArgs e)
        {
            mnuUpdateAc.IsChecked = !mnuUpdateAc.IsChecked;
            edtAc.CalculateAcValueChanged(mnuUpdateAc.IsChecked);
            if (_sheetLoaded && !_isUpdating)
            {
                sheetSettings["calcIncludeAc"] = mnuUpdateAc.IsChecked ? "true" : "false";
            }
        }

        private void mnuAutoUpdate_Click(object sender, RoutedEventArgs e)
        {
            mnuAutoUpdate.IsChecked = !mnuAutoUpdate.IsChecked;
            if (_sheetLoaded && !_isUpdating)
            {
                sheetSettings["calcAutorun"] = mnuAutoUpdate.IsChecked ? "true" : "false";
            }
        }

        private void mnuUpdateTotals_Click(object sender, RoutedEventArgs e)
        {
            mnuUpdateTotals.IsChecked = !mnuUpdateTotals.IsChecked;
            if (_sheetLoaded && !_isUpdating)
            {
                sheetSettings["calcIncludeTotals"] = mnuUpdateTotals.IsChecked ? "true" : "false";
            }
        }

        #endregion

        void UpdateInternalBab()
        {
            string bab = txtBab.Text;
            string buffer = "";

            if (string.IsNullOrEmpty(bab))
            {
                babCalc = 0;
                return;
            }

            foreach (char c in bab)
            {
                if (char.IsDigit(c) || c == '+' || c == '-')
                {
                    buffer += c;
                }
                else
                {
                    break;
                }
            }

            bool res = int.TryParse(buffer, out int value);

            if (res)
            {
                babCalc = value;
            }
            else
            {
                babCalc = 0;
            }
        }

        async Task UpdateCalculations(bool skills = true, bool totals = true, bool ac = true)
        {
            if (!_sheetLoaded)
            {
                MessageDialog md = new MessageDialog(App.ColorScheme);
                md.ShowDialog("Cannot run calculations when no sheet is opened.", App.ColorScheme, this, "Update Calculations", MessageDialogButtonDisplay.Auto, image: MessageDialogImage.Error);
                return;
            }

            if (_isCalculating)
            {
                return;
            }

            _isUpdating = true;

            if (currentView == RAWJSON_VIEW && _isEditorDirty)
            {
                SyncSheetFromEditor();
            }

            _isCalculating = true;
            brdrCalculating.Visibility = Visibility.Visible;

            UpdateInternalBab();

            if (grdAbilityIcon.Visibility == Visibility.Visible)
            {
                ApplyIconValuesToTable();
            }

            txtStrm.Text = CalculateModifier(txtStr.Value);
            txtDexm.Text = CalculateModifier(txtDex.Value);
            txtCham.Text = CalculateModifier(txtCha.Value);
            txtConm.Text = CalculateModifier(txtCon.Value);
            txtIntm.Text = CalculateModifier(txtInt.Value);
            txtWism.Text = CalculateModifier(txtWis.Value);

            edtFort.UpdateCoreModifier(txtConm.Text);
            edtReflex.UpdateCoreModifier(txtDexm.Text);
            edtWill.UpdateCoreModifier(txtWism.Text);

            edtAc.UpdateCoreModifier(txtDexm.Text);
            edtInit.UpdateCoreModifier(txtDexm.Text);
            edtCmb.UpdateCoreModifier(txtStrm.Text, "", babCalc.ToString());
            edtCmd.UpdateCoreModifier(txtStrm.Text, txtDexm.Text, babCalc.ToString());

            if (skills)
            {
                foreach (SkillEditor? item in stkSkills.Children)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    string modifier = "";

                    switch (item.ModifierName)
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
                    item.ModifierValue = int.Parse(modifier);

                    if (totals)
                    {
                        item.UpdateCalculations();
                    }
                }
            }

            if (ac)
            {
                // special calculations for AC items
                int acShield = 0;
                int acArmor = 0;

                int tWeight = 0;
                int tBonus = 0;
                int tSpellcheck = 0;
                int tPenalty = 0;

                // TODO: switch over to load items from ILD
                foreach (AcItemEditor acItem in selAcItem.Items.OfType<AcItemEditor>())
                {
                    AcItem ai = acItem.GetAcItem();
                    if ((ai.Name ?? "").ToLowerInvariant().Contains("shield") || (ai.Type ?? "").ToLowerInvariant().Contains("shield"))
                    {
                        // this is a shield
                        acShield += ParseStringAsInt(ai.Bonus);
                    }
                    else
                    {
                        // probably not a shield? consider it armor
                        acArmor += ParseStringAsInt(ai.Bonus);
                    }

                    tBonus += ParseStringAsInt(ai.Bonus);
                    tSpellcheck += ParseStringAsInt((ai.SpellFailure ?? "").Replace("%", ""));
                    tPenalty += ParseStringAsInt(ai.ArmorCheckPenalty);
                    tWeight += ParseStringAsInt(ai.Weight);
                }

                txtAcBonus.Text = tBonus.ToString();
                txtAcPenalty.Text = tPenalty.ToString();
                txtAcSpellFailure.Text = tSpellcheck.ToString() + "%";
                txtAcWeight.Text = tWeight.ToString();

                edtAc.UpdateAcItemBonuses(acShield.ToString(), acArmor.ToString());
            }

            if (totals)
            {
                edtFort.UpdateTotal();
                edtReflex.UpdateTotal();
                edtWill.UpdateTotal();

                edtAc.UpdateTotal();
                edtInit.UpdateTotal();
                edtCmb.UpdateTotal();
                edtCmd.UpdateTotal();
            }

            if (currentView == RAWJSON_VIEW)
            {
                SyncEditorFromSheet();
            }

            _isCalculating = false;
            brdrCalculating.Visibility = Visibility.Collapsed;

            _isUpdating = false;

            SetIsDirty();
        }

        #region Sync

        /// <summary>
        /// Update the sheet views from data in the text editor. Also sets the editor as no longer dirty (out-of-sync), as long as the editor has valid JSON.
        /// </summary>
        /// <returns></returns>
        void SyncSheetFromEditor()
        {
            if (!string.IsNullOrEmpty(txtEditRaw.Text))
            {
                // if no file is loaded, we don't want to write empty data into the raw JSON editor
                if (!_sheetLoaded)
                {
                    return;
                }

                try
                {
                    PathfinderSheet ps = PathfinderSheet.LoadJsonText(txtEditRaw.Text);
                    LoadPathfinderSheet(ps);
                    fileTitle = ps.Name;
                    _isEditorDirty = false;
                    if (fileTitle != displayedTitle) UpdateTitlebar();
                }
                catch (Newtonsoft.Json.JsonReaderException)
                {
                    _isEditorDirty = false;
                    _isTabsDirty = true;
                    SetIsDirty(true, false);
                }
                catch (FormatException)
                {
                    _isEditorDirty = false;
                    _isTabsDirty = true;
                    SetIsDirty(true, false);

                    MessageDialog md = new MessageDialog(App.ColorScheme);
                    md.Message = "This JSON file doesn't seem to look like it's a character sheet at all. " +
                        "It may be good to open the Raw JSON view to check that the file matches what you're expecting.\n\n" +
                        "PathfinderJSON will continue, but if you save any changes, any non-character sheet data may be deleted.";
                    md.Title = "File Check Warning";
                    md.Owner = this;
                    md.Image = MessageDialogImage.Hand;
                    md.ShowDialog();
                }
                catch (Newtonsoft.Json.JsonSerializationException)
                {
                    _isEditorDirty = false;
                    _isTabsDirty = true;
                    SetIsDirty(true, false);

                    MessageDialog md = new MessageDialog(App.ColorScheme);
                    md.Message = "This JSON file doesn't seem to look like it's a character sheet at all. " +
                        "It may be good to open the Raw JSON view to check that the file matches what you're expecting.\n\n" +
                        "PathfinderJSON will continue, but if you save any changes, any non-character sheet data may be deleted.";
                    md.Title = "File Check Warning";
                    md.Owner = this;
                    md.Image = MessageDialogImage.Hand;
                    md.ShowDialog();
                }
            }
            else
            {
                _isEditorDirty = false;
                _isTabsDirty = true;
                SetIsDirty(true, false);
            }
        }

        /// <summary>
        /// Update the editor view from data in the sheet views. Also sets the sheet as no longer dirty (out-of-sync).
        /// </summary>
        /// <returns></returns>
        void SyncEditorFromSheet()
        {
            // if no file is loaded, we don't want to write empty data into the raw JSON editor
            if (!_sheetLoaded)
            {
                return;
            }

            PathfinderSheet ps = CreatePathfinderSheet();
            txtEditRaw.Text = ps.SaveJsonText(App.Settings.IndentJsonData);
            _isTabsDirty = false;
        }

        // these two menu commands are hidden
        // hopefully, we shouldn't be needing these commands at all, as the program should automatically do the syncing as needed
        // but we'll have to see if any bugs come up
        private void mnuRefresh_Click(object sender, RoutedEventArgs e)
        {
            SyncSheetFromEditor();
        }

        private void mnuRefreshEditor_Click(object sender, RoutedEventArgs e)
        {
            SyncEditorFromSheet();
        }

        #endregion

        /// <summary>
        /// Create a PathfinderSheet object by loading in all the values from the sheet view.
        /// </summary>
        /// <returns></returns>
        private PathfinderSheet CreatePathfinderSheet()
        {
            //await Task.Delay(10);
            //return await Task.Run(() => {
            PathfinderSheet sheet = new PathfinderSheet();

            // just starting going through all the controls and writing their values into the sheet object lol

            sheet.Player = ud;
            sheet.Id = sheetid;
            sheet.Version = version;

            sheet.Notes = txtNotes.Text;

            if (!SpellCheck.GetIsEnabled(txtNotes))
            {
                sheetSettings["notesNoSpellCheck"] = "enabled";
            }

            if (chkNotesMarkdown.IsChecked)
            {
                sheetSettings["notesMarkdown"] = "enabled";
            }
            //sheet.NotesMarkdown = chkNotesMarkdown.IsChecked;

            sheet.Name = txtCharacter.Text;
            sheet.Level = txtLevel.Text;
            sheet.Alignment = GetStringOrNull(txtAlignment.Text);
            sheet.Homeland = GetStringOrNull(txtHomeland.Text);
            sheet.Deity = GetStringOrNull(txtDeity.Text);

            sheet.Race = GetStringOrNull(txtPhyRace.Text);
            sheet.Gender = GetStringOrNull(txtPhyGender.Text);
            sheet.Size = GetStringOrNull(txtPhySize.Text);
            sheet.Age = GetStringOrNull(txtPhyAge.Text);
            sheet.Height = GetStringOrNull(txtPhyHeight.Text);
            sheet.Weight = GetStringOrNull(txtPhyWeight.Text);
            sheet.Hair = GetStringOrNull(txtPhyHair.Text);
            sheet.Eyes = GetStringOrNull(txtPhyEyes.Text);

            Speed? sp = new Speed();
            sp.Base = GetStringOrNull(txtSpeedBase.Text, true);
            sp.Burrow = GetStringOrNull(txtSpeedBase.Text, true);
            sp.Climb = GetStringOrNull(txtSpeedBase.Text, true);
            sp.Fly = GetStringOrNull(txtSpeedBase.Text, true);
            sp.Swim = GetStringOrNull(txtSpeedBase.Text, true);
            sp.WithArmor = GetStringOrNull(txtSpeedBase.Text, true);
            sp.TempModifier = GetStringOrNull(txtSpeedTemp.Text);

            if (sp.Base == null && sp.Burrow == null && sp.Climb == null &&
                sp.Fly == null && sp.Swim == null && sp.WithArmor == null
                && sp.TempModifier == null)
            {
                sp = null;
            }
            sheet.Speed = sp;

            _isUpdating = true;
            if (grdAbilityIcon.Visibility == Visibility.Visible)
            {
                ApplyIconValuesToTable();
            }
            _isUpdating = false;

            //Dictionary<string, string> abilities = new Dictionary<string, string>
            //{
            //    { "str", txtStr.Value.ToString() },
            //    { "dex", txtDex.Value.ToString() },
            //    { "cha", txtCha.Value.ToString() },
            //    { "con", txtCon.Value.ToString() },
            //    { "int", txtInt.Value.ToString() },
            //    { "wis", txtWis.Value.ToString() }
            //};
            abilities["str"] = txtStr.Value.ToString();
            abilities["dex"] = txtDex.Value.ToString();
            abilities["cha"] = txtCha.Value.ToString();
            abilities["con"] = txtCon.Value.ToString();
            abilities["int"] = txtInt.Value.ToString();
            abilities["wis"] = txtWis.Value.ToString();
            sheet.RawAbilities = abilities;

            // temp abilities
            if (grdTempStr.Visibility == Visibility.Visible) abilities["tempStr"] = grdTempStr.Value.ToString();
            if (grdTempDex.Visibility == Visibility.Visible) abilities["tempDex"] = grdTempStr.Value.ToString();
            if (grdTempCha.Visibility == Visibility.Visible) abilities["tempCha"] = grdTempStr.Value.ToString();
            if (grdTempCon.Visibility == Visibility.Visible) abilities["tempCon"] = grdTempStr.Value.ToString();
            if (grdTempInt.Visibility == Visibility.Visible) abilities["tempInt"] = grdTempStr.Value.ToString();
            if (grdTempWis.Visibility == Visibility.Visible) abilities["tempWis"] = grdTempStr.Value.ToString();

            // also set the actual ability values, to fix bugs with the undo stack
            sheet.Strength = txtStr.Value;
            sheet.Dexterity = txtDex.Value;
            sheet.Charisma = txtCha.Value;
            sheet.Constitution = txtCon.Value;
            sheet.Intelligence = txtInt.Value;
            sheet.Wisdom = txtWis.Value;

            Dictionary<string, CompoundModifier> saves = new Dictionary<string, CompoundModifier>
            {
                { "fort", edtFort.GetModifier() },
                { "reflex", edtReflex.GetModifier() },
                { "will", edtWill.GetModifier() }
            };
            sheet.Saves = saves;

            sheet.HP = new HP();
            sheet.HP.Total = GetStringOrNull(txtHpTotal.ValueString, true);
            sheet.HP.Wounds = GetStringOrNull(txtHpWounds.ValueString, true);
            sheet.HP.NonLethal = GetStringOrNull(txtHpNl.Text, true);

            Dictionary<string, string?> xp = new Dictionary<string, string?>
            {
                { "total", GetStringOrNull(txtXpTotal.ValueString, true) },
                { "toNextLevel", GetStringOrNull(txtXpNl.ValueString, true) }
            };
            sheet.Xp = xp;

            sheet.Languages = txtLanguages.Text;

            // ArmorClass saving
            ArmorClass ac = edtAc.GetArmorClass();

            foreach (AcItemEditor itEd in selAcItem.Items.OfType<AcItemEditor>())
            {
                ac.Items.Add(itEd.GetAcItem());
            }

            AcItem totals = new AcItem();
            totals.Bonus = GetStringOrNull(txtAcBonus.Text, true);
            totals.ArmorCheckPenalty = GetStringOrNull(txtAcPenalty.Text, true);
            totals.SpellFailure = GetStringOrNull(txtAcSpellFailure.Text, true);
            totals.Weight = GetStringOrNull(txtAcWeight.Text, true);
            ac.ItemTotals = totals;

            sheet.AC = ac;

            sheet.Initiative = edtInit.GetModifier();
            sheet.BaseAttackBonus = txtBab.Text;
            sheet.CombatManeuverBonus = edtCmb.GetModifier();
            sheet.CombatManeuverDefense = edtCmd.GetModifier();
            sheet.DamageReduction = GetStringOrNull(txtDr.Text);
            sheet.Resistances = GetStringOrNull(txtResist.Text);

            sheet.MeleeWeapons = selMelee.GetItems<Weapon>();

            sheet.RangedWeapons = selRanged.GetItems<Weapon>();

            // feats/abilites
            sheet.Feats = selFeats.GetItems<Feat>();
            //foreach (FeatEditor item in selFeats.GetItemsAsType<FeatEditor>())
            //{
            //    sheet.Feats.Add(item.GetFeat());
            //}

            sheet.SpecialAbilities = selAbilities.GetItems<SpecialAbility>();
            //sheet.SpecialAbilities = new List<SpecialAbility>();
            //foreach (AbilityEditor item in selAbilities.GetItemsAsType<AbilityEditor>())
            //{
            //    sheet.SpecialAbilities.Add(item.GetAbility());
            //}

            sheet.Traits = selAbilities.GetItems<SpecialAbility>();
            //sheet.Traits = new List<SpecialAbility>();
            //foreach (AbilityEditor item in selTraits.GetItemsAsType<AbilityEditor>())
            //{
            //    sheet.Traits.Add(item.GetAbility());
            //}

            sheet.SpellLikeAbilities = new List<Spell>();
            foreach (SpellEditor item in selSpellLikes.Items.OfType<SpellEditor>())
            {
                sheet.SpellLikeAbilities.Add(item.GetSpell());
            }

            // equipment
            sheet.Money = new Dictionary<string, string?>
            {
                { "cp", GetStringOrNull(txtMoneyCp.Value.ToString(), true) },
                { "sp", GetStringOrNull(txtMoneySp.Value.ToString(), true) },
                { "gp", GetStringOrNull(txtMoneyGp.Value.ToString(), true) },
                { "pp", GetStringOrNull(txtMoneyPp.Value.ToString(), true) },
                { "gems", GetStringOrNull(txtGemsArt.Text, true) },
                { "other", GetStringOrNull(txtOtherTreasure.Text, true) }
            };

            // if no money is set, then just set the whole value as null
            // (when the whole value is null, the money JSON element just won't be written at all)
            bool allNull = true;
            foreach (string? item in sheet.Money.Values)
            {
                if (item != null) allNull = false;
            }

            if (allNull) // set the whole value to null
            {
                sheet.Money = null;
            }

            sheet.Equipment = selEquipment.GetItems<Equipment>();
            //foreach (ItemEditor item in selEquipment.GetItemsAsType<ItemEditor>())
            //{
            //    sheet.Equipment.Add(item.GetEquipment());
            //}

            // skills
            Dictionary<string, Skill> skills = new Dictionary<string, Skill>();
            foreach (SkillEditor? item in stkSkills.Children)
            {
                if (item == null) continue;
                skills.Add(item.InternalSkillName, item.GetSkillData());
            }

            if (!string.IsNullOrWhiteSpace(txtSkillModifiers.Text))
            {
                sheet.SkillConditionalModifiers = txtSkillModifiers.Text;
                skills.Add("conditionalModifiers", new Skill("conditionalModifiers", txtSkillModifiers.Text));
            }

            sheet.Skills = skills;

            if (skillModSubs.Count > 0)
            {
                sheetSettings["skillModSet"] = string.Join(";", GetStringListFromDictionary());
            }

            IEnumerable<string> GetStringListFromDictionary()
            {
                foreach (KeyValuePair<string, string> item in skillModSubs)
                {
                    yield return item.Key + "," + item.Value;
                }
            }

            // spells

            List<Spell> allspells = new List<Spell>();
            foreach (SpellEditor item in selSpells.Items.OfType<SpellEditor>())
            {
                allspells.Add(item.GetSpell());
            }

            sheet.Spells = new List<SpellLevel>(10);
            for (int i = 0; i < 10; i++)
            {
                SpellLevel sl = new SpellLevel();

                sl.TotalKnown = GetStringOrNull(((IntegerSpinner)grdSpells.FindName("txtSpellsKnown" + i)).Value.ToString(), true);
                sl.SaveDC = GetStringOrNull(((IntegerSpinner)grdSpells.FindName("txtSpellsDC" + i)).Value.ToString(), true);
                sl.TotalPerDay = GetStringOrNull(((IntegerSpinner)grdSpells.FindName("txtSpellsPerDay" + i)).Value.ToString(), true);
                sl.BonusSpells = GetStringOrNull(((TextBox)grdSpells.FindName("txtSpellsBonus" + i)).Text.ToString(), true);

                sl.Spells = new List<Spell>();

                foreach (Spell item in allspells)
                {
                    if (item.Level == i) sl.Spells.Add(item);
                }

                if (sl.Spells.Count == 0) sl.Spells = null;

                sheet.Spells.Add(sl);
            }
            sheet.SpellsConditionalModifiers = GetStringOrNull(txtSpellConditionalModifiers.Text);
            sheet.SpellsSpeciality = GetStringOrNull(txtSpellSpecialty.Text);

            if (sheetSettings.Count > 0)
            {
                sheet.SheetSettings = sheetSettings;
            }

            return sheet;
            //});
        }

        #endregion

        #region General sheet event handlers

        private void scrSheet_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop)!;

                if (!SaveDirtyChanges() || CheckCalculating())
                {
                    return;
                }

                LoadFile(files[0]);
            }
        }

        private void textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isUpdating)
            {
                SetIsDirty();
                StartUndoTimer(sender as UIElement ?? new TextBox());
            }

            lastEditedItem = sender as UIElement;
        }

        private void nud_TextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!_isUpdating)
            {
                SetIsDirty();
                StartUndoTimer(sender as UIElement ?? new IntegerSpinner());
            }

            lastEditedItem = sender as UIElement;
        }

        private void editor_ContentChanged(object? sender, EventArgs e)
        {
            if (!_isUpdating)
            {
                SetIsDirty();
                CreateUndoState();
            }
        }

        private async void txtStr_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!_isUpdating)
            {
                txtStrm.Text = CalculateModifier(txtStr.Value);
                txtDexm.Text = CalculateModifier(txtDex.Value);
                txtCham.Text = CalculateModifier(txtCha.Value);
                txtConm.Text = CalculateModifier(txtCon.Value);
                txtIntm.Text = CalculateModifier(txtInt.Value);
                txtWism.Text = CalculateModifier(txtWis.Value);

                SetIsDirty();
                CreateUndoState();

                if (mnuAutoUpdate.IsChecked)
                {
                    await UpdateCalculations(true, false, false);
                }
            }
        }

        private async void eleStr_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!_isUpdating)
            {
                txtStrm.Text = CalculateModifier(eleStr.Value);
                txtDexm.Text = CalculateModifier(eleDex.Value);
                txtCham.Text = CalculateModifier(eleCha.Value);
                txtConm.Text = CalculateModifier(eleCon.Value);
                txtIntm.Text = CalculateModifier(eleInt.Value);
                txtWism.Text = CalculateModifier(eleWis.Value);

                SetIsDirty();
                CreateUndoState();

                if (mnuAutoUpdate.IsChecked)
                {
                    await UpdateCalculations(true, false, false);
                }
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
                StartUndoTimer(txtCharacter);
            }
        }

        private void btnEditPlayerData_Click(object sender, RoutedEventArgs e)
        {
            UserdataEditor ude = new UserdataEditor();
            ude.LoadUserData(ud);
            ude.Owner = this;

            ude.ShowDialog();
            if (ude.DialogResult)
            {
                // update userdata
                ud = ude.GetUserData();

                // update UI with new userdata
                txtPlayerName.Text = ud.DisplayName;

                string email = "";

                foreach (UserData.Email item in ud.Emails)
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
                        email = ud.Emails[0].Value;
                    }
                    catch (IndexOutOfRangeException) { }
                    catch (ArgumentOutOfRangeException) { }
                }
                txtPlayerEmail.Text = email;

                try
                {
                    if (ud.Photos != null)
                    {
                        ImageSource iss = new BitmapImage(new Uri(ud.Photos[0].Value ?? ""));
                        imgPlayer.Source = iss;
                    }
                    else
                    {
                        imgPlayer.Source = null;
                    }
                }
                catch (IndexOutOfRangeException) { imgPlayer.Source = null; }
                catch (NullReferenceException) { imgPlayer.Source = null; }
                catch (ArgumentOutOfRangeException) { imgPlayer.Source = null; }
                catch (System.Net.WebException) { imgPlayer.Source = null; }
                catch (UriFormatException) { imgPlayer.Source = null; }

                SetIsDirty();
                CreateUndoState();
            }
        }

        private void txtStr_LostFocus(object sender, RoutedEventArgs e)
        {
            //if (mnuAutoUpdate.IsChecked)
            //{
            //    await UpdateCalculations(true, false, false);
            //}
        }

        private async void txtBab_LostFocus(object sender, RoutedEventArgs e)
        {
            if (mnuAutoUpdate.IsChecked)
            {
                await UpdateCalculations(false, false, false);
            }
        }

        private void SkillHeaderGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //if (grdSkillHeader.ActualWidth > SkillEditor.WIDE_STATE_THRESHOLD)
            //{
            //    // activate wide state for the header grid
            //    colSkillModifiers.Width = new GridLength(0);
            //    colSkillExtra.Width = new GridLength(3, GridUnitType.Star);
            //    colSkillExtra.MinWidth = 280;
            //}
            //else
            //{
            //    // disable wide state for the header grid
            //    colSkillModifiers.Width = new GridLength(85);
            //    colSkillExtra.Width = new GridLength(0);
            //    colSkillExtra.MinWidth = 0;
            //}
        }

        private void lnkCombat_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe)
            {
                if (fe.Tag is string link)
                {
                    OpenBrowser(link);
                }
                else
                {
                    Debugger.Log(0, null, "Tag is not a link: " + fe.Tag.ToString());
                }
            }
            else
            {
                Debugger.Log(0, null, "Sender is not a FrameworkElement. It is " + sender.GetType().FullName);
            }
        }

        #endregion

        #region Skill editors

        public void LoadSkillModSubstitutions(string s)
        {
            if (string.IsNullOrEmpty(s)) return;

            skillModSubs.Clear();

            string[] pairs = s.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (string item in pairs)
            {
                string[] itemVals = item.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (itemVals.Length != 2)
                {
                    continue;
                }
                else
                {
                    if (!abilityMods.ContainsKey(itemVals[1])) continue;
                    skillModSubs.Add(itemVals[0], itemVals[1]);
                }
            }

            if (skillModSubs.Count > 0)
            {
                foreach (SkillEditor? item in stkSkills.Children)
                {
                    if (item != null)
                    {
                        if (skillModSubs.ContainsKey(item.InternalSkillName))
                        {
                            item.ModifierName = skillModSubs[item.InternalSkillName];
                        }
                    }
                }
            }
        }

        private void editor_ModifierChanged(object? sender, EventArgs e)
        {
            // TODO: handle modifier change
            //throw new NotImplementedException();
            if (sender is SkillEditor se)
            {
                if (se.ModifierName == se.OriginalModifierName)
                {
                    skillModSubs.Remove(se.InternalSkillName);
                }
                else
                {
                    skillModSubs[se.InternalSkillName] = se.ModifierName;
                }
                se.ModifierValue = abilityMods[se.ModifierName];
            }
        }


        #endregion

        #region Feats/Abilities editors

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
            selSpellLikes.Items.Add(se);

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
            selSpellLikes.Items.ClearSelection();
        }

        #endregion

        #region Equipment/combat item list editors

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
            selAcItem.Items.Add(ae);

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
            selAcItem.Items.ClearSelection();
        }


        #endregion

        #region Spell list editors

        private void btnAddSpell_Click(object sender, EventArgs e)
        {
            SpellEditor se = new SpellEditor();
            se.ContentChanged += editor_ContentChanged;
            se.ApplyColorScheme(App.ColorScheme);
            selSpells.Items.Add(se);

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
            selSpells.Items.ClearSelection();
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
            bool allowMarked = true;
            bool allowUnmarked = true;

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

            allowMarked = mnuSpellFilterM.IsChecked;
            allowUnmarked = mnuSpellFilterUM.IsChecked;

            foreach (SpellEditor item in selSpells.Items.OfType<SpellEditor>())
            {
                if (AllowedLevels.Contains(item.Level))
                {
                    if (item.Marked && allowMarked || !item.Marked && allowUnmarked)
                    {
                        item.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        item.Visibility = Visibility.Collapsed;
                        item.IsSelected = false; // don't have hidden items be selected
                    }
                }
                else
                {
                    item.Visibility = Visibility.Collapsed;
                    item.IsSelected = false; // don't have hidden items be selected
                }
            }
        }


        #endregion

        #region Notes tab

        private void chkNotesMarkdown_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (!_isUpdating)
            {
                if (chkNotesMarkdown.IsChecked)
                {
                    ShowMarkdownElements();
                }
                else
                {
                    HideMarkdownElements();
                }

                SetIsDirty();
            }
        }

        private void btnNotesEdit_Click(object sender, RoutedEventArgs e)
        {
            OpenNotesEditTab();
        }

        private void btnNotesView_Click(object sender, RoutedEventArgs e)
        {
            OpenNotesViewTab();
        }

        private void txtNotes_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isUpdating)
            {
                SetIsDirty();
            }

            vwrNotes.Markdown = txtNotes.Text;
            UpdateMarkdownViewerVisuals();

            lastEditedItem = sender as TextBox;
        }

        private void HyperlinkCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string? s = e.Parameter.ToString();

            try
            {
                if (s != null)
                {
                    OpenBrowser(s);
                }
            }
            catch (ArgumentNullException)
            {
                // could not open the link as it is null
                if (Debugger.IsAttached)
                {
                    Debugger.Log(0, "vwrNotes", "Link is null, from Notes viewer.\n");
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                // could not open the link as it doesn't exist
                if (Debugger.IsAttached)
                {
                    Debugger.Log(0, "vwrNotes", "Link \"" + s + "\" does not exist, from Notes viewer.\n");
                }
            }
            catch (InvalidOperationException)
            {
                // could not open the link for some reason
                if (Debugger.IsAttached)
                {
                    Debugger.Log(0, "vwrNotes", "Link \"" + s + "\" was not defined, from Notes viewer.\n");
                }
            }
            catch (Win32Exception)
            {
                // could not open the link for some reason
                if (Debugger.IsAttached)
                {
                    Debugger.Log(0, "vwrNotes", "Link \"" + s + "\" could not be opened, from Notes viewer.\n");
                }
            }
        }

        void UpdateMarkdownViewerVisuals()
        {
            if (vwrNotes.Document != null)
            {
                vwrNotes.Document.PagePadding = new Thickness(2);

                foreach (var item in vwrNotes.Document.Blocks)
                {
                    item.Padding = new Thickness(0);
                    item.Margin = new Thickness(0,1,0,4);
                }
            }
        }

        public void OpenNotesEditTab()
        {
            notesEdit = true;

            vwrNotes.Visibility = Visibility.Collapsed;
            txtNotes.Visibility = Visibility.Visible;

            btnNotesView.IsSelected = false;
            btnNotesEdit.IsSelected = true;

            btnNotesView.BorderThickness = new Thickness(0, 0, 0, 1);
            btnNotesEdit.BorderThickness = new Thickness(1, 1, 1, 0);
        }

        public void OpenNotesViewTab()
        {
            notesEdit = false;

            vwrNotes.Visibility = Visibility.Visible;
            txtNotes.Visibility = Visibility.Collapsed;

            btnNotesView.IsSelected = true;
            btnNotesEdit.IsSelected = false;

            btnNotesView.BorderThickness = new Thickness(1, 1, 1, 0);
            btnNotesEdit.BorderThickness = new Thickness(0, 0, 0, 1);
        }

        public void HideMarkdownElements()
        {
            OpenNotesEditTab();

            btnNotesEdit.Visibility = Visibility.Collapsed;
            btnNotesView.Visibility = Visibility.Collapsed;
            brdrNotesMarkdown.Visibility = Visibility.Collapsed;

            txtNotes.BorderThickness = new Thickness(1, 1, 1, 1);

            if (_sheetLoaded && !_isUpdating)
            {
                sheetSettings["notesMarkdown"] = "disabled";
            }
        }

        public void ShowMarkdownElements()
        {
            btnNotesEdit.Visibility = Visibility.Visible;
            btnNotesView.Visibility = Visibility.Visible;
            brdrNotesMarkdown.Visibility = Visibility.Visible;

            txtNotes.BorderThickness = new Thickness(1, 0, 1, 1);
            
            if (_sheetLoaded && !_isUpdating)
            {
                sheetSettings["notesMarkdown"] = "enabled";
            }
        }

        #endregion

        public void LoadSheetSettings(bool reloadSkills = false)
        {
            if (sheetSettings != null)
            {
                if (HasSheetSettingValue("notesMarkdown", "enabled"))
                {
                    ShowMarkdownElements();
                    OpenNotesViewTab();
                    chkNotesMarkdown.IsChecked = true;
                }
                else
                {
                    HideMarkdownElements();
                    chkNotesMarkdown.IsChecked = false;
                }

                if (HasSheetSettingValue("notesNoSpellCheck", "enabled"))
                {
                    SpellCheck.SetIsEnabled(txtNotes, false);
                }
                else
                {
                    SpellCheck.SetIsEnabled(txtNotes, true);
                }

                if (HasSheetSettingValue("calcIncludeAc", "false"))
                {
                    mnuUpdateAc.IsChecked = false;
                }
                else
                {
                    mnuUpdateAc.IsChecked = true;
                }

                if (HasSheetSettingValue("calcIncludeTotals", "false"))
                {
                    mnuUpdateTotals.IsChecked = false;
                }
                else
                {
                    mnuUpdateTotals.IsChecked = true;
                }

                if (HasSheetSettingValue("calcAutorun", "false"))
                {
                    mnuAutoUpdate.IsChecked = false;
                }
                else
                {
                    mnuAutoUpdate.IsChecked = true;
                }
            }
            else
            {
                HideMarkdownElements();
                chkNotesMarkdown.IsChecked = false;
                SpellCheck.SetIsEnabled(txtNotes, true);

                mnuUpdateAc.IsChecked = true;
                mnuUpdateTotals.IsChecked = true;
                mnuAutoUpdate.IsChecked = true;
            }

            if (reloadSkills)
            {
                PathfinderSheet pf = CreatePathfinderSheet();
                var ses = SkillEditorFactory.CreateEditors(pf, this);

                stkSkills.Children.Clear();
                foreach (SkillEditor item in ses)
                {
                    item.ModifierValue = abilityMods[item.ModifierName];
                    item.UpdateCalculations();

                    item.ContentChanged += editor_ContentChanged;
                    item.ModifierChanged += editor_ModifierChanged;

                    stkSkills.Children.Add(item);
                    item.ColorScheme = ColorScheme;
                    //item.UpdateAppearance();
                }

                if (sheetSettings?.ContainsKey("skillModSet") ?? false)
                {
                    LoadSkillModSubstitutions(sheetSettings["skillModSet"] ?? "");
                }
            }
        }

        /// <summary>
        /// Check if a certain sheet setting value is stored in this sheet's sheet settings.
        /// </summary>
        /// <param name="settingName">The name of the setting value to check.</param>
        /// <param name="checkValue">The value to check. Only returns true if the setting matches this value.</param>
        /// <returns>Returns true only if the setting with the specified name has the specified value. In all other situations, returns false.</returns>
        /// <remarks>To check simply if a setting name exists, use the <c>sheetSettings.ContainsKey()</c> method instead.</remarks>
        public bool HasSheetSettingValue(string settingName, string? checkValue = null)
        {
            return sheetSettings.ContainsKey(settingName) ? sheetSettings[settingName]?.ToLowerInvariant() == checkValue : false;
        }

        private void mnuCounters_Click(object sender, RoutedEventArgs e)
        {
            CountersWindow cw = new CountersWindow();
            cw.Show();
        }

        private void mnuSheetSettings_Click(object sender, RoutedEventArgs e)
        {
            if (!_sheetLoaded)
            {
                MessageDialog md = new MessageDialog(App.ColorScheme);
                md.ShowDialog("No sheet is currently open.", null, this, "No Sheet Open", MessageDialogButtonDisplay.Auto, image: MessageDialogImage.Error);
                return;
            }

            SheetSettings sse = new SheetSettings();
            sse.SheetSettingsList = sheetSettings;
            sse.Owner = this;
            sse.UpdateUi();
            sse.ShowDialog();

            if (sse.DialogResult)
            {
                sheetSettings = sse.SheetSettingsList;
                LoadSheetSettings(true);
                SetIsDirty();
            }
        }

        private void mnuInsertJson_Click(object sender, RoutedEventArgs e)
        {
            if (!_sheetLoaded)
            {
                MessageDialog md = new MessageDialog(App.ColorScheme);
                md.ShowDialog("No sheet is currently open.", null, this, "No Sheet Open", MessageDialogButtonDisplay.Auto, image: MessageDialogImage.Error);
                return;
            }

            if (currentView == RAWJSON_VIEW)
            {
                string jsonText = txtEditRaw.Text;

                try
                {
                    Newtonsoft.Json.Linq.JObject jo = Newtonsoft.Json.Linq.JObject.Parse(jsonText);
                }
                catch (Newtonsoft.Json.JsonReaderException)
                {
                    MessageDialog md = new MessageDialog(App.ColorScheme);
                    md.ShowDialog("The sheet currently does not appear to be valid JSON. Please fix or undo any JSON errors and try again.", null, this, "Invalid Sheet JSON", MessageDialogButtonDisplay.Auto, image: MessageDialogImage.Error);
                    return;
                }
            }

            InsertJson ij = new InsertJson();
            ij.Owner = this;
            ij.ShowDialog();

            if (ij.DialogResult)
            {
                string newJson = ij.InsertedJson;

                // check if the inserted JSON starts with enclosing braces (to build the root JSON element). if it does not, add it in
                // in the future, I may go back and make this disable-able via an advanced setting in case this ends up messing up something for someone
                if (!newJson.StartsWith("{") && !newJson.EndsWith("}"))
                {
                    newJson = "{" + newJson + "}";
                }

                if (currentView == RAWJSON_VIEW)
                {
                    string jsonText = txtEditRaw.Text;

                    Newtonsoft.Json.Linq.JObject jo = Newtonsoft.Json.Linq.JObject.Parse(jsonText);
                    Newtonsoft.Json.Linq.JObject jn;

                    try
                    {
                        jn = Newtonsoft.Json.Linq.JObject.Parse(newJson);
                    }
                    catch (Newtonsoft.Json.JsonReaderException)
                    {
                        MessageDialog md = new MessageDialog(App.ColorScheme);
                        md.ShowDialog("The JSON to be inserted does not appear to be valid. No merging will occur.", null, this, "Invalid JSON to Insert", MessageDialogButtonDisplay.Auto, image: MessageDialogImage.Error);
                        return;
                    }

                    jo.Merge(jn, new Newtonsoft.Json.Linq.JsonMergeSettings()
                    {
                        PropertyNameComparison = StringComparison.InvariantCulture,
                        MergeArrayHandling = Newtonsoft.Json.Linq.MergeArrayHandling.Merge,
                        MergeNullValueHandling = Newtonsoft.Json.Linq.MergeNullValueHandling.Merge
                    });

                    txtEditRaw.Text = jo.ToString();
                }
                else
                {
                    Newtonsoft.Json.Linq.JObject jo = Newtonsoft.Json.Linq.JObject.Parse(CreatePathfinderSheet().SaveJsonText(false, "", false));
                    Newtonsoft.Json.Linq.JObject jn;

                    try
                    {
                        jn = Newtonsoft.Json.Linq.JObject.Parse(newJson);
                    }
                    catch (Newtonsoft.Json.JsonReaderException)
                    {
                        MessageDialog md = new MessageDialog(App.ColorScheme);
                        md.ShowDialog("The JSON to be inserted does not appear to be valid. No merging will occur.", null, this, "Invalid JSON to Insert", MessageDialogButtonDisplay.Auto, image: MessageDialogImage.Error);
                        return;
                    }

                    jo.Merge(jn, new Newtonsoft.Json.Linq.JsonMergeSettings()
                    {
                        PropertyNameComparison = StringComparison.InvariantCulture,
                        MergeArrayHandling = Newtonsoft.Json.Linq.MergeArrayHandling.Merge,
                        MergeNullValueHandling = Newtonsoft.Json.Linq.MergeNullValueHandling.Merge
                    });

                    LoadPathfinderSheet(PathfinderSheet.LoadJsonText(jo.ToString()));
                }
            }
        }

        #region Ability score editors / views

        #region Temporary editors
        private void btnTempStr_Click(object sender, RoutedEventArgs e)
        {
            grdTempStr.Visibility = Visibility.Visible;
            btnTempStr.Visibility = Visibility.Collapsed;
            SetIsDirty();
        }

        private void btnTempDex_Click(object sender, RoutedEventArgs e)
        {
            grdTempDex.Visibility = Visibility.Visible;
            btnTempDex.Visibility = Visibility.Collapsed;
            SetIsDirty();
        }

        private void btnTempCon_Click(object sender, RoutedEventArgs e)
        {
            grdTempCon.Visibility = Visibility.Visible;
            btnTempCon.Visibility = Visibility.Collapsed;
            SetIsDirty();
        }

        private void btnTempInt_Click(object sender, RoutedEventArgs e)
        {
            grdTempInt.Visibility = Visibility.Visible;
            btnTempInt.Visibility = Visibility.Collapsed;
            SetIsDirty();
        }

        private void btnTempWis_Click(object sender, RoutedEventArgs e)
        {
            grdTempWis.Visibility = Visibility.Visible;
            btnTempWis.Visibility = Visibility.Collapsed;
            SetIsDirty();
        }

        private void btnTempCha_Click(object sender, RoutedEventArgs e)
        {
            grdTempCha.Visibility = Visibility.Visible;
            btnTempCha.Visibility = Visibility.Collapsed;
            SetIsDirty();
        }

        private void grdTempStr_CloseRequested(object sender, EventArgs e)
        {
            grdTempStr.Visibility = Visibility.Collapsed;
            btnTempStr.Visibility = Visibility.Visible;
            stkTempStr.Visibility = Visibility.Collapsed;
            eleStr.ShowTempButton();
            SetIsDirty();
        }

        private void grdTempDex_CloseRequested(object sender, EventArgs e)
        {
            grdTempDex.Visibility = Visibility.Collapsed;
            btnTempDex.Visibility = Visibility.Visible;
            stkTempDex.Visibility = Visibility.Collapsed;
            eleDex.ShowTempButton();
            SetIsDirty();
        }

        private void grdTempCon_CloseRequested(object sender, EventArgs e)
        {
            grdTempCon.Visibility = Visibility.Collapsed;
            btnTempCon.Visibility = Visibility.Visible;
            stkTempCon.Visibility = Visibility.Collapsed;
            eleCon.ShowTempButton();
            SetIsDirty();
        }

        private void grdTempInt_CloseRequested(object sender, EventArgs e)
        {
            grdTempInt.Visibility = Visibility.Collapsed;
            btnTempInt.Visibility = Visibility.Visible;
            stkTempInt.Visibility = Visibility.Collapsed;
            eleInt.ShowTempButton();
            SetIsDirty();
        }

        private void grdTempWis_CloseRequested(object sender, EventArgs e)
        {
            grdTempWis.Visibility = Visibility.Collapsed;
            btnTempWis.Visibility = Visibility.Visible;
            stkTempWis.Visibility = Visibility.Collapsed;
            eleWis.ShowTempButton();
            SetIsDirty();
        }

        private void grdTempCha_CloseRequested(object sender, EventArgs e)
        {
            grdTempCha.Visibility = Visibility.Collapsed;
            btnTempCha.Visibility = Visibility.Visible;
            stkTempCha.Visibility = Visibility.Collapsed;
            eleCha.ShowTempButton();
            SetIsDirty();
        }

        private void eleStr_RequestTempEditorDisplay(object sender, EventArgs e)
        {
            stkTempStr.Visibility = Visibility.Visible;
            SetIsDirty();
        }

        private void eleDex_RequestTempEditorDisplay(object sender, EventArgs e)
        {
            stkTempDex.Visibility = Visibility.Visible;
            SetIsDirty();
        }

        private void eleCon_RequestTempEditorDisplay(object sender, EventArgs e)
        {
            stkTempCon.Visibility = Visibility.Visible;
            SetIsDirty();
        }

        private void eleInt_RequestTempEditorDisplay(object sender, EventArgs e)
        {
            stkTempInt.Visibility = Visibility.Visible;
            SetIsDirty();
        }

        private void eleWis_RequestTempEditorDisplay(object sender, EventArgs e)
        {
            stkTempWis.Visibility = Visibility.Visible;
            SetIsDirty();
        }

        private void eleCha_RequestTempEditorDisplay(object sender, EventArgs e)
        {
            stkTempCha.Visibility = Visibility.Visible;
            SetIsDirty();
        }

        #endregion

        private void btnAbilitiesInfo_Click(object sender, RoutedEventArgs e)
        {
            OpenBrowser("https://www.d20pfsrd.com/basics-ability-scores/ability-scores/");
        }

        void ApplyIconValuesToTable()
        {
            txtWis.Value = eleWis.Value;
            txtCha.Value = eleCha.Value;
            txtInt.Value = eleInt.Value;
            txtStr.Value = eleStr.Value;
            txtCon.Value = eleCon.Value;
            txtDex.Value = eleDex.Value;

            // TODO: add in temp values if the temp editors in icon view are present
        }

        void SetAbilityScoreView(int view, bool init = false)
        {
            bool updOld = _isUpdating;
            switch (view)
            {
                case ABILITY_ICON_VIEW:
                    // convert from table view to icon view
                    mnuAbilityView2.IsChecked = true;
                    mnuAbilityView1.IsChecked = false;
                    mnuAbilityView.Content = "Icon view";

                    grdAbilityList.Visibility = Visibility.Collapsed;
                    grdAbilityIcon.Visibility = Visibility.Visible;

                    if (!init)
                    {
                        _isUpdating = true;
                        eleWis.Value = txtWis.Value;
                        eleCha.Value = txtCha.Value;
                        eleInt.Value = txtInt.Value;
                        eleStr.Value = txtStr.Value;
                        eleCon.Value = txtCon.Value;
                        eleDex.Value = txtDex.Value;

                        if (grdTempCha.Visibility == Visibility.Visible) { stkTempCha.Visibility = Visibility.Visible; eleCha.HideTempButton(); } else { eleCha.ShowTempButton(); }
                        if (grdTempCon.Visibility == Visibility.Visible) { stkTempCon.Visibility = Visibility.Visible; eleCon.HideTempButton(); } else { eleCon.ShowTempButton(); }
                        if (grdTempDex.Visibility == Visibility.Visible) { stkTempDex.Visibility = Visibility.Visible; eleDex.HideTempButton(); } else { eleDex.ShowTempButton(); }
                        if (grdTempInt.Visibility == Visibility.Visible) { stkTempInt.Visibility = Visibility.Visible; eleInt.HideTempButton(); } else { eleInt.ShowTempButton(); }
                        if (grdTempStr.Visibility == Visibility.Visible) { stkTempStr.Visibility = Visibility.Visible; eleStr.HideTempButton(); } else { eleStr.ShowTempButton(); }
                        if (grdTempWis.Visibility == Visibility.Visible) { stkTempWis.Visibility = Visibility.Visible; eleWis.HideTempButton(); } else { eleWis.ShowTempButton(); }

                        eleTempCha.Value = grdTempCha.Value;
                        eleTempCon.Value = grdTempCon.Value;
                        eleTempDex.Value = grdTempDex.Value;
                        eleTempInt.Value = grdTempInt.Value;
                        eleTempStr.Value = grdTempStr.Value;
                        eleTempWis.Value = grdTempWis.Value;

                        _isUpdating = updOld;
                    }

                    break;
                case ABILITY_TABLE_VIEW:
                    // convert from icon view to table view
                    mnuAbilityView2.IsChecked = false;
                    mnuAbilityView1.IsChecked = true;
                    mnuAbilityView.Content = "Table view";

                    grdAbilityList.Visibility = Visibility.Visible;
                    grdAbilityIcon.Visibility = Visibility.Collapsed;

                    if (!init)
                    {
                        _isUpdating = true;
                        txtWis.Value = eleWis.Value;
                        txtCha.Value = eleCha.Value;
                        txtInt.Value = eleInt.Value;
                        txtStr.Value = eleStr.Value;
                        txtCon.Value = eleCon.Value;
                        txtDex.Value = eleDex.Value;

                        if (stkTempCha.Visibility == Visibility.Visible) grdTempCha.Visibility = Visibility.Visible;
                        if (stkTempCon.Visibility == Visibility.Visible) grdTempCon.Visibility = Visibility.Visible;
                        if (stkTempDex.Visibility == Visibility.Visible) grdTempDex.Visibility = Visibility.Visible;
                        if (stkTempInt.Visibility == Visibility.Visible) grdTempInt.Visibility = Visibility.Visible;
                        if (stkTempStr.Visibility == Visibility.Visible) grdTempStr.Visibility = Visibility.Visible;
                        if (stkTempWis.Visibility == Visibility.Visible) grdTempWis.Visibility = Visibility.Visible;

                        grdTempCha.Value = eleTempCha.Value;
                        grdTempCon.Value = eleTempCon.Value;
                        grdTempDex.Value = eleTempDex.Value;
                        grdTempInt.Value = eleTempInt.Value;
                        grdTempStr.Value = eleTempStr.Value;
                        grdTempWis.Value = eleTempWis.Value;

                        _isUpdating = updOld;
                    }
                    break;
                default:
                    break;
            }
        }

        private void mnuAbilityView1_Click(object sender, RoutedEventArgs e)
        {
            SetAbilityScoreView(ABILITY_TABLE_VIEW);
        }

        private void mnuAbilityView2_Click(object sender, RoutedEventArgs e)
        {
            SetAbilityScoreView(ABILITY_ICON_VIEW);
        }

        #endregion

    }
}
