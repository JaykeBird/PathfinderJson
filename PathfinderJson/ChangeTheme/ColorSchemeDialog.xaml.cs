using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SolidShineUi;

namespace PathfinderJson.ChangeTheme
{
    /// <summary>
    /// Interaction logic for ColorSchemeDialog.xaml
    /// </summary>
    public partial class ColorSchemeDialog : FlatWindow
    {
        public ColorSchemeDialog()
        {
            InitializeComponent();
            LoadColorSchemes();
        }

        #region Variables / Properties
        ColorScheme customTheme = new ColorScheme(Colors.Peru);

        public ColorScheme SelectedColorScheme { get; private set; } = new ColorScheme(Colors.Peru);

        private List<ColorSchemeButton> ColorSchemeButtons = new List<ColorSchemeButton>();

        public new bool DialogResult { get; set; } = false;
        #endregion

        void LoadColorSchemes()
        {
            // create array
            ColorSchemeButtons = new List<ColorSchemeButton>()
            {
                btnPreset1, btnPreset2, btnPreset3, btnPreset4, btnPreset5, btnPreset6, btnPreset7,
                btnPreset8, btnPreset9, btnPreset10, btnPreset11, btnPreset12, btnPreset13, btnPreset14,
                btnCustomColorTheme, btnLightTheme, btnDarkTheme, btnHc1, btnHc2, btnHc3, btnCustomTheme
            };

            foreach (ColorSchemeButton item in ColorSchemeButtons)
            {
                item.Click += (s, e) => { ColorSchemeSelected(item, item.DisplayedColorScheme); };
            }

            btnPreset1.DisplayedColorScheme = new ColorScheme(Colors.Peru);
            btnPreset2.DisplayedColorScheme = new ColorScheme(Colors.CornflowerBlue);
            btnPreset3.DisplayedColorScheme = new ColorScheme(Colors.Tomato);
            btnPreset4.DisplayedColorScheme = new ColorScheme(Colors.LightGreen);
            btnPreset5.DisplayedColorScheme = new ColorScheme(Colors.Goldenrod);
            btnPreset6.DisplayedColorScheme = new ColorScheme(Colors.Orange);
            btnPreset7.DisplayedColorScheme = new ColorScheme(Colors.Coral);
            btnPreset8.DisplayedColorScheme = new ColorScheme(Colors.Violet);
            btnPreset9.DisplayedColorScheme = new ColorScheme(Colors.LimeGreen);
            btnPreset10.DisplayedColorScheme = new ColorScheme(Colors.Chocolate);
            btnPreset11.DisplayedColorScheme = new ColorScheme(Colors.MediumSlateBlue);
            btnPreset12.DisplayedColorScheme = new ColorScheme(Colors.ForestGreen);
            btnPreset13.DisplayedColorScheme = new ColorScheme(Colors.DeepSkyBlue);
            btnPreset14.DisplayedColorScheme = new ColorScheme(Colors.LightSteelBlue);

            btnCustomColorTheme.DisplayedColorScheme = new ColorScheme(btnCustomColored.DisplayedColor);

            btnLightTheme.DisplayedColorScheme = ColorScheme.CreateLightTheme();
            btnDarkTheme.DisplayedColorScheme = ColorScheme.CreateDarkTheme();

            btnHc1.DisplayedColorScheme = ColorScheme.GetHighContrastScheme(HighContrastOption.WhiteOnBlack);
            btnHc2.DisplayedColorScheme = ColorScheme.GetHighContrastScheme(HighContrastOption.GreenOnBlack);
            btnHc3.DisplayedColorScheme = ColorScheme.GetHighContrastScheme(HighContrastOption.BlackOnWhite);

            btnCustomTheme.DisplayedColorScheme = customTheme;
        }

        #region Event Handlers
        void ColorSchemeSelected(ColorSchemeButton btn, ColorScheme cs)
        {
            foreach (ColorSchemeButton item in ColorSchemeButtons)
            {
                item.IsSelected = false;
            }

            btn.IsSelected = true;
            SelectedColorScheme = cs;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        #endregion

        private void btnCustomColored_Click(object sender, EventArgs e)
        {
            ColorPickerDialog cpd = new ColorPickerDialog(ColorScheme, btnCustomColored.DisplayedColor);
            cpd.ShowDialog();

            if (cpd.DialogResult)
            {
                btnCustomColored.DisplayedColor = cpd.SelectedColor;
                btnCustomColorTheme.DisplayedColorScheme = new ColorScheme(cpd.SelectedColor);
                btnCustomColorTheme.PerformClick();
            }
        }
    }
}
