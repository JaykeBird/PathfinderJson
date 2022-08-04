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
    /// Interaction logic for ItemEditor.xaml
    /// </summary>
    public partial class ItemEditor : SelectableListItem
    {
        public ItemEditor()
        {
            InitializeComponent();
        }

        public void LoadEquipment(Equipment q)
        {
            txtName.Text = q.Name;
            txtNotes.Text = q.Notes;
            txtLocation.Text = q.Location;
            txtQuantity.Text = q.Quantity;
            txtWeight.Text = q.Weight;
            txtType.Text = q.Type;
        }

        public Equipment GetEquipment()
        {
            Equipment q = new Equipment
            {
                Name = GetStringOrNull(txtName.Text),
                Notes = GetStringOrNull(txtNotes.Text),
                Location = GetStringOrNull(txtLocation.Text),
                Quantity = GetStringOrNull(txtQuantity.Text),
                Type = GetStringOrNull(txtType.Text),
                Weight = GetStringOrNull(txtWeight.Text)
            };

            return q;
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
            = DependencyProperty.Register("ItemName", typeof(string), typeof(ItemEditor));

        [IldLink("Notes")]
        public string Notes { get => (string)GetValue(NotesProperty); set => SetValue(NotesProperty, value); }

        public static DependencyProperty NotesProperty
            = DependencyProperty.Register("Notes", typeof(string), typeof(ItemEditor));

        [IldLink("Location")]
        public string Location { get => (string)GetValue(LocationProperty); set => SetValue(LocationProperty, value); }

        public static DependencyProperty LocationProperty
            = DependencyProperty.Register("Location", typeof(string), typeof(ItemEditor));

        [IldLink("Quantity")]
        public int Quantity { get => (int)GetValue(QuantityProperty); set => SetValue(QuantityProperty, value); }

        public static DependencyProperty QuantityProperty
            = DependencyProperty.Register("Quantity", typeof(int), typeof(ItemEditor));

        [IldLink("Type")]
        public string ItemType { get => (string)GetValue(ItemTypeProperty); set => SetValue(ItemTypeProperty, value); }

        public static DependencyProperty ItemTypeProperty
            = DependencyProperty.Register("ItemType", typeof(string), typeof(ItemEditor));

        [IldLink("Weight")]
        public string Weight { get => (string)GetValue(WeightProperty); set => SetValue(WeightProperty, value); }

        public static DependencyProperty WeightProperty
            = DependencyProperty.Register("Weight", typeof(string), typeof(ItemEditor));

        [IldLink("Equippable")]
        public bool Equippable { get => (bool)GetValue(EquippableProperty); set => SetValue(EquippableProperty, value); }

        public static DependencyProperty EquippableProperty
            = DependencyProperty.Register("Equippable", typeof(bool), typeof(ItemEditor));

        [IldLink("Equipped")]
        public bool Equipped { get => (bool)GetValue(EquippedProperty); set => SetValue(EquippedProperty, value); }

        public static DependencyProperty EquippedProperty
            = DependencyProperty.Register("Equipped", typeof(bool), typeof(ItemEditor));

        #endregion

        public override void ApplyColorScheme(ColorScheme cs)
        {
            base.ApplyColorScheme(cs);

            btnRemove.ApplyColorScheme(cs);
            imgRemove.ApplyColorScheme(cs);
            btnDetails.ApplyColorScheme(cs);

            btnMoveDown.ApplyColorScheme(cs);
            btnMoveUp.ApplyColorScheme(cs);
            imgMoveDown.ApplyColorScheme(cs);
            imgMoveUp.ApplyColorScheme(cs);
        }

        // event just to update main window's "isDirty" value
        //public event EventHandler? ContentChanged;

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

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            DoRequestDelete();
        }

        private void btnMoveUp_Click(object sender, RoutedEventArgs e)
        {
            DoRequestMoveUp();
        }

        private void btnMoveDown_Click(object sender, RoutedEventArgs e)
        {
            DoRequestMoveDown();
        }
    }
}
