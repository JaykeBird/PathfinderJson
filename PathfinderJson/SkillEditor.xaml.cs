using SolidShineUi;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static PathfinderJson.CoreUtils;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for SkillEditor.xaml
    /// </summary>
    public partial class SkillEditor : UserControl
    {

        public SkillEditor()
        {
            InitializeComponent();
            _init = false;

            //UpdateCalculations();
        }


        public event EventHandler? ContentChanged;
        public event EventHandler? ModifierChanged;

        bool _init = true;
        bool _cbbc = false;

        #region ColorScheme

        public event DependencyPropertyChangedEventHandler? ColorSchemeChanged;

        public static DependencyProperty ColorSchemeProperty
            = DependencyProperty.Register("ColorScheme", typeof(ColorScheme), typeof(SkillEditor),
            new FrameworkPropertyMetadata(new ColorScheme(), new PropertyChangedCallback(OnColorSchemeChanged)));

        public static void OnColorSchemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorScheme cs = (e.NewValue as ColorScheme)!;

            if (d is SkillEditor s)
            {
                s.ColorSchemeChanged?.Invoke(d, e);
                s.ApplyColorScheme(cs);
            }
        }

        public ColorScheme ColorScheme
        {
            get => (ColorScheme)GetValue(ColorSchemeProperty);
            set => SetValue(ColorSchemeProperty, value);
        }

        public void ApplyColorScheme(ColorScheme cs)
        {
            if (cs != ColorScheme)
            {
                ColorScheme = cs;
                return;
            }

            BorderBrush = cs.BorderColor.ToBrush();
        }

        #endregion

        public string InternalSkillName { get; set; } = "Unnamed";

        public static DependencyProperty SkillRanksProperty
            = DependencyProperty.Register("SkillRanks", typeof(int), typeof(SkillEditor),
            new FrameworkPropertyMetadata(0));

        public int SkillRanks
        {
            get => (int)GetValue(SkillRanksProperty);
            set => SetValue(SkillRanksProperty, value);
        }

        public static DependencyProperty MiscModifierProperty
            = DependencyProperty.Register("MiscModifier", typeof(int), typeof(SkillEditor),
            new FrameworkPropertyMetadata(0));

        public int MiscModifier
        {
            get => (int)GetValue(MiscModifierProperty);
            set => SetValue(MiscModifierProperty, value);
        }

        public static DependencyProperty RacialModifierProperty
            = DependencyProperty.Register("RacialModifier", typeof(int), typeof(SkillEditor),
            new FrameworkPropertyMetadata(0));

        public int RacialModifier
        {
            get => (int)GetValue(RacialModifierProperty);
            set => SetValue(RacialModifierProperty, value);
        }

        public static DependencyProperty TraitModifierProperty
    = DependencyProperty.Register("TraitModifier", typeof(int), typeof(SkillEditor),
    new FrameworkPropertyMetadata(0));

        public int TraitModifier
        {
            get => (int)GetValue(TraitModifierProperty);
            set => SetValue(TraitModifierProperty, value);
        }

        public static DependencyProperty ModifierValueProperty
            = DependencyProperty.Register("ModifierValue", typeof(int), typeof(SkillEditor),
            new FrameworkPropertyMetadata(0));

        public int ModifierValue
        {
            get => (int)GetValue(ModifierValueProperty);
            set => SetValue(ModifierValueProperty, value);
        }

        public static DependencyProperty IsTrainedProperty
            = DependencyProperty.Register("IsTrained", typeof(bool), typeof(SkillEditor),
            new FrameworkPropertyMetadata(false));

        public bool IsTrained
        {
            get => (bool)GetValue(IsTrainedProperty);
            set => SetValue(IsTrainedProperty, value);
        }

        public static DependencyProperty HasSpecializationProperty
            = DependencyProperty.Register("HasSpecialization", typeof(bool), typeof(SkillEditor),
            new FrameworkPropertyMetadata(false));

        public bool HasSpecialization
        {
            get => (bool)GetValue(HasSpecializationProperty);
            set => SetValue(HasSpecializationProperty, value);
        }

        public static DependencyProperty SkillNameProperty
            = DependencyProperty.Register("SkillName", typeof(string), typeof(SkillEditor),
            new FrameworkPropertyMetadata("Skill name here"));

        public string SkillName
        {
            get => (string)GetValue(SkillNameProperty);
            set => SetValue(SkillNameProperty, value);
        }

        public static DependencyProperty SpecializationProperty
            = DependencyProperty.Register("Specialization", typeof(string), typeof(SkillEditor),
            new FrameworkPropertyMetadata(""));

        public string Specialization
        {
            get => (string)GetValue(SpecializationProperty);
            set => SetValue(SpecializationProperty, value);
        }

        public static DependencyProperty ModifierNameProperty
            = DependencyProperty.Register("ModifierName", typeof(string), typeof(SkillEditor),
            new FrameworkPropertyMetadata("INT", new PropertyChangedCallback(OnModifierNameChanged)));

        public string ModifierName
        {
            get => (string)GetValue(ModifierNameProperty);
            set => SetValue(ModifierNameProperty, value);
        }

        public string OriginalModifierName { get; set; } = "";

        public static DependencyProperty InfoUrlProperty
            = DependencyProperty.Register("InfoUrl", typeof(string), typeof(SkillEditor),
            new FrameworkPropertyMetadata("https://d20pfsrd.com/skills", new PropertyChangedCallback(OnModifierNameChanged)));

        public string InfoUrl
        {
            get => (string)GetValue(InfoUrlProperty);
            set => SetValue(InfoUrlProperty, value);
        }

        private static void OnModifierNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SkillEditor s)
            {
                s.ModifierNameChanged();
            }
        }

        public void LoadSkillData(Skill s)
        {
            IsTrained = s.ClassSkill;
            SkillRanks = ParseString(s.Ranks);
            MiscModifier = ParseString(s.Misc);
            RacialModifier = ParseString(s.Racial);
            TraitModifier = ParseString(s.Trait);
            Specialization = s.Specialization ?? "";

            UpdateCalculations();
        }

        private int ParseString(string? str)
        {
            if (int.TryParse(str ?? "0", out int i))
            {
                return i;
            }
            else
            {
                return 0;
            }
        }

        public Skill GetSkillData()
        {
            return new Skill()
            {
                Name = InternalSkillName,
                ClassSkill = IsTrained,
                Ranks = SkillRanks.ToString(),
                Misc = MiscModifier.ToString(),
                Racial = RacialModifier.ToString(),
                Trait = TraitModifier.ToString(),
                Total = txtTotal.Text,
                Specialization = Specialization.Trim('(', ')', ' ', '\t')
            };
        }

        protected void ModifierNameChanged()
        {
            if (_cbbc) return;

            cbbStat.SelectedIndex = ModifierName switch
            {
                "STR" => 0,
                "DEX" => 1,
                "CON" => 2,
                "INT" => 3,
                "WIS" => 4,
                "CHA" => 5,
                _ => 3,
            };
        }

        private void btnSpecialization_Click(object sender, RoutedEventArgs e)
        {
            StringInputDialog sid = new StringInputDialog(ColorScheme, "Edit Specialzation", "Edit the specialization for the \"" + SkillName + "\" skill.", Specialization.Trim('(', ')', ' ', '\t'));
            sid.Owner = OwnerWindow;
            sid.ShowDialog();

            if (sid.DialogResult)
            {
                Specialization = "(" + sid.Value + ")";
                ContentChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public Window? OwnerWindow { get; set; } = null;

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            popEdit.PlacementTarget = btnEdit;
            popEdit.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;

            popEdit.IsOpen = true;
            nudRanks.Focus();
        }

        private void cbbStat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_init) return;

            if (cbbStat.SelectedItem is ComboBoxItem cbbi)
            {
                if (cbbi.Content is string s)
                {
                    _cbbc = true;
                    ModifierName = s;
                    ModifierChanged?.Invoke(this, EventArgs.Empty);
                    ContentChanged?.Invoke(this, EventArgs.Empty);
                    UpdateCalculations();
                    _cbbc = false;
                }
            }
        }

        private void popEdit_Opened(object sender, EventArgs e)
        {
            btnEdit.IsSelected = true;
        }

        private void popEdit_Closed(object sender, EventArgs e)
        {
            btnEdit.IsSelected = false;
        }

        private void popEdit_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                popEdit.IsOpen = false;
                btnEdit.Focus();
            }
        }

        public void UpdateCalculations()
        {
            int miscTotal = nudRanks.Value + nudRacial.Value + nudTrait.Value + nudMisc.Value + (chkSkill.IsChecked ? 3 : 0);
            int modifier = ModifierValue;

            txtMiscTotal.Text = miscTotal.ToString();
            txtMod.Text = modifier.ToString();

            txtTotal.Text = (miscTotal + modifier).ToString();
        }

        private void chkSkill_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (_init) return;

            UpdateCalculations();
            ContentChanged?.Invoke(this, EventArgs.Empty);
        }

        private void nudRanks_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_init) return;

            UpdateCalculations();
            ContentChanged?.Invoke(this, EventArgs.Empty);
        }

        private void btnInfo_Click(object sender, RoutedEventArgs e)
        {
            CoreUtils.OpenBrowser(InfoUrl);
        }
    }
}
