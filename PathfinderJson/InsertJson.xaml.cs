using System;
using System.Collections.Generic;
using System.Windows;
using SolidShineUi;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using System.Windows.Media;
using System.Windows.Threading;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for InsertJson.xaml
    /// </summary>
    public partial class InsertJson : FlatWindow
    {
        /// <summary>The search panel associated with the raw JSON editor</summary>
        SearchPanel.SearchPanel sp;

        public const string NO_HIGH_CONTRAST = "0";

        public string InsertedJson { get; set; } = "";

        public new bool DialogResult { get; set; } = false;

        public InsertJson()
        {
            InitializeComponent();
            ColorScheme = App.ColorScheme;

            // setup up raw JSON editor
            if (App.Settings.EditorSyntaxHighlighting && App.Settings.HighContrastTheme == NO_HIGH_CONTRAST)
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

            SearchPanel.SearchPanel p = SearchPanel.SearchPanel.Install(txtEditRaw);
            p.FontFamily = SystemFonts.MessageFontFamily; // so it isn't a fixed-width font lol
            sp = p;
            sp.ColorScheme = App.ColorScheme;

            txtEditRaw.Encoding = new System.Text.UTF8Encoding(false);
        }

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

        private void mnuFind_Click(object sender, RoutedEventArgs e)
        {
            sp.Open();
            if (!(txtEditRaw.TextArea.Selection.IsEmpty || txtEditRaw.TextArea.Selection.IsMultiline))
                sp.SearchPattern = txtEditRaw.TextArea.Selection.GetText();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Input, (Action)delegate { sp.Reactivate(); });
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnInsert_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            InsertedJson = txtEditRaw.Text;
            Close();
        }
    }
}
