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
using UiCore;

using static PathfinderJson.CoreUtils;
using static PathfinderJson.App;

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
        SearchPanel sp;
        /// <summary>Get or set if the sheet view is currently running calculations</summary>
        bool _isCalculating = false;

        // functions for handling undo/redo
        // these aren't actually used for anything at the current time as I've not properly introduced undo/redo yet
        private const int undoLimit = 20;
        Stack<PathfinderSheet> undoItems = new Stack<PathfinderSheet>(undoLimit);
        Stack<PathfinderSheet> redoItems = new Stack<PathfinderSheet>(undoLimit);
        TextBox? lastEditedBox = null;
        DispatcherTimer undoSetTimer = new DispatcherTimer();

        // these are stored here as the program doesn't display these values to the user directly
        UserData ud;
        string sheetid;
        Dictionary<string, string> abilities = new Dictionary<string, string>();

        #region Constructor/ window events/ basic functions

        public MainWindow()
        {
            ud = new UserData(false);
            sheetid = "-1";

            undoSetTimer.Interval = new TimeSpan(0, 0, 3);
            undoSetTimer.Tick += UndoSetTimer_Tick;

            InitializeComponent();
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
                        break;
                    case "2":
                        App.ColorScheme = ColorScheme.GetHighContrastScheme(HighContrastOption.GreenOnBlack);
                        break;
                    case "3":
                        App.ColorScheme = ColorScheme.GetHighContrastScheme(HighContrastOption.BlackOnWhite);
                        break;
                    default:
                        App.Settings.HighContrastTheme = NO_HIGH_CONTRAST;
                        App.ColorScheme = new ColorScheme(ColorsHelper.CreateFromHex(App.Settings.ThemeColor));
                        SaveSettings();
                        break;
                }
            }
            UpdateAppearance();

            SetupTabs();
            selTabs.IsEnabled = false;
            //LoadGeneralTab();

            ChangeView(App.Settings.StartView, false, true, false);

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

            // setup up raw JSON editor
            using (Stream? s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("PathfinderJson.Json.xshd"))
            {
                if (s != null)
                {
                    using XmlReader reader = new XmlTextReader(s);
                    txtEditRaw.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }

            SearchPanel p = SearchPanel.Install(txtEditRaw);
            p.FontFamily = SystemFonts.MessageFontFamily; // so it isn't a fixed-width font lol
            sp = p;

            txtEditRaw.Encoding = new System.Text.UTF8Encoding(false);

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

        #region Other Base Functions

        /// <summary>
        /// Open a file into the editor. This is intended for when opening files from the command-line arguments.
        /// </summary>
        /// <param name="filename">The path to the file to open.</param>
        public void OpenFile(string filename)
        {
            LoadFile(filename, true);
        }

        void SaveSettings()
        {
            App.Settings.Save(Path.Combine(appDataPath, "settings.json"));
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
                        Title = "Pathfinder JSON - New File";
                        displayedTitle = "New File";
                    }
                    else
                    {
                        Title = "Pathfinder JSON - " + Path.GetFileName(filePath);
                        displayedTitle = Path.GetFileName(filePath);
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(fileTitle))
                    {
                        Title = "Pathfinder JSON - (unnamed character)";
                        displayedTitle = fileTitle;
                    }
                    else
                    {
                        Title = "Pathfinder JSON - " + fileTitle;
                        displayedTitle = fileTitle;
                    }
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

            if (update || fileTitle != displayedTitle) UpdateTitlebar();
        }

        #endregion

        #endregion

        #region File / Help menus

        private void mnuNew_Click(object sender, RoutedEventArgs e)
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
            }
        }

        private void mnuNewWindow_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Process.GetCurrentProcess().MainModule?.FileName);
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
                md.ShowDialog("The file has some unsaved changes. Do you want to save them first?", null, this, "Unsaved Changes", MessageDialogButtonDisplay.Three, MessageDialogImage.Question, MessageDialogResult.Cancel,
                    "Save", "Cancel", "Discard");

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
                md.ShowDialog("The file has some unsaved changes. Are you sure you want to discard them?", App.ColorScheme, this, "Unsaved Changes", MessageDialogButtonDisplay.Auto, image: MessageDialogImage.Question,
                    customOkButtonText: "Discard", customCancelButtonText: "Cancel");

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

        private void mnuGithub_Click(object sender, RoutedEventArgs e)
        {
            OpenBrowser("https://github.com/JaykeBird/PathfinderJson/");
        }

        private void mnuFeedback_Click(object sender, RoutedEventArgs e)
        {
            OpenBrowser("https://github.com/JaykeBird/PathfinderJson/issues/new/choose");
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
                    md.ShowDialog("There are no updates available. You're on the latest release!", App.ColorScheme, this, "Check for Updates", MessageDialogButtonDisplay.Auto, image: MessageDialogImage.Hand);
                }
            }
            catch (System.Net.WebException)
            {
                MessageDialog md = new MessageDialog(App.ColorScheme);
                md.ShowDialog("Could not check for updates. Make sure you're connected to the Internet.", App.ColorScheme, this, "Check for Updates", MessageDialogButtonDisplay.Auto, image: MessageDialogImage.Error);
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
            ToolTip tt = new ToolTip
            {
                Content = filename,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Right,
                PlacementTarget = mi
            };
            mi.ToolTip = tt;
            mi.Tag = filename;
            mi.Click += miRecentFile_Click;
            mi.ContextMenuOpening += miRecentContext_Opening;
            mnuRecent.Items.Insert(0, mi);

            UiCore.ContextMenu cm = new UiCore.ContextMenu();
            cm.PlacementTarget = mi;
            cm.Width = 180;

            MenuItem cm1 = new MenuItem();
            cm1.Header = "Open";
            cm1.Tag = mi;
            cm1.Click += miRecentOpen_Click;
            cm.Items.Add(cm1);

            MenuItem cm4 = new MenuItem();
            cm4.Header = "Open in New Window";
            cm4.Tag = mi;
            cm4.Click += miRecentOpenNew_Click;
            cm.Items.Add(cm4);

            MenuItem cm5 = new MenuItem();
            cm5.Header = "Copy Path";
            cm5.Tag = mi;
            cm5.Click += miRecentCopy_Click;
            cm.Items.Add(cm5);

            MenuItem cm2 = new MenuItem();
            cm2.Header = "View in Explorer";
            cm2.Tag = mi;
            cm2.Click += miRecentView_Click;
            cm.Items.Add(cm2);

            MenuItem cm3 = new MenuItem();
            cm3.Header = "Remove";
            cm3.Tag = mi;
            cm3.Click += miRecentRemove_Click;
            cm.Items.Add(cm3);

            mi.ContextMenu = cm;

            if (storeInSettings)
            {
                App.Settings.RecentFiles.Add(filename);
                SaveSettings();
            }

            mnuRecentEmpty.Visibility = Visibility.Collapsed;
        }

        private void miRecentContext_Opening(object sender, ContextMenuEventArgs e)
        {
            if (sender is MenuItem m)
            {
                if (m.ContextMenu is UiCore.ContextMenu cm)
                {
                    cm.ApplyColorScheme(App.ColorScheme);
                }
            }
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
                        Process.Start(Process.GetCurrentProcess().MainModule?.FileName, "\"" + file + "\"");
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

        private void UndoSetTimer_Tick(object? sender, EventArgs e)
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

        #region Tab bar / visuals / appearance

        void UpdateAppearance()
        {
            ApplyColorScheme(App.ColorScheme);
            menu.ApplyColorScheme(App.ColorScheme);
            toolbar.Background = App.ColorScheme.MainColor.ToBrush();

            if (App.ColorScheme.IsHighContrast)
            {
                menu.Background = App.ColorScheme.BackgroundColor.ToBrush();
                toolbar.Background = App.ColorScheme.BackgroundColor.ToBrush();
            }

            selTabs.ApplyColorScheme(App.ColorScheme);

            // quick fix until I make a better system post-1.0
            foreach (SelectableItem item in selTabs.GetItemsAsType<SelectableItem>())
            {
                item.Foreground = App.ColorScheme.ForegroundColor.ToBrush();
            }

            brdrCalculating.Background = App.ColorScheme.SecondaryColor.ToBrush();
            brdrCalculating.BorderBrush = App.ColorScheme.HighlightColor.ToBrush();

            (txtEditRaw.ContextMenu as UiCore.ContextMenu)!.ApplyColorScheme(App.ColorScheme);

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

            edtAc.UpdateAppearance();

            foreach (SkillEditor? item in stkSkills.Children)
            {
                if (item == null) continue;
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
            if (App.Settings.HighContrastTheme != NO_HIGH_CONTRAST)
            {
                MessageDialog md = new MessageDialog(App.ColorScheme);
                if (md.ShowDialog("A high-contrast theme is currently being used. Changing the color scheme will turn off the high-contrast theme. Do you want to continue?", null, this, "High Contrast Theme In Use", MessageDialogButtonDisplay.Two,
                    MessageDialogImage.Warning, MessageDialogResult.Cancel, "Continue", "Cancel") == MessageDialogResult.Cancel)
                {
                    return;
                }
            }

            ColorPickerDialog cpd = new ColorPickerDialog(App.ColorScheme, App.ColorScheme.MainColor);
            cpd.Owner = this;
            cpd.ShowDialog();

            if (cpd.DialogResult)
            {
                App.ColorScheme = new ColorScheme(cpd.SelectedColor);
                App.Settings.ThemeColor = cpd.SelectedColor.GetHexString();
                App.Settings.HighContrastTheme = NO_HIGH_CONTRAST;
                SaveSettings();
                UpdateAppearance();
            }
        }

        private void mnuHighContrast_Click(object sender, RoutedEventArgs e)
        {
            MessageDialog md = new MessageDialog(App.ColorScheme)
            {
                ExtraButton1Text = "Use White on Black",
                ExtraButton2Text = "Use Green on Black",
                ExtraButton3Text = "Use Black on White",
                OkButtonText = "Don't use",
                Message = "A high contrast theme is good for users who have vision-impairment or other issues. PathfinderJSON comes with 3 high-contrast options available.",
                Title = "High Contrast Theme"
            };

            md.ShowDialog();

            switch (md.DialogResult)
            {
                case MessageDialogResult.OK:
                    App.Settings.HighContrastTheme = NO_HIGH_CONTRAST;
                    break;
                case MessageDialogResult.Cancel:
                    App.Settings.HighContrastTheme = NO_HIGH_CONTRAST;
                    break;
                case MessageDialogResult.Extra1:
                    App.Settings.HighContrastTheme = "1";
                    break;
                case MessageDialogResult.Extra2:
                    App.Settings.HighContrastTheme = "2";
                    break;
                case MessageDialogResult.Extra3:
                    App.Settings.HighContrastTheme = "3";
                    break;
                default:
                    break;
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
                        break;
                    case "2":
                        App.ColorScheme = ColorScheme.GetHighContrastScheme(HighContrastOption.GreenOnBlack);
                        break;
                    case "3":
                        App.ColorScheme = ColorScheme.GetHighContrastScheme(HighContrastOption.BlackOnWhite);
                        break;
                    default:
                        App.Settings.HighContrastTheme = NO_HIGH_CONTRAST;
                        App.ColorScheme = new ColorScheme(ColorsHelper.CreateFromHex(App.Settings.ThemeColor));
                        break;
                }
            }

            SaveSettings();
            UpdateAppearance();
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
                    colTabs.Width = new GridLength(120);

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
                    colTabs.Width = new GridLength(120);

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
                    colTabs.Width = new GridLength(0);

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
                rowToolbar.Height = new GridLength(28);
                toolbar.IsEnabled = true;
                mnuToolbar.IsChecked = true;
            }
            else
            {
                rowToolbar.Height = new GridLength(0);
                toolbar.IsEnabled = false;
                mnuToolbar.IsChecked = false;
            }
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
                Owner = this,
            };

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
            // set this flag so that the program doesn't try to set the sheet as dirty while loading in the file
            _isUpdating = true;

            // check if the userdata structure is present
            bool _userDataCheck = false;

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
                }
                catch (IndexOutOfRangeException) { }
                catch (NullReferenceException) { }
                catch (ArgumentOutOfRangeException) { }
                catch (System.Net.WebException) { }

                _userDataCheck = false;
            }
            else
            {
                _userDataCheck = true;
                sheet.Player = new UserData(true);
            }

            ud = sheet.Player;
            //ac = sheet.AC;
            sheetid = sheet.Id ?? "-1";

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

            Dictionary<string, string?> xp = sheet.Xp ?? new Dictionary<string, string?>();
            txtXpNl.Text = xp.ContainsKey("toNextLevel") ? xp["toNextLevel"] : "";
            txtXpTotal.Text = xp.ContainsKey("total") ? xp["total"] : "";

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

            Dictionary<string, string?> money = sheet.Money ?? new Dictionary<string, string?>();
            if (sheet.Money == null) // in sheets where the player hasn't given their character money, Mottokrosh's site doesn't add a "money" object to the JSON output
            {
                money = new Dictionary<string, string?>();
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
            // (update: it didn't really do anything lol)
            //var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            //var ses = await Task<List<SkillEditor>>.Factory.StartNew(() => SkillEditorFactory.CreateEditors(sheet, this), cts.Token, TaskCreationOptions.None, scheduler);

            var ses = SkillEditorFactory.CreateEditors(sheet, this);

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
                md.Message = "This JSON file doesn't seem to look like a character sheet file; it may have been that certain data was deleted, but this file could also not be a character sheet file at all. " +
                    "It may be good to open the Raw JSON view to check that the file matches what you're expecting.\n\n" +
                    "PathfinderJSON will continue, but if you save any changes, any non-character sheet data may be deleted.";
                md.Title = "File Check Warning";
                md.Image = MessageDialogImage.Hand;
                md.ShowDialog();
            }
        }
        #endregion

        #region Sync Editors / update sheet / CreatePathfinderSheetAsync

        private async void mnuUpdate_Click(object sender, RoutedEventArgs e)
        {
            await UpdateCalculations(true, mnuUpdateTotals.IsChecked, mnuUpdateAc.IsChecked);
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
            edtCmb.UpdateCoreModifier(txtStrm.Text, "", txtBab.Text);
            edtCmd.UpdateCoreModifier(txtStrm.Text, txtDexm.Text, txtBab.Text);

            if (skills)
            {
                foreach (SkillEditor? item in stkSkills.Children)
                {
                    if (item == null)
                    {
                        continue;
                    }

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

                    if (totals)
                    {
                        await item.UpdateTotals(cts.Token);
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

                foreach (AcItemEditor acItem in selAcItem.GetItemsAsType<AcItemEditor>())
                {
                    AcItem ai = acItem.GetAcItem();
                    if ((ai.Name ?? "").ToLowerInvariant().Contains("shield") || (ai.Type ?? "").ToLowerInvariant().Contains("shield"))
                    {
                        // this is a shield
                        try { acShield += ParseStringAsInt(ai.Bonus); } catch (FormatException) { }
                    }
                    else
                    {
                        // probably not a shield? consider it armor
                        try { acArmor += ParseStringAsInt(ai.Bonus); } catch (FormatException) { }
                    }

                    try { tBonus += ParseStringAsInt(ai.Bonus); } catch (FormatException) { }
                    try { tSpellcheck += ParseStringAsInt((ai.SpellFailure ?? "").Replace("%", "")); } catch (FormatException) { }
                    try { tPenalty += ParseStringAsInt(ai.ArmorCheckPenalty); } catch (FormatException) { }
                    try { tWeight += ParseStringAsInt(ai.Weight); } catch (FormatException) { }
                }

                txtAcBonus.Text = tBonus.ToString();
                txtAcPenalty.Text = tPenalty.ToString();
                txtAcSpellFailure.Text = tSpellcheck.ToString() + "%";
                txtAcWeight.Text = tWeight.ToString();

                edtAc.UpdateAcItemBonuses(acShield.ToString(), acArmor.ToString());
            }

            if (totals)
            {
                await edtFort.UpdateTotal(cts.Token);
                await edtReflex.UpdateTotal(cts.Token);
                await edtWill.UpdateTotal(cts.Token);

                edtAc.UpdateTotal();
                await edtInit.UpdateTotal(cts.Token);
                await edtCmb.UpdateTotal(cts.Token);
                await edtCmd.UpdateTotal(cts.Token);
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

            sheet.Notes = txtNotes.Text;

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

            Dictionary<string, CompoundModifier> saves = new Dictionary<string, CompoundModifier>
            {
                { "fort", edtFort.GetModifier() },
                { "reflex", edtReflex.GetModifier() },
                { "will", edtWill.GetModifier() }
            };
            sheet.Saves = saves;

            sheet.HP = new HP();
            sheet.HP.Total = GetStringOrNull(txtHpTotal.Text, true);
            sheet.HP.Wounds = GetStringOrNull(txtHpWounds.Text, true);
            sheet.HP.NonLethal = GetStringOrNull(txtHpNl.Text, true);

            Dictionary<string, string?> xp = new Dictionary<string, string?>
            {
                { "total", GetStringOrNull(txtXpTotal.Text, true) },
                { "toNextLevel", GetStringOrNull(txtXpNl.Text, true) }
            };
            sheet.Xp = xp;

            sheet.Languages = txtLanguages.Text;

            // ArmorClass saving
            ArmorClass ac = edtAc.GetArmorClass();

            foreach (AcItemEditor itEd in selAcItem.GetItemsAsType<AcItemEditor>())
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
            sheet.Money = new Dictionary<string, string?>
            {
                { "cp", GetStringOrNull(txtMoneyCp.Text, true) },
                { "sp", GetStringOrNull(txtMoneySp.Text, true) },
                { "gp", GetStringOrNull(txtMoneyGp.Text, true) },
                { "pp", GetStringOrNull(txtMoneyPp.Text, true) },
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

            sheet.Equipment = new List<Equipment>();
            foreach (ItemEditor item in selEquipment.GetItemsAsType<ItemEditor>())
            {
                sheet.Equipment.Add(item.GetEquipment());
            }

            // skills
            Dictionary<string, Skill> skills = new Dictionary<string, Skill>();
            foreach (SkillEditor? item in stkSkills.Children)
            {
                if (item == null) continue;
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

                sl.TotalKnown = GetStringOrNull(((TextBox)grdSpells.FindName("txtSpellsKnown" + i)).Text, true);
                sl.SaveDC = GetStringOrNull(((TextBox)grdSpells.FindName("txtSpellsDC" + i)).Text, true);
                sl.TotalPerDay = GetStringOrNull(((TextBox)grdSpells.FindName("txtSpellsPerDay" + i)).Text, true);
                sl.BonusSpells = GetStringOrNull(((TextBox)grdSpells.FindName("txtSpellsBonus" + i)).Text, true);

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
            }

            lastEditedBox = sender as TextBox;
        }

        private void editor_ContentChanged(object? sender, EventArgs e)
        {
            if (!_isUpdating)
            {
                SetIsDirty();
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
                }
                txtPlayerEmail.Text = email;

                SetIsDirty();
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
            if (grdSkillHeader.ActualWidth > SkillEditor.WIDE_STATE_THRESHOLD)
            {
                // activate wide state for the header grid
                colSkillModifiers.Width = new GridLength(0);
                colSkillExtra.Width = new GridLength(3, GridUnitType.Star);
                colSkillExtra.MinWidth = 280;
            }
            else
            {
                // disable wide state for the header grid
                colSkillModifiers.Width = new GridLength(85);
                colSkillExtra.Width = new GridLength(0);
                colSkillExtra.MinWidth = 0;
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
