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

        public void UpdateAppearance()
        {
            btnEdit.ColorScheme = App.ColorScheme;
            btnModifiers.ColorScheme = App.ColorScheme;
            txtModifier.Background = new SolidColorBrush(App.ColorScheme.SecondHighlightColor);
        }

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

            txtTotal.Text = skill.Total;
            txtRacial.Text = skill.Racial;
            txtRanks.Text = skill.Ranks;
            txtTrait.Text = skill.Trait;
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
        }

        public Skill GetSkillData()
        {
            Skill s = new Skill
            {
                ClassSkill = chkSkill.IsChecked == true,
                Misc = txtMisc.Text,
                Racial = txtRacial.Text,
                Ranks = txtRanks.Text,
                Specialization = specialization,
                Total = txtTotal.Text,
                Trait = txtTrait.Text
            };

            return s;
        }

        public void LoadModifier(string modifier)
        {
            txtModifier.Text = modifier;
        }

        public void UpdateTotals()
        {
            int total = 0;
            try { total += int.Parse(txtRanks.Text); } catch (FormatException) { }
            try { total += int.Parse(txtMisc.Text); } catch (FormatException) { }
            try { total += int.Parse(txtModifier.Text); } catch (FormatException) { }
            try { total += int.Parse(txtRacial.Text); } catch (FormatException) { }
            try { total += int.Parse(txtTrait.Text); } catch (FormatException) { }

            txtTotal.Text = total.ToString();
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
            colName.Width = new GridLength(0);
            colBase.Width = new GridLength(0);
            colExtra.Width = new GridLength(1, GridUnitType.Star);
            Background = new SolidColorBrush(App.ColorScheme.LightBackgroundColor);
        }

        private void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            colName.Width = new GridLength(1, GridUnitType.Star);
            colBase.Width = new GridLength(170);
            //colModifiers.Width = new GridLength(0);
            colExtra.Width = new GridLength(0);
            Background = new SolidColorBrush(Colors.Transparent);
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            UiCore.StringInputDialog sid = new UiCore.StringInputDialog(App.ColorScheme, "Edit Skill", "Edit the specialization for this skill.", specialization);
            sid.SelectTextOnFocus = true;
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
            }
        }

        // event just to update main window's "isDirty" value
        public event EventHandler? ContentChanged;

        private void textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ContentChanged?.Invoke(this, e);
        }
    }
}
