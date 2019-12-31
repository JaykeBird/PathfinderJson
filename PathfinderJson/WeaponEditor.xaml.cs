using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UiCore;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for WeaponEditor.xaml
    /// </summary>
    public partial class WeaponEditor : SelectableUserControl
    {

        public WeaponEditor()
        {
            InitializeComponent();
        }

        public void LoadWeapon(Weapon w)
        {
            txtAttack.Text = w.AttackBonus;
            txtCritical.Text = w.CriticalRange;
            txtAmmunition.Text = w.Ammunition;
            txtDamage.Text = w.Damage;
            txtName.Text = w.Name;
            txtNotes.Text = w.Notes;
            txtRange.Text = w.Range;
            txtType.Text = w.Type;
        }

        public Weapon GetWeapon()
        {
            Weapon w = new Weapon()
            {
                Ammunition = txtAmmunition.Text,
                AttackBonus = txtAttack.Text,
                CriticalRange = txtCritical.Text,
                Damage = txtDamage.Text,
                Name = txtName.Text,
                Notes = txtNotes.Text,
                Range = txtRange.Text,
                Type = txtType.Text
            };

            return w;
        }

        public bool EnableSpellCheck
        {
            get
            {
                return SpellCheck.GetIsEnabled(txtNotes);
            }
            set
            {
                SpellCheck.SetIsEnabled(txtNotes, value);
            }
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            rowDetails.Height = new GridLength(1, GridUnitType.Star);
        }

        private void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            rowDetails.Height = new GridLength(0);
        }

        // event just to update main window's "isDirty" value
        public event EventHandler? ContentChanged;

        private void textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ContentChanged?.Invoke(this, e);
        }
    }
}
