using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UiCore;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for ItemEditor.xaml
    /// </summary>
    public partial class ItemEditor : SelectableUserControl
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
                Name = txtName.Text,
                Notes = txtNotes.Text,
                Location = txtLocation.Text,
                Quantity = txtQuantity.Text,
                Type = txtType.Text,
                Weight = txtWeight.Text
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
