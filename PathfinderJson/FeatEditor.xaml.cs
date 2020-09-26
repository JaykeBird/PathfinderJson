using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SolidShineUi;
using static PathfinderJson.CoreUtils;

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
            rowDetails.Height = new GridLength(1, GridUnitType.Auto);
            rowDetails.MinHeight = 125;
        }

        private void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            rowDetails.Height = new GridLength(0);
            rowDetails.MinHeight = 0;
        }

        // event just to update main window's "isDirty" value
        public event EventHandler? ContentChanged;

        private void textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ContentChanged?.Invoke(this, e);
        }

        private void lblSearch_Click(object sender, RoutedEventArgs e)
        {
            OpenBrowser("https://cse.google.com/cse?cx=006680642033474972217%3A6zo0hx_wle8&q=" + txtName.Text);
        }
    }
}
