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

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for UndoStackTest.xaml
    /// </summary>
    public partial class UndoStackTest : Window
    {
        public UndoStackTest()
        {
            InitializeComponent();
        }

        UndoStack<string> undos = new UndoStack<string>();

        private void button_Click(object sender, RoutedEventArgs e)
        {
            // store state
            undos.StoreState(textBox.Text);

            UpdateStatusLabels();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            // undo
            if (undos.CanUndo)
            {
                textBox.Text = undos.Undo();
                UpdateStatusLabels("undo");
            }
            else
            {
                UpdateStatusLabels("can't undo");
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            // redo
            if (undos.CanRedo)
            {
                textBox.Text = undos.Redo();
                UpdateStatusLabels("redo");
            }
            else
            {
                UpdateStatusLabels("can't redo");
            }
        }

        private void UpdateStatusLabels(string lastAction = "updateState")
        {
            tb1.Text = undos.UndoCount.ToString();
            tb2.Text = undos.RedoCount.ToString();
            if (undos.UndoCount > 0)
            {
                tb3.Text = undos.PeekUndo();
            }
            else
            {
                tb3.Text = "-";
            }
            tb4.Text = lastAction;
        }
    }
}
