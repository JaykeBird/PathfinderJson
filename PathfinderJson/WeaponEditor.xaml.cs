using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SolidShineUi;
using static PathfinderJson.CoreUtils;
using PathfinderJson.Ild;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for WeaponEditor.xaml
    /// </summary>
    public partial class WeaponEditor : SelectableListItem
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
                Name = txtName.Text,

                Ammunition = GetStringOrNull(txtAmmunition.Text),
                AttackBonus = GetStringOrNull(txtAttack.Text),
                CriticalRange = GetStringOrNull(txtCritical.Text),
                Damage = GetStringOrNull(txtDamage.Text),
                Notes = GetStringOrNull(txtNotes.Text),
                Range = GetStringOrNull(txtRange.Text),
                Type = GetStringOrNull(txtType.Text)
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

        private void textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DoContentChanged();
        }

        public override void ApplyColorScheme(ColorScheme cs)
        {
            base.ApplyColorScheme(cs);

            btnRemove.ApplyColorScheme(cs);
            btnDetails.ApplyColorScheme(cs);
            btnMoveDown.ApplyColorScheme(cs);
            btnMoveUp.ApplyColorScheme(cs);

            imgMoveDown.ApplyColorScheme(cs);
            imgMoveUp.ApplyColorScheme(cs);
            imgRemove.ApplyColorScheme(cs);
        }

        public override void LoadValues(Dictionary<IldPropertyInfo, object> properties)
        {
            LoadValuesInternal(this, properties);
        }

        public override object? GetPropertyValue(IldPropertyInfo property)
        {
            return GetPropertyValueInternal(this, property.Name);
        }

        public override Dictionary<string, object> GetAllProperties()
        {
            return GetAllPropertiesInternal(this);
        }

        #region Dependency Properties

        [IldLink("Name")]
        public string WeaponName { get => (string)GetValue(WeaponNameProperty); set => SetValue(WeaponNameProperty, value); }

        public static DependencyProperty WeaponNameProperty
            = DependencyProperty.Register("WeaponName", typeof(string), typeof(WeaponEditor));

        [IldLink("AttackBonus")]
        public string AttackBonus { get => (string)GetValue(AttackBonusProperty); set => SetValue(AttackBonusProperty, value); }

        public static DependencyProperty AttackBonusProperty
            = DependencyProperty.Register("AttackBonus", typeof(string), typeof(WeaponEditor));

        [IldLink("CriticalRange")]
        public string CriticalRange { get => (string)GetValue(CriticalRangeProperty); set => SetValue(CriticalRangeProperty, value); }

        public static DependencyProperty CriticalRangeProperty
            = DependencyProperty.Register("CriticalRange", typeof(string), typeof(WeaponEditor));

        [IldLink("Damage")]
        public string Damage { get => (string)GetValue(DamageProperty); set => SetValue(DamageProperty, value); }

        public static DependencyProperty DamageProperty
            = DependencyProperty.Register("Damage", typeof(string), typeof(WeaponEditor));

        [IldLink("Notes")]
        public string Notes { get => (string)GetValue(NotesProperty); set => SetValue(NotesProperty, value); }

        public static DependencyProperty NotesProperty
            = DependencyProperty.Register("Notes", typeof(string), typeof(WeaponEditor));

        [IldLink("Range")]
        public string Range { get => (string)GetValue(RangeProperty); set => SetValue(RangeProperty, value); }

        public static DependencyProperty RangeProperty
            = DependencyProperty.Register("Range", typeof(string), typeof(WeaponEditor));

        [IldLink("Ammunition")]
        public string Ammunition { get => (string)GetValue(AmmunitionProperty); set => SetValue(AmmunitionProperty, value); }

        public static DependencyProperty AmmunitionProperty
            = DependencyProperty.Register("Ammunition", typeof(string), typeof(WeaponEditor));

        [IldLink("Type")]
        public string WeaponType { get => (string)GetValue(WeaponTypeProperty); set => SetValue(WeaponTypeProperty, value); }

        public static DependencyProperty WeaponTypeProperty
            = DependencyProperty.Register("WeaponType", typeof(string), typeof(WeaponEditor));

        #endregion

        private void btnMoveUp_Click(object sender, RoutedEventArgs e)
        {
            DoRequestMoveUp();
        }

        private void btnMoveDown_Click(object sender, RoutedEventArgs e)
        {
            DoRequestMoveDown();
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            DoRequestDelete();
        }
    }
}
