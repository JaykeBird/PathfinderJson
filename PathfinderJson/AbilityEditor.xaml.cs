using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using PathfinderJson.Ild;
using SolidShineUi;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for AbilityEditor.xaml
    /// </summary>
    public partial class AbilityEditor : SelectableListItem
    {
        public AbilityEditor()
        {
            InitializeComponent();
        }

        public void LoadAbility(SpecialAbility ab)
        {
            txtName.Text = ab.Name;
            txtNotes.Text = ab.Notes;
            txtType.Text = ab.Type;
        }

        public SpecialAbility GetAbility()
        {
            SpecialAbility sa = new SpecialAbility
            {
                Name = txtName.Text,
                Notes = txtNotes.Text,
                Type = txtType.Text
            };

            return sa;
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

        #region Dependency Properties
        [IldLink("Name")]
        public string ItemName { get => (string)GetValue(ItemNameProperty); set => SetValue(ItemNameProperty, value); }

        public static DependencyProperty ItemNameProperty
            = DependencyProperty.Register("ItemName", typeof(string), typeof(AbilityEditor));

        [IldLink("Notes")]
        public string Notes { get => (string)GetValue(NotesProperty); set => SetValue(NotesProperty, value); }

        public static DependencyProperty NotesProperty
            = DependencyProperty.Register("Notes", typeof(string), typeof(AbilityEditor));

        [IldLink("Type")]
        public string ItemType { get => (string)GetValue(ItemTypeProperty); set => SetValue(ItemTypeProperty, value); }

        public static DependencyProperty ItemTypeProperty
            = DependencyProperty.Register("ItemType", typeof(string), typeof(AbilityEditor));
        #endregion

        public override void ApplyColorScheme(ColorScheme cs)
        {
            base.ApplyColorScheme(cs);

            //btnRemove.ApplyColorScheme(cs);
            btnDetails.ApplyColorScheme(cs);
        }

        private void textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DoContentChanged();
            //ContentChanged?.Invoke(this, e);
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
    }
}
