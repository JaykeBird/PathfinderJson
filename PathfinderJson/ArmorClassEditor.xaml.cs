using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static PathfinderJson.CoreUtils;

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

        public void CalculateAcValueChanged(bool newValue)
        {
            grdCalculationData.Visibility = newValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public void UpdateAppearance()
        {
            btnDetails.ColorScheme = App.ColorScheme;
            imgInfo.ColorScheme = App.ColorScheme;

            txtModifier.Background = new SolidColorBrush(App.ColorScheme.SecondHighlightColor);
            brdrModifiers.BorderBrush = new SolidColorBrush(App.ColorScheme.SecondaryColor);
            pathOutline.Stroke = new SolidColorBrush(App.ColorScheme.BorderColor);

            txtTotal.ColorScheme = App.ColorScheme;
            txtTouch.ColorScheme = App.ColorScheme;
            txtFlat.ColorScheme = App.ColorScheme;

            txtArmor.ColorScheme = App.ColorScheme;
            txtNatural.ColorScheme = App.ColorScheme;
            txtSize.ColorScheme = App.ColorScheme;
            txtDeflection.ColorScheme = App.ColorScheme;
            txtShield.ColorScheme = App.ColorScheme;
        }

        public void LoadArmorClass(ArmorClass ac, string modValue)
        {
            txtTotal.ValueString = ac.Total;
            txtTouch.ValueString = ac.Touch;
            txtFlat.ValueString = ac.FlatFooted;

            txtArmor.ValueString = ac.ArmorBonus ?? "0";
            txtNatural.ValueString = ac.NaturalArmor ?? "0";
            txtSize.ValueString = ac.SizeModifier ?? "0";
            txtDeflection.ValueString = ac.Deflection ?? "0";
            txtShield.ValueString = ac.ShieldBonus ?? "0";

            txtModifier.Text = modValue;

            txtMisc.Text = ac.MiscModifier;
            txtOther.Text = ac.OtherModifiers;
        }

        public ArmorClass GetArmorClass()
        {
            ArmorClass ac = new ArmorClass()
            {
                ArmorBonus = GetStringOrNull(txtArmor.ValueString, true),
                NaturalArmor = GetStringOrNull(txtNatural.ValueString, true),
                Deflection = GetStringOrNull(txtDeflection.ValueString, true),
                MiscModifier = GetStringOrNull(txtMisc.Text, true),
                OtherModifiers = GetStringOrNull(txtOther.Text, true),
                ShieldBonus = GetStringOrNull(txtShield.ValueString, true),
                SizeModifier = GetStringOrNull(txtSize.ValueString, true),

                FlatFooted = txtFlat.ValueString,
                Total = txtTotal.ValueString,
                Touch = txtTouch.ValueString
            };

            return ac;
        }

        public void UpdateCoreModifier(string modValue)
        {
            txtModifier.Text = modValue;
        }

        public void UpdateAcItemBonuses(string shield, string armor)
        {
            txtShield.ValueString = shield;
            txtArmor.ValueString = armor;
        }

        public void UpdateTotal()
        {
            int total = 0;
            total += txtDeflection.Value ?? 0;
            total += txtArmor.Value ?? 0;
            total += txtSize.Value ?? 0;
            try { total += int.Parse(txtModifier.Text); } catch (FormatException) { }
            total += txtNatural.Value ?? 0;
            total += txtShield.Value ?? 0;

            total += 10;

            int flat = total;
            try { flat -= int.Parse(txtModifier.Text); } catch (FormatException) { }
            int touch = total;
            touch -= txtArmor.Value ?? 0;
            touch -= txtShield.Value ?? 0;

            txtTotal.Value = total;
            txtFlat.Value = flat;
            txtTouch.Value = touch;
        }

        // event just to update main window's "isDirty" value
        public event EventHandler? ContentChanged;

        private void textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ContentChanged?.Invoke(this, e);
        }

        public bool ShowShieldGlyph
        {
            get { return glyShield.Visibility == Visibility.Visible; }
            set { glyShield.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
        }

        private void textbox_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ContentChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
