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
            btnReset.ApplyColorScheme(cs);

            imgMoveDown.ApplyColorScheme(cs);
            imgMoveUp.ApplyColorScheme(cs);
            imgRemove.ApplyColorScheme(cs);
            imgAdd.ApplyColorScheme(cs);
            imgReset.ApplyColorScheme(cs);
        }

        public void LoadSpell(Spell s)
        {
            SpellName = s.Name;
            Notes = s.Notes;
            School = s.School;
            Subschool = s.Subschool;

            AtWill = s.AtWill;
            Marked = s.Marked;

            Level = s.Level;
            Cast = s.Cast;
            Prepared = s.Prepared;
        }

        public Spell GetSpell()
        {
            Spell s = new Spell
            {
                Name = SpellName,
                Notes = Notes,
                School = School,
                Subschool = Subschool,
                AtWill = AtWill,
                Marked = Marked,
                Level = Level,
                //Level = nudLevel.Value,
                Cast = Cast,
                Prepared = Prepared,
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
        public static readonly DependencyProperty SpellNameProperty
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
        public static readonly DependencyProperty LevelProperty
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

        [IldLink(baseName: "Marked")]
        public bool Marked { get => (bool)GetValue(MarkedProperty); set => SetValue(MarkedProperty, value); }

        /// <summary>The backing dependency property for <see cref="Marked"/>. See the related property for details.</summary>
        public static readonly DependencyProperty MarkedProperty
            = DependencyProperty.Register(nameof(Marked), typeof(bool), typeof(SpellEditor),
            new FrameworkPropertyMetadata(false));

        [IldLink("AtWill")]
        public bool AtWill { get => (bool)GetValue(AtWillProperty); set => SetValue(AtWillProperty, value); }

        /// <summary>The backing dependency property for <see cref="AtWill"/>. See the related property for details.</summary>
        public static readonly DependencyProperty AtWillProperty
            = DependencyProperty.Register(nameof(AtWill), typeof(bool), typeof(SpellEditor),
            new FrameworkPropertyMetadata(false));

        [IldLink(baseName: "Cast")]
        public int Cast { get => (int)GetValue(CastProperty); set => SetValue(CastProperty, value); }

        /// <summary>The backing dependency property for <see cref="Cast"/>. See the related property for details.</summary>
        public static readonly DependencyProperty CastProperty
            = DependencyProperty.Register(nameof(Cast), typeof(int), typeof(SpellEditor),
            new FrameworkPropertyMetadata(0));

        [IldLink("Prepared")]
        public int Prepared { get => (int)GetValue(PreparedProperty); set => SetValue(PreparedProperty, value); }

        /// <summary>The backing dependency property for <see cref="Prepared"/>. See the related property for details.</summary>
        public static readonly DependencyProperty PreparedProperty
            = DependencyProperty.Register(nameof(Prepared), typeof(int), typeof(SpellEditor),
            new FrameworkPropertyMetadata(0));

        [IldLink("School")]
        public string School { get => (string)GetValue(SchoolProperty); set => SetValue(SchoolProperty, value); }

        /// <summary>The backing dependency property for <see cref="School"/>. See the related property for details.</summary>
        public static readonly DependencyProperty SchoolProperty
            = DependencyProperty.Register(nameof(School), typeof(string), typeof(SpellEditor),
            new FrameworkPropertyMetadata(""));

        [IldLink("Subschool")]
        public string Subschool { get => (string)GetValue(SubschoolProperty); set => SetValue(SubschoolProperty, value); }

        /// <summary>The backing dependency property for <see cref="Subschool"/>. See the related property for details.</summary>
        public static DependencyProperty SubschoolProperty
            = DependencyProperty.Register(nameof(Subschool), typeof(string), typeof(SpellEditor),
            new FrameworkPropertyMetadata(""));

        [IldLink("Notes")]
        public string Notes { get => (string)GetValue(NotesProperty); set => SetValue(NotesProperty, value); }

        /// <summary>The backing dependency property for <see cref="Notes"/>. See the related property for details.</summary>
        public static DependencyProperty NotesProperty
            = DependencyProperty.Register(nameof(Notes), typeof(string), typeof(SpellEditor),
            new FrameworkPropertyMetadata(""));


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

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            ResetCastingAmount();
        }

        private void btnAddCast_Click(object sender, RoutedEventArgs e)
        {
            nudCast.Value++;
        }

        public void ResetCastingAmount()
        {
            nudCast.Value = 0;
        }
    }
}
