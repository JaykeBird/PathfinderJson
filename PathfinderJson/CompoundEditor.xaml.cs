using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static PathfinderJson.CoreUtils;

namespace PathfinderJson
{
    /// <summary>
    /// An editor for compound properties (i.e. properties that can be influenced by a variety of sources/modifiers).
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

        /// <summary>Set if the user can edit the base value.</summary>
        bool editBase = true;
        /// <summary>Set if the Magic Modifier box should be displayed.</summary>
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

        /// <summary>
        /// Get or set if a "+10" should be displayed in the editor, and also added when the total is updated.
        /// </summary>
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
            btnDetails.ColorScheme = App.ColorScheme;

            txtTotal.ColorScheme = App.ColorScheme;
            txtBase.ColorScheme = App.ColorScheme;
            txtMagic.ColorScheme = App.ColorScheme;
            txtMisc.ColorScheme = App.ColorScheme;
            txtSize.ColorScheme = App.ColorScheme;

            SolidColorBrush shc = new SolidColorBrush(App.ColorScheme.SecondHighlightColor);

            txtModifier.Background = shc;
            txtModifier2.Background = shc;

            brdrModifiers.BorderBrush = new SolidColorBrush(App.ColorScheme.SecondaryColor);
            
            if (editBase)
            {
                if (SystemParameters.HighContrast)
                {
                    txtBase.Background = new SolidColorBrush(SystemColors.ControlColor);
                }
                else
                {
                    txtBase.Background = new SolidColorBrush(Colors.White);
                }
            }
            else
            {
                txtBase.Background = shc;
            }
        }

        public void LoadModifier(CompoundModifier cm, string modValue, string modValue2 = "", string baseValue = "")
        {
            txtTotal.ValueString = cm.Total ?? "0";
            txtMagic.ValueString = cm.MagicModifier ?? "0";
            txtMisc.ValueString = cm.MiscModifier ?? "0";
            txtSize.ValueString = cm.SizeModifier ?? "0";

            txtOther.Text = cm.OtherModifiers;
            txtModifier.Text = modValue;
            txtTemp.Text = cm.TempModifier;

            if (!editBase)
            {
                txtBase.ValueString = baseValue;
            }
            else
            {
                txtBase.ValueString = cm.Base ?? "0";
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
                txtBase.ValueString = baseValue;
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

            total += txtMagic.Value ?? 0;
            total += txtMisc.Value ?? 0;
            total += txtSize.Value ?? 0;
            total += txtBase.Value ?? 0;

            try { total += int.Parse(txtModifier.Text); } catch (FormatException) { }
            try { total += int.Parse(txtModifier2.Text); } catch (FormatException) { }

            if (ShowPlusTen) total += 10;

            txtTotal.Value = total;
        }

        public CompoundModifier GetModifier()
        {
            CompoundModifier cm = new CompoundModifier();
            cm.Total = GetStringOrNull(txtTotal.ValueString, true);
            cm.Base = GetStringOrNull(editBase ? txtBase.ValueString : "0", true);
            cm.MagicModifier = GetStringOrNull(txtMagic.ValueString, true);
            cm.MiscModifier = GetStringOrNull(txtMisc.ValueString, true);
            cm.OtherModifiers = GetStringOrNull(txtOther.Text, true);
            cm.SizeModifier = GetStringOrNull(txtSize.ValueString, true);
            cm.TempModifier = GetStringOrNull(txtTemp.Text, true);

            return cm;
        }

        // event just to update main window's "isDirty" value
        public event EventHandler? ContentChanged;

        private void textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ContentChanged?.Invoke(this, e);
        }

        private void textbox_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ContentChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
