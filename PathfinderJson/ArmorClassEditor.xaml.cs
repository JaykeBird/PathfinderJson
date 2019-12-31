using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for ArmorClassEditor.xaml
    /// </summary>
    public partial class ArmorClassEditor : UserControl
    {
        public ArmorClassEditor()
        {
            InitializeComponent();
        }

        public void UpdateAppearance()
        {
            txtModifier.Background = new SolidColorBrush(App.ColorScheme.SecondHighlightColor);
            brdrModifiers.BorderBrush = new SolidColorBrush(App.ColorScheme.SecondaryColor);
        }

        private void ExpModifiers_Collapsed(object sender, RoutedEventArgs e)
        {
            rowExtra.Height = new GridLength(0);
        }

        private void ExpModifiers_Expanded(object sender, RoutedEventArgs e)
        {
            rowExtra.Height = new GridLength(95);
        }

        public void LoadArmorClass(ArmorClass ac, string modValue)
        {
            txtTotal.Text = ac.Total;
            txtTouch.Text = ac.Touch;
            txtFlat.Text = ac.FlatFooted;

            txtArmor.Text = ac.ArmorBonus;
            txtNatural.Text = ac.NaturalArmor;
            txtSize.Text = ac.SizeModifier;
            txtDeflection.Text = ac.Deflection;
            txtShield.Text = ac.ShieldBonus;

            txtModifier.Text = modValue;

            txtMisc.Text = ac.MiscModifier;
            txtOther.Text = ac.OtherModifiers;
        }

        public ArmorClass GetArmorClass()
        {
            ArmorClass ac = new ArmorClass()
            {
                ArmorBonus = txtArmor.Text,
                NaturalArmor = txtNatural.Text,
                Deflection = txtDeflection.Text,
                FlatFooted = txtFlat.Text,
                MiscModifier = txtMisc.Text,
                OtherModifiers = txtOther.Text,
                ShieldBonus = txtShield.Text,
                SizeModifier = txtSize.Text,
                Total = txtTotal.Text,
                Touch = txtTouch.Text
            };

            return ac;
        }

        public void UpdateCoreModifier(string modValue)
        {
            txtModifier.Text = modValue;
        }

        public void UpdateAcItemBonuses(string shield, string armor)
        {
            txtShield.Text = shield;
            txtArmor.Text = armor;
        }

        public void UpdateTotal()
        {
            int total = 0;
            try { total += int.Parse(txtDeflection.Text); } catch (FormatException) { }
            try { total += int.Parse(txtArmor.Text); } catch (FormatException) { }
            try { total += int.Parse(txtSize.Text); } catch (FormatException) { }
            try { total += int.Parse(txtModifier.Text); } catch (FormatException) { }
            try { total += int.Parse(txtNatural.Text); } catch (FormatException) { }
            try { total += int.Parse(txtShield.Text); } catch (FormatException) { }

            total += 10;

            int flat = total;
            try { flat -= int.Parse(txtModifier.Text); } catch (FormatException) { }
            int touch = total;
            try { touch -= int.Parse(txtArmor.Text); } catch (FormatException) { }
            try { touch -= int.Parse(txtShield.Text); } catch (FormatException) { }

            txtTotal.Text = total.ToString();
            txtFlat.Text = flat.ToString();
            txtTouch.Text = touch.ToString();
        }

        // event just to update main window's "isDirty" value
        public event EventHandler? ContentChanged;

        private void textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ContentChanged?.Invoke(this, e);
        }
    }
}
