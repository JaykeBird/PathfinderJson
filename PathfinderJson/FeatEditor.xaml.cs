using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UiCore;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for FeatDisplay.xaml
    /// </summary>
    public partial class FeatEditor : SelectableUserControl
    {
        public FeatEditor()
        {
            InitializeComponent();
        }

        public void LoadFeat(Feat f)
        {
            txtName.Text = f.Name;
            txtNotes.Text = f.Notes;
            txtSchool.Text = f.School;
            txtSubschool.Text = f.Subschool;
            txtType.Text = f.Type;
        }

        public Feat GetFeat()
        {
            Feat f = new Feat
            {
                Name = txtName.Text,
                Notes = txtNotes.Text,
                School = txtSchool.Text,
                Subschool = txtSubschool.Text,
                Type = txtType.Text
            };

            return f;
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
