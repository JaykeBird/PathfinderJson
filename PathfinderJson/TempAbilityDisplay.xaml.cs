using SolidShineUi;
using System;
using System.Windows;
using System.Windows.Controls;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for TempAbilityDisplay.xaml
    /// </summary>
    public partial class TempAbilityDisplay : UserControl
    {
        public TempAbilityDisplay()
        {
            InitializeComponent();
        }

        #region ColorScheme

        public event DependencyPropertyChangedEventHandler? ColorSchemeChanged;

        public static DependencyProperty ColorSchemeProperty
            = DependencyProperty.Register("ColorScheme", typeof(ColorScheme), typeof(TempAbilityDisplay),
            new FrameworkPropertyMetadata(new ColorScheme(), new PropertyChangedCallback(OnColorSchemeChanged)));

        public static void OnColorSchemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorScheme cs = (e.NewValue as ColorScheme)!;

            if (d is TempAbilityDisplay s)
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

            brdr.BorderBrush = cs.SecondaryColor.ToBrush();
            brdr.Background = cs.LightBackgroundColor.ToBrush();
        }

        #endregion

        public event EventHandler? CloseRequested;

        public int Value { get => (int?)nudValue.Value ?? 0; set => nudValue.Value = value; }

        int _modifier = 0;

        public int Modifier { get => _modifier; }

        private void nudValue_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _modifier = CoreUtils.CalculateModifierInt((int)e.NewValue);

            if (txtMod == null) return;

            txtMod.Text = "( " + CoreUtils.DisplayModifier(_modifier) + " )";
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
