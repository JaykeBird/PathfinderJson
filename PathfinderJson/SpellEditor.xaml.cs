using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PathfinderJson.Ild;
using SolidShineUi;
using static PathfinderJson.CoreUtils;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for SpellEditor.xaml
    /// </summary>
    public partial class SpellEditor : SelectableListItem
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
            chkMarked.ApplyColorScheme(cs);
            expander.ApplyColorScheme(cs);
            btnMoveDown.ApplyColorScheme(cs);
            btnMoveUp.ApplyColorScheme(cs);
            btnRemove.ApplyColorScheme(cs);
            btnAddCast.ApplyColorScheme(cs);

            imgMoveDown.ApplyColorScheme(cs);
            imgMoveUp.ApplyColorScheme(cs);
            imgRemove.ApplyColorScheme(cs);
            imgAdd.ApplyColorScheme(cs);
        }

        public void LoadSpell(Spell s)
        {
            SpellName = s.Name;
            txtNotes.Text = s.Notes;
            txtSchool.Text = s.School;
            txtSubschool.Text = s.Subschool;

            chkAtWill.IsChecked = s.AtWill;
            chkMarked.IsChecked = s.Marked;

            Level = s.Level;
            nudCast.Value = s.Cast;
            nudPrepared.Value = s.Prepared;
        }

        public Spell GetSpell()
        {
            Spell s = new Spell
            {
                Name = SpellName,
                Notes = txtNotes.Text,
                School = txtSchool.Text,
                Subschool = txtSubschool.Text,
                AtWill = chkAtWill.IsChecked,
                Marked = chkMarked.IsChecked,
                Level = Level,
                //Level = nudLevel.Value,
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

        [IldLink("Name")]
        public string SpellName { get => (string)GetValue(SpellNameProperty); set => SetValue(SpellNameProperty, value); }

        /// <summary>The backing dependency property for <see cref="SpellName"/>. See the related property for details.</summary>
        public static DependencyProperty SpellNameProperty
            = DependencyProperty.Register(nameof(SpellName), typeof(string), typeof(SpellEditor),
            new FrameworkPropertyMetadata("", OnSpellNameChanged));

        private static void OnSpellNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SpellEditor o)
            {
                if (o.SpellName == "")
                {
                    o.txtName.ToolTip = "Spell Name";
                }
                else
                {
                    o.txtName.ToolTip = "Spell Name:\n" + o.SpellName;
                }
            }
        }

        [IldLink("Level")]
        public int Level { get => (int)GetValue(LevelProperty); set => SetValue(LevelProperty, value); }

        /// <summary>The backing dependency property for <see cref="Level"/>. See the related property for details.</summary>
        public static DependencyProperty LevelProperty
            = DependencyProperty.Register(nameof(Level), typeof(int), typeof(SpellEditor),
            new FrameworkPropertyMetadata(0, OnLevelChanged));

        private static void OnLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SpellEditor o)
            {
                o.chkMarked.Content = o.Level switch
                {
                    1 => "1st",
                    2 => "2nd",
                    3 => "3rd",
                    _ => o.Level + "th"
                };
            }
        }

        public bool Marked
        {
            get => chkMarked.IsChecked;
            set => chkMarked.IsChecked = value;
        }

        // event just to update main window's "isDirty" value
        //public event EventHandler? ContentChanged;

        private void textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DoContentChanged();
        }

        private void nudLevel_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DoContentChanged();
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
