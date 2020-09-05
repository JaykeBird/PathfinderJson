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
using System.Windows.Shapes;
using SolidShineUi;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for DiceRoller.xaml
    /// </summary>
    public partial class DiceRollerWindow : FlatWindow
    {
        public DiceRollerWindow()
        {
            InitializeComponent();
            txtInput.Focus();
        }

        private void window_SourceInitialized(object sender, EventArgs e)
        {
            DisableMaximizeAction();
        }

        private void btnRoll_Click(object sender, RoutedEventArgs e)
        {
            Roll();
        }

        void Roll()
        {
            try
            {
                (string str, double res) = DiceRoller.RollDice(txtInput.Text);
                txtRolls.Text = str;
                txtResult.Text = res.ToString();
            }
            catch (FormatException ex)
            {
                txtRolls.Text = "Error: " + ex.Message;
                txtResult.Text = "";
            }
            catch (OverflowException)
            {
                txtRolls.Text = "Error: Cannot roll negative number of dice.";
                txtResult.Text = "";
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void txtInput_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Roll();
            }
        }
    }
}
