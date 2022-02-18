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

        private int _value = 0;

        public int Value { get => _value; set { _value = value; lblValue.Text = value.ToString(); } }

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

        bool editMode = false;

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (editMode)
            {
                ExitEditMode();
            }
            else
            {
                RemoveRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (editMode)
            {
                ApplyEditChanges();
                ExitEditMode();
            }
            else
            {
                nudInput.Visibility = Visibility.Visible;
                nudInput.Value = Value;
                btnChangeColor.Visibility = Visibility.Visible;
                btnChangeColor.Background = brdrEllipse.Background;
                btnChangeColor.HighlightBrush = brdrEllipse.Background;
                btnChangeColor.ClickBrush = brdrEllipse.Background;

                txtTitle.Visibility = Visibility.Visible;
                txtTitle.Text = lblTitle.Text;
                txtTitle.Focus();

                btnDecrease.Visibility = Visibility.Collapsed;
                btnIncrease.Visibility = Visibility.Collapsed;

                imgEdit.ImageName = "Save";
                imgDelete.ImageName = "Undo";

                btnEdit.ToolTip = "Apply Changes";
                btnDelete.ToolTip = "Cancel";

                editMode = true;
            }
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
            //lblValue.Text = Value.ToString();
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
            //lblValue.Text = Value.ToString();
        }

        private void txtTitle_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyEditChanges();
                ExitEditMode();
            }
            else if (e.Key == Key.Escape)
            {
                ExitEditMode();
            }
        }

        void ApplyEditChanges()
        {
            lblTitle.Text = txtTitle.Text;
            Value = nudInput.Value;
            if (brdrEllipse.Background != btnChangeColor.Background)
            {
                brdrEllipse.Background = btnChangeColor.Background;
                innerBorder.Background = btnChangeColor.Background;
            }
        }

        void ExitEditMode()
        {
            nudInput.Visibility = Visibility.Collapsed;
            btnChangeColor.Visibility = Visibility.Collapsed;
            txtTitle.Visibility = Visibility.Collapsed;

            btnDecrease.Visibility = Visibility.Visible;
            btnIncrease.Visibility = Visibility.Visible;

            imgEdit.ImageName = "Edit";
            imgDelete.ImageName = "Cancel";

            btnEdit.ToolTip = "Edit";
            btnDelete.ToolTip = "Delete";

            editMode = false;
        }

        private void btnChangeColor_Click(object sender, RoutedEventArgs e)
        {
            ColorPickerDialog cpd = new ColorPickerDialog(ColorScheme, ((SolidColorBrush)btnChangeColor.Background).Color);
            cpd.Owner = Window.GetWindow(this);
            cpd.ShowDialog();

            if (cpd.DialogResult)
            {
                btnChangeColor.Background = cpd.SelectedColor.ToBrush();
                btnChangeColor.HighlightBrush = cpd.SelectedColor.ToBrush();
                btnChangeColor.ClickBrush = cpd.SelectedColor.ToBrush();
            }
        }
    }
}
