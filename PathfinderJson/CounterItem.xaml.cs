using SolidShineUi;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for CounterItem.xaml
    /// </summary>
    public partial class CounterItem : UserControl
    {
        public event EventHandler? RemoveRequested;

        public CounterItem()
        {
            InitializeComponent();
        }

        public int Value { get; set; }

        #region Color Scheme

        public event DependencyPropertyChangedEventHandler? ColorSchemeChanged;

        public static DependencyProperty ColorSchemeProperty
            = DependencyProperty.Register("ColorScheme", typeof(ColorScheme), typeof(CounterItem),
            new FrameworkPropertyMetadata(new ColorScheme(), new PropertyChangedCallback(OnColorSchemeChanged)));

        public static void OnColorSchemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorScheme cs = (e.NewValue as ColorScheme)!;

            if (d is CounterItem s)
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
        }
        #endregion

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            RemoveRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            txtTitle.Visibility = Visibility.Visible;
            txtTitle.Text = lblTitle.Text;
            txtTitle.Focus();
        }

        private void btnDecrease_Click(object sender, RoutedEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                Value -= 5;
            }
            else
            {
                Value--;
            }
            lblValue.Text = Value.ToString();
        }

        private void btnIncrease_Click(object sender, RoutedEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                Value += 5;
            }
            else
            {
                Value++;
            }
            lblValue.Text = Value.ToString();
        }

        private void txtTitle_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                txtTitle.Visibility = Visibility.Collapsed;
                lblTitle.Text = txtTitle.Text;
            }
        }
    }
}
