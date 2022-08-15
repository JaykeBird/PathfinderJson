using System;
using System.Collections.Generic;
using System.Linq;
using SolidShineUi;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PathfinderJson.ChangeTheme
{
    /// <summary>
    /// Interaction logic for ColorSchemeButton.xaml
    /// </summary>
    public partial class ColorSchemeButton : UserControl
    {
        public ColorSchemeButton()
        {
            InitializeComponent();
            btn.Click += (s, e) => { Click?.Invoke(this, EventArgs.Empty); };
        }

        public void PerformClick()
        {
            Click?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? Click;

        private ColorScheme _cs = new ColorScheme();

        public ColorScheme DisplayedColorScheme
        {
            get { return _cs; }
            set
            {
                _cs = value;

                brdrWindow.BorderBrush = _cs.BorderColor.ToBrush();
                brdrWindow.Background = _cs.BackgroundColor.ToBrush();
                txtFore.Foreground = _cs.ForegroundColor.ToBrush();
                grdTitlebar.Background = _cs.WindowTitleBarColor.ToBrush();
                grdMain.Background = _cs.MainColor.ToBrush();
                brdrPopout.BorderBrush = _cs.BorderColor.ToBrush();
                brdrPopout.Background = _cs.SecondaryColor.ToBrush();
            }
        }

        public ColorScheme ColorScheme { get => (ColorScheme)GetValue(ColorSchemeProperty); set => SetValue(ColorSchemeProperty, value); }

        public static DependencyProperty ColorSchemeProperty
            = DependencyProperty.Register("ColorScheme", typeof(ColorScheme), typeof(ColorSchemeButton));

        public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

        public static DependencyProperty TitleProperty
            = DependencyProperty.Register("Title", typeof(string), typeof(ColorSchemeButton));

        public bool ShowTitle { get => (bool)GetValue(ShowTitleProperty); set => SetValue(ShowTitleProperty, value); }

        public static DependencyProperty ShowTitleProperty
            = DependencyProperty.Register("ShowTitle", typeof(bool), typeof(ColorSchemeButton),
            new FrameworkPropertyMetadata(false));

        public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }

        public static DependencyProperty IsSelectedProperty
            = DependencyProperty.Register("IsSelected", typeof(bool), typeof(ColorSchemeButton),
            new FrameworkPropertyMetadata(false));


        public static ColorSchemeButton CreateButtonFromScheme(ColorScheme cs, string? title = null)
        {
            ColorSchemeButton csb = new ColorSchemeButton();
            csb.DisplayedColorScheme = cs;
            csb.Title = title ?? "";
            csb.ShowTitle = !(title == null);

            return csb;
        }

    }
}
