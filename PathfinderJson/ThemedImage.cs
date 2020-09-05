using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SolidShineUi;

namespace PathfinderJson
{
    public class ThemedImage : Image
    {
        private string _image = "";
        private ImageColor _theme = ImageColor.Color;

        public string ImageName
        {
            get
            {
                return _image;
            }
            set
            {
                _image = value;
                SetImage();
            }
        }

        public ImageColor ColorTheme
        {
            get
            {
                return _theme;
            }
            set
            {
                _theme = value;
                SetImage();
            }
        }

        #region Color Scheme

        public event DependencyPropertyChangedEventHandler? ColorSchemeChanged;

        public static DependencyProperty ColorSchemeProperty
            = DependencyProperty.Register("ColorScheme", typeof(ColorScheme), typeof(ThemedImage),
            new FrameworkPropertyMetadata(new ColorScheme(), new PropertyChangedCallback(OnColorSchemeChanged)));

        public static void OnColorSchemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorScheme cs = (e.NewValue as ColorScheme)!;

            if (d is ThemedImage s)
            {
                s.ColorSchemeChanged?.Invoke(d, e);
                s.ApplyColorScheme(cs);
            }
        }

        public ColorScheme ColorScheme
        {
            get => (ColorScheme)GetValue(ColorSchemeProperty);
            set => SetValue(ColorSchemeProperty, value);
        }

        public void ApplyColorScheme(ColorScheme cs)
        {
            if (cs != ColorScheme)
            {
                ColorScheme = cs;
                return;
            }

            if (cs.IsHighContrast)
            {
                if (cs.BackgroundColor == Colors.Black)
                {
                    _theme = ImageColor.White;
                }
                else
                {
                    _theme = ImageColor.Black;
                }
            }
            else
            {
                if (cs.BackgroundColor == Colors.Black)
                {
                    _theme = ImageColor.White;
                }
                else if (cs.BackgroundColor == Colors.White)
                {
                    _theme = ImageColor.Black;
                }
                else
                {
                    _theme = ImageColor.Color;
                }
            }

            SetImage();
        }
        #endregion

        bool init = false;

        void SetImage()
        {
            if (init)
            {
                ApplyColorScheme(ColorScheme);
            }

            try
            {
                Source = App.GetResourcesImage(_theme.ToString("g") + "/" + _image);
            }
            catch (System.IO.IOException)
            {
                Source = null;
            }
        }
    }

    public enum ImageColor
    {
        Color = 0,
        Black = 1,
        White = 2
    }
}
