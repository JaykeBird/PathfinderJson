using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for CompoundEditor.xaml
    /// </summary>
    public partial class CompoundEditor : UserControl
    {
        public CompoundEditor()
        {
            InitializeComponent();
        }

        public string Title
        {
            get
            {
                return lblTitle.Text;
            }
            set
            {
                lblTitle.Text = value;
            }
        }

        public string ModifierTitle
        {
            get
            {
                return lblModifier.Text;
            }
            set
            {
                lblModifier.Text = value;
            }
        }

        public string Modifier2Title
        {
            get
            {
                return lblModifier2.Text;
            }
            set
            {
                lblModifier2.Text = value;
            }
        }

        public string BaseTitle
        {
            get
            {
                return lblBase.Text;
            }
            set
            {
                lblBase.Text = value;
            }
        }

        public bool ShowMagicModifier
        {
            get
            {
                return showMagic;
            }
            set
            {
                showMagic = value;
                if (value)
                {
                    lblMagic.Visibility = Visibility.Visible;
                    txtMagic.Visibility = Visibility.Visible;
                }
                else
                {
                    lblMagic.Visibility = Visibility.Collapsed;
                    txtMagic.Visibility = Visibility.Collapsed;
                }
            }
        }

        bool editBase = true;
        bool showMagic = true;

        public bool CanEditBase
        {
            get
            {
                return editBase;
            }
            set
            {
                editBase = value;
                txtBase.IsEnabled = value;
                UpdateAppearance();
            }
        }

        public bool ShowPlusTen
        {
            get
            {
                return lblPlusTen.Visibility == Visibility.Visible;
            }
            set
            {
                lblPlusTen.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public void UpdateAppearance()
        {
            SolidColorBrush shc = new SolidColorBrush(App.ColorScheme.SecondHighlightColor);

            txtModifier.Background = shc;
            txtModifier2.Background = shc;

            brdrModifiers.BorderBrush = new SolidColorBrush(App.ColorScheme.SecondaryColor);
            
            if (editBase)
            {
                txtBase.Background = new SolidColorBrush(Colors.White);
            }
            else
            {
                txtBase.Background = shc;
            }
        }

        public void LoadModifier(CompoundModifier cm, string modValue, string modValue2 = "", string baseValue = "")
        {
            txtTotal.Text = cm.Total;
            txtMagic.Text = cm.MagicModifier;
            txtMisc.Text = cm.MiscModifier;
            txtOther.Text = cm.OtherModifiers;
            txtSize.Text = cm.SizeModifier;
            txtModifier.Text = modValue;

            if (!editBase)
            {
                txtBase.Text = baseValue;
            }
            else
            {
                txtBase.Text = cm.Base;
            }

            if (string.IsNullOrEmpty(modValue2))
            {
                txtModifier2.Visibility = Visibility.Collapsed;
                lblModifier2.Visibility = Visibility.Collapsed;
                txtModifier2.Text = "";
            }
            else
            {
                txtModifier2.Visibility = Visibility.Visible;
                lblModifier2.Visibility = Visibility.Visible;
                txtModifier2.Text = modValue2;
            }

        }

        public void UpdateCoreModifier(string modValue, string modValue2 = "", string baseValue = "")
        {
            txtModifier.Text = modValue;

            if (!editBase)
            {
                txtBase.Text = baseValue;
            }

            if (string.IsNullOrEmpty(modValue2))
            {
                txtModifier2.Visibility = Visibility.Collapsed;
                lblModifier2.Visibility = Visibility.Collapsed;
                txtModifier2.Text = "";
            }
            else
            {
                txtModifier2.Visibility = Visibility.Visible;
                lblModifier2.Visibility = Visibility.Visible;
                txtModifier2.Text = modValue2;
            }
        }

        public void UpdateTotal()
        {
            int total = 0;
            try { total += int.Parse(txtMagic.Text); } catch (FormatException) { }
            try { total += int.Parse(txtMisc.Text); } catch (FormatException) { }
            try { total += int.Parse(txtSize.Text); } catch (FormatException) { }
            try { total += int.Parse(txtModifier.Text); } catch (FormatException) { }
            try { total += int.Parse(txtBase.Text); } catch (FormatException) { }
            try { total += int.Parse(txtModifier2.Text); } catch (FormatException) { }

            if (ShowPlusTen) total += 10;

            txtTotal.Text = total.ToString();
        }

        public CompoundModifier GetModifier()
        {
            CompoundModifier cm = new CompoundModifier();
            cm.Total = txtTotal.Text;
            cm.Base = editBase ? txtBase.Text : "0";
            cm.MagicModifier = txtMagic.Text;
            cm.MiscModifier = txtMisc.Text;
            cm.OtherModifiers = txtOther.Text;
            cm.SizeModifier = txtSize.Text;

            return cm;
        }

        private void ExpModifiers_Collapsed(object sender, RoutedEventArgs e)
        {
            rowExtra.Height = new GridLength(0);
        }

        private void ExpModifiers_Expanded(object sender, RoutedEventArgs e)
        {
            rowExtra.Height = new GridLength(95);
        }

        // event just to update main window's "isDirty" value
        public event EventHandler? ContentChanged;

        private void textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ContentChanged?.Invoke(this, e);
        }
    }
}
