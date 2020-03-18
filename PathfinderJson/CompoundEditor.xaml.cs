using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        public async Task UpdateTotal(CancellationToken ct)
        {
            int total = 0;

            string magic = txtMagic.Text;
            string misc = txtMisc.Text;
            string size = txtSize.Text;
            string mod = txtModifier.Text;
            string mod2 = txtModifier2.Text;
            string bas = txtBase.Text;

            await Task.Run(() =>
            {
                try { total += int.Parse(magic); } catch (FormatException) { }
                if (ct.IsCancellationRequested) return;
                try { total += int.Parse(misc); } catch (FormatException) { }
                if (ct.IsCancellationRequested) return;
                try { total += int.Parse(size); } catch (FormatException) { }
                if (ct.IsCancellationRequested) return;
                try { total += int.Parse(mod); } catch (FormatException) { }
                if (ct.IsCancellationRequested) return;
                try { total += int.Parse(mod2); } catch (FormatException) { }
                if (ct.IsCancellationRequested) return;
                try { total += int.Parse(bas); } catch (FormatException) { }
            });

            if (ShowPlusTen) total += 10;

            txtTotal.Text = total.ToString();
        }

        public CompoundModifier GetModifier()
        {
            CompoundModifier cm = new CompoundModifier();
            cm.Total = GetStringOrNull(txtTotal.Text, true);
            cm.Base = GetStringOrNull(editBase ? txtBase.Text : "0", true);
            cm.MagicModifier = GetStringOrNull(txtMagic.Text, true);
            cm.MiscModifier = GetStringOrNull(txtMisc.Text, true);
            cm.OtherModifiers = GetStringOrNull(txtOther.Text, true);
            cm.SizeModifier = GetStringOrNull(txtSize.Text, true);

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
