using SolidShineUi;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for UndoStackTest.xaml
    /// </summary>
    public partial class UndoStackTest : FlatWindow
    {
        public UndoStackTest()
        {
            InitializeComponent();

            button.SetBinding(FlatButton.ColorSchemeProperty, new Binding("ColorScheme") { Source = this } );
            button1.SetBinding(FlatButton.ColorSchemeProperty, new Binding("ColorScheme") { Source = this });
            button2.SetBinding(FlatButton.ColorSchemeProperty, new Binding("ColorScheme") { Source = this });
            btnReload.SetBinding(FlatButton.ColorSchemeProperty, new Binding("ColorScheme") { Source = this });
            selUndo.SetBinding(SelectPanel.ColorSchemeProperty, new Binding("ColorScheme") { Source = this });
            selRedo.SetBinding(SelectPanel.ColorSchemeProperty, new Binding("ColorScheme") { Source = this });

            scrUndoHistory.Background = ColorScheme.BackgroundColor.ToBrush();
            grdBasicTest.Background = ColorScheme.BackgroundColor.ToBrush();
        }

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUndoStack();
        }

        private void window_ColorSchemeChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is ColorScheme cs)
            {
                Background = cs.SecondHighlightColor.ToBrush();
                ContentBackground = cs.SecondHighlightColor.ToBrush();
                HighlightBrush = cs.MainColor.ToBrush();

                scrUndoHistory.Background = cs.BackgroundColor.ToBrush();
                grdBasicTest.Background = cs.BackgroundColor.ToBrush();
            }
        }

        public MainWindow? MainWindow { get; set; } = null;

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

        private void btnReload_Click(object sender, RoutedEventArgs e)
        {
            LoadUndoStack();
        }

        void LoadUndoStack()
        {
            if (MainWindow == null) return;

            selRedo.Items.Clear();
            selUndo.Items.Clear();

            UndoStack<PathfinderSheet> undoStack = MainWindow.GetUndoStack();

            PathfinderSheet? previous = null;
            int count = 0;

            // list out the UndoItems
            foreach (PathfinderSheet item in undoStack.UndoItems)
            {
                SelectableItem si = new SelectableItem("Undo " + count);

                if (previous != null)
                {
                    // compare item to previous
                    CompareResult cr = Compare.CompareObjects(previous, item);
                    if (cr.Success == CompareSuccessValue.Success)
                    {
                        StringBuilder sb = new StringBuilder();
                        List<string> props = cr.DifferingProperties;
                        foreach (string prop in props)
                        {
                            sb.AppendLine(prop + ": " + GetPropertyValueAsString(previous, prop) + ", " + GetPropertyValueAsString(item, prop));
                        }

                        si.RightText = sb.ToString();
                        si.Height = double.NaN;
                    }
                }
                else
                {
                    si.RightText = "(new)";
                }

                selUndo.Items.Add(si);

                previous = item;
                count++;
            }

            previous = null;
            count = 0;

            // list out the RedoItems
            foreach (PathfinderSheet item in undoStack.RedoItems)
            {
                SelectableItem si = new SelectableItem("Redo " + count);

                if (previous != null)
                {
                    // compare item to previous
                    CompareResult cr = Compare.CompareObjects(previous, item);
                    if (cr.Success == CompareSuccessValue.Success)
                    {
                        StringBuilder sb = new StringBuilder();
                        List<string> props = cr.DifferingProperties;
                        foreach (string prop in props)
                        {
                            sb.AppendLine(prop + ": " + GetPropertyValueAsString(previous, prop) + ", " + GetPropertyValueAsString(item, prop));
                        }

                        si.RightText = sb.ToString();
                        si.Height = double.NaN;
                    }
                }
                else
                {
                    si.RightText = "(new)";
                }

                selRedo.Items.Add(si);

                previous = item;
                count++;
            }
        }

        string GetPropertyValueAsString<T>(T item, string propertyName)
        {
            Type tt = typeof(T);
            var prop = tt.GetProperty(propertyName);
            if (prop == null)
            {
                return "(does not exist)";
            }
            else
            {
                object? val = prop.GetValue(item, null);

                if (val is ICollection il)
                {
                    List<string> colStrs = new List<string>();
                    foreach (var colItem in il)
                    {
                        colStrs.Add(colItem?.ToString() ?? "(null)");
                    }
                    return string.Join(",", colStrs);
                }
                else
                {
                    return val?.ToString() ?? "(null)";
                }
            }
        }
    }
}
