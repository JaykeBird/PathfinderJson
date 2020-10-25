using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SolidShineUi;
using static PathfinderJson.CoreUtils;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for SpellEditor.xaml
    /// </summary>
    public partial class SpellEditor : SelectableUserControl
    {
        public SpellEditor()
        {
            InitializeComponent();
        }

        public override void ApplyColorScheme(ColorScheme cs)
        {
            nudLevel.ApplyColorScheme(cs);
            nudCast.ApplyColorScheme(cs);
            nudPrepared.ApplyColorScheme(cs);
        }

        public void LoadSpell(Spell s)
        {
            txtName.Text = s.Name;
            txtNotes.Text = s.Notes;
            txtSchool.Text = s.School;
            txtSubschool.Text = s.Subschool;

            chkAtWill.IsChecked = s.AtWill;
            chkMarked.IsChecked = s.Marked;

            nudLevel.Value = s.Level;
            nudCast.Value = s.Cast;
            nudPrepared.Value = s.Prepared;
        }

        public Spell GetSpell()
        {
            Spell s = new Spell
            {
                Name = txtName.Text,
                Notes = txtNotes.Text,
                School = txtSchool.Text,
                Subschool = txtSubschool.Text,
                AtWill = chkAtWill.IsChecked.GetValueOrDefault(false),
                Marked = chkMarked.IsChecked.GetValueOrDefault(false),
                Level = nudLevel.Value,
                Cast = nudCast.Value,
                Prepared = nudPrepared.Value,
            };

            return s;
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

        public int Level
        {
            get => nudLevel.Value;
            set => nudLevel.Value = value;
        }

        public bool Marked
        {
            get => chkMarked.IsChecked.GetValueOrDefault(false);
            set => chkMarked.IsChecked = value;
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

        private void nudLevel_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ContentChanged?.Invoke(this, EventArgs.Empty);
        }

        private void lblSearch_Click(object sender, RoutedEventArgs e)
        {
            OpenBrowser("https://cse.google.com/cse?cx=006680642033474972217%3A6zo0hx_wle8&q=" + txtName.Text);
        }

        private void nudLevel_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
