using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SolidShineUi;
using PathfinderJson.Ild;
using static PathfinderJson.CoreUtils;
using System.Reflection;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for FeatDisplay.xaml
    /// </summary>
    public partial class FeatEditor : SelectableListItem
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

        [IldLink(BaseName = "Name")]
        public string FeatName { get => (string)GetValue(FeatNameProperty); set => SetValue(FeatNameProperty, value); }

        public static DependencyProperty FeatNameProperty
            = DependencyProperty.Register("FeatName", typeof(string), typeof(FeatEditor));

        [IldLink(BaseName = "Notes")]
        public string Notes { get => (string)GetValue(NotesProperty); set => SetValue(NotesProperty, value); }

        public static DependencyProperty NotesProperty
            = DependencyProperty.Register("Notes", typeof(string), typeof(FeatEditor));

        [IldLink(BaseName = "School")]
        public string School { get => (string)GetValue(SchoolProperty); set => SetValue(SchoolProperty, value); }

        public static DependencyProperty SchoolProperty
            = DependencyProperty.Register("School", typeof(string), typeof(FeatEditor));

        [IldLink(BaseName = "Subschool")]
        public string Subschool { get => (string)GetValue(SubschoolProperty); set => SetValue(SubschoolProperty, value); }

        public static DependencyProperty SubschoolProperty
            = DependencyProperty.Register("Subschool", typeof(string), typeof(FeatEditor));

        [IldLink(BaseName = "Type")]
        public string FeatType { get => (string)GetValue(FeatTypeProperty); set => SetValue(FeatTypeProperty, value); }

        public static DependencyProperty FeatTypeProperty
            = DependencyProperty.Register("FeatType", typeof(string), typeof(FeatEditor));

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
        //public event EventHandler? ContentChanged;

        private void textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DoContentChanged();
            //ContentChanged?.Invoke(this, e);
        }

        private void lblSearch_Click(object sender, RoutedEventArgs e)
        {
            OpenBrowser("https://cse.google.com/cse?cx=006680642033474972217%3A6zo0hx_wle8&q=" + txtName.Text);
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
