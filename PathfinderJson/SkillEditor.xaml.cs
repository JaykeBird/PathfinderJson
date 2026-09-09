using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SolidShineUi;
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
        }

        public ColorScheme ColorScheme { get => (ColorScheme)GetValue(ColorSchemeProperty); set => SetValue(ColorSchemeProperty, value); }

        /// <summary>The backing dependency property for <see cref="ColorScheme"/>. See the related property for details.</summary>
        public static readonly DependencyProperty ColorSchemeProperty
            = DependencyProperty.Register(nameof(ColorScheme), typeof(ColorScheme), typeof(SkillEditor),
            new FrameworkPropertyMetadata(new ColorScheme()));


        public void UpdateAppearance()
        {
            ColorScheme = App.ColorScheme;
            //btnEdit.ColorScheme = App.ColorScheme;
            //btnInfo.ColorScheme = App.ColorScheme;
            //btnModifiers.ColorScheme = App.ColorScheme;
            //imgEdit.ColorScheme = App.ColorScheme;
            //imgInfo.ColorScheme = App.ColorScheme;
            //chkSkill.ColorScheme = App.ColorScheme;
            if (App.ColorScheme.IsHighContrast)
            {
                txtModifier.BorderBrush = new SolidColorBrush(App.ColorScheme.LightDisabledColor);
                txtModifier.Background = new SolidColorBrush(SystemColors.ControlColor);
            }
            else
            {
                txtModifier.BorderBrush = new SolidColorBrush(SystemColors.ControlDarkColor);
                txtModifier.Background = new SolidColorBrush(App.ColorScheme.SecondHighlightColor);
            }
        }

        bool _modifiersOpened = false;
        bool _wideState = false;
        public const int WIDE_STATE_THRESHOLD = 610;

        public string SkillName
        {
            get
            {
                return title;
            }
            set
            {
                title = value;
                if (!string.IsNullOrEmpty(specialization))
                {
                    txtName.Text = title + " (" + specialization + ")";
                }
                else
                {
                    txtName.Text = title;
                }
            }
        }

        public string SkillInternalName { get; set; } = "";
        public string SkillOnlineName { get; set; } = "";

        public string SkillAbility
        {
            get
            {
                return lblSkillAbility.Text;
            }
            set
            {
                lblSkillAbility.Text = value;
            }
        }

        public bool CanEditTitle
        {
            get
            {
                return btnEdit.Visibility == Visibility.Visible;
            }
            set
            {
                btnEdit.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                chkSkill.Margin = value ? new Thickness(5, 0, 20, 0) : new Thickness(5, 0, 0, 0);
            }
        }

        string title = "";
        string? specialization = null;

        public void LoadSkillData(Skill skill)
        {
            if (skill.ClassSkill) chkSkill.IsChecked = true;

            txtTotal.ValueString = skill.Total ?? "";
            txtRacial.ValueString = skill.Racial ?? "";
            txtRanks.ValueString = skill.Ranks ?? "";
            txtTrait.ValueString = skill.Trait ?? "";
            txtMisc.Text = skill.Misc;

            if (!string.IsNullOrEmpty(skill.Specialization))
            {
                specialization = skill.Specialization;
                txtName.Text = title + " (" + specialization + ")";
            }
            else
            {
                txtName.Text = title;
            }

            ToolTip tt = new ToolTip();
            tt.Content = "Racial: \"" + txtRacial.ValueString + "\" Trait: \"" + txtTrait.ValueString + "\" Misc: \"" + txtMisc.Text + "\"";
            btnModifiers.ToolTip = tt;
        }

        public Skill GetSkillData()
        {
            Skill s = new Skill
            {
                ClassSkill = chkSkill.IsChecked == true,
                Misc = GetStringOrNull(txtMisc.Text, true),
                Racial = GetStringOrNull(txtRacial.ValueString, true),
                Ranks = GetStringOrNull(txtRanks.ValueString, true),
                Specialization = specialization,
                Total = GetStringOrNull(txtTotal.ValueString, true),
                Trait = GetStringOrNull(txtTrait.ValueString, true)
            };

            return s;
        }

        public void LoadModifier(string modifier)
        {
            txtModifier.Text = modifier;
        }

        public async Task UpdateTotals(CancellationToken ct, int modifier)
        {
            int total = 0;

            int ranks = txtRanks.Value ?? 0;
            string misc = txtMisc.Text;
            int racial = txtRacial.Value ?? 0;
            int trait = txtTrait.Value ?? 0;

            await Task.Run(() =>
            {
                total += ranks;
                total += modifier;
                if (ct.IsCancellationRequested) return;
                try { total += int.Parse(misc); } catch (FormatException) { }
                if (ct.IsCancellationRequested) return;
                total += racial;
                if (ct.IsCancellationRequested) return;
                total += trait;
            });

            txtTotal.Value = total;
        }

        private void btnModifiers_Click(object sender, RoutedEventArgs e)
        {
            if (expander.IsExpanded)
            {
                expander.IsExpanded = false;
            }
            else
            {
                expander.IsExpanded = true;
            }
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            if (!_wideState)
            {
                colName.Width = new GridLength(0);
                colBase.Width = new GridLength(0);
                colExtra.Width = new GridLength(1, GridUnitType.Star);
                Background = new SolidColorBrush(App.ColorScheme.LightBackgroundColor);

                ToolTip tt = new ToolTip();
                tt.Content = SkillName;
                btnModifiers.ToolTip = tt;
                _modifiersOpened = true;
            }
        }

        private void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            if (!_wideState)
            {
                colName.Width = new GridLength(2, GridUnitType.Star);
                colBase.Width = new GridLength(172);
                //colModifiers.Width = new GridLength(0);
                colExtra.Width = new GridLength(0);
                Background = new SolidColorBrush(Colors.Transparent);

                ToolTip tt = new ToolTip();
                tt.Content = "Racial: \"" + txtRacial.ValueString + "\" Trait: \"" + txtTrait.ValueString + "\" Misc: \"" + txtMisc.Text + "\"";
                btnModifiers.ToolTip = tt;
                _modifiersOpened = false;
            }
        }

        public Window? OwnerWindow { get; set; } = null;

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            SolidShineUi.StringInputDialog sid = new SolidShineUi.StringInputDialog(App.ColorScheme, "Edit Skill", "Edit the specialization for this skill.", specialization ?? "");
            sid.SelectTextOnFocus = true;
            if (OwnerWindow != null) sid.Owner = OwnerWindow;
            sid.ShowDialog();
            if (sid.DialogResult)
            {
                specialization = sid.Value;
                if (!string.IsNullOrEmpty(specialization))
                {
                    txtName.Text = title + " (" + specialization + ")";
                }
                else
                {
                    txtName.Text = title;
                }
                ContentChanged?.Invoke(this, e);
            }
        }

        // event just to update main window's "isDirty" value
        public event EventHandler? ContentChanged;

        private void textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ContentChanged?.Invoke(this, e);
        }

        private void chkSkill_Checked(object sender, RoutedEventArgs e)
        {
            ContentChanged?.Invoke(this, e);
        }

        private void valueEditor_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ContentChanged?.Invoke(this, EventArgs.Empty);
        }

        private void btnInfo_Click(object sender, RoutedEventArgs e)
        {
            OpenBrowser(SkillOnlineName);
            //OpenBrowser("https://www.d20pfsrd.com/skills/" + SkillOnlineName);
        }

        private void userControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ActualWidth > WIDE_STATE_THRESHOLD)
            {
                _wideState = true;

                colName.Width = new GridLength(2, GridUnitType.Star);
                colBase.Width = new GridLength(172);
                Background = new SolidColorBrush(Colors.Transparent);

                colModifiers.Width = new GridLength(0);
                colModifiers.MinWidth = 0;
                expander.IsEnabled = false;
                btnModifiers.IsEnabled = false;
                colExtra.Width = new GridLength(3, GridUnitType.Star);
                colExtra.MinWidth = 280;
            }
            else
            {
                colModifiers.Width = new GridLength(0, GridUnitType.Auto);
                colModifiers.MinWidth = 85;
                expander.IsEnabled = true;
                btnModifiers.IsEnabled = true;
                colExtra.MinWidth = 0;

                _wideState = false;

                if (_modifiersOpened)
                {
                    colName.Width = new GridLength(0);
                    colBase.Width = new GridLength(0);
                    colExtra.Width = new GridLength(1, GridUnitType.Star);
                    Background = new SolidColorBrush(App.ColorScheme.LightBackgroundColor);

                    ToolTip tt = new ToolTip();
                    tt.Content = SkillName;
                    btnModifiers.ToolTip = tt;
                }
                else
                {
                    colName.Width = new GridLength(2, GridUnitType.Star);
                    colBase.Width = new GridLength(172);
                    //colModifiers.Width = new GridLength(0);
                    colExtra.Width = new GridLength(0);
                    Background = new SolidColorBrush(Colors.Transparent);

                    ToolTip tt = new ToolTip();
                    tt.Content = "Racial: \"" + txtRacial.ValueString + "\" Trait: \"" + txtTrait.ValueString + "\" Misc: \"" + txtMisc.Text + "\"";
                    btnModifiers.ToolTip = tt;
                }
            }
        }

        private void nudMisc_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

    }
}
