using SolidShineUi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PathfinderJson.ChangeTheme
{
    /// <summary>
    /// Interaction logic for ColorChangeButton.xaml
    /// </summary>
    public partial class ColorChangeButton : UserControl
    {
        public ColorChangeButton()
        {
            InitializeComponent();
            fb.Click += (s, e) => { Click?.Invoke(this, EventArgs.Empty); };
        }

        public event EventHandler? Click;

        public Color DisplayedColor { get => (Color)GetValue(DisplayedColorProperty); set => SetValue(DisplayedColorProperty, value); }

        public static readonly DependencyProperty DisplayedColorProperty
            = DependencyProperty.Register("DisplayedColor", typeof(Color), typeof(ColorChangeButton));

        public ColorScheme ColorScheme { get => (ColorScheme)GetValue(ColorSchemeProperty); set => SetValue(ColorSchemeProperty, value); }

        public static readonly DependencyProperty ColorSchemeProperty
            = DependencyProperty.Register("ColorScheme", typeof(ColorScheme), typeof(ColorChangeButton));

        public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

        public static readonly DependencyProperty TitleProperty
            = DependencyProperty.Register("Title", typeof(string), typeof(ColorChangeButton));

        public bool ShowTitle { get => (bool)GetValue(ShowTitleProperty); set => SetValue(ShowTitleProperty, value); }

        public static readonly DependencyProperty ShowTitleProperty
            = DependencyProperty.Register("ShowTitle", typeof(bool), typeof(ColorChangeButton),
            new FrameworkPropertyMetadata(false));

    }
}
