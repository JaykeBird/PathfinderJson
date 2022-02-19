using SolidShineUi;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace PathfinderJson
{
    /// <summary>
    /// Interaction logic for CountersWindow.xaml
    /// </summary>
    public partial class CountersWindow : FlatWindow
    {
        public CountersWindow()
        {
            InitializeComponent();
            ColorScheme = App.ColorScheme;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            CounterItem ci = new CounterItem();
            ci.ColorScheme = this.ColorScheme;
            ci.Margin = new Thickness(8, 6, 8, 6);
            ci.RemoveRequested += counter_RemoveRequested;
            grdCounters.Children.Add(ci);
        }

        private void counter_RemoveRequested(object? sender, EventArgs e)
        {
            if (sender is CounterItem ci)
            {
                grdCounters.Children.Remove(ci);
            }
        }

        public async void WriteJsonFile(string filename)
        {
            List<string> values = new List<string>();
            values.Add("{");
            values.Add("  counters: [");
            bool first = true;
            foreach (UIElement? item in grdCounters.Children)
            {
                if (item is CounterItem ci)
                {
                    values.Add("    " + (first ? "" : ",") + ci.WriteJson());
                    first = false;
                }
            }
            values.Add("  ]");
            values.Add("}");

            await System.IO.File.WriteAllLinesAsync(filename, values, new UTF8Encoding(false));
        }

        public async void ReadJsonFile(string filename)
        {
            string content =  await System.IO.File.ReadAllTextAsync(filename);
            JObject? jo = null;
            try
            {
                jo = JObject.Parse(content);
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                throw new FormatException("The inputted file is not a valid JSON.", e);
            }

            if (jo != null)
            {
                var jta = jo["counters"];
                if (jta != null)
                {
                    if (jta.Type == JTokenType.Array)
                    {
                        JArray jca = (JArray)jta;

                        foreach (JToken jt in jca.Children())
                        {
                            CounterItem ci = new CounterItem();
                            ci.ReadJson((JObject)jt);
                            ci.ColorScheme = this.ColorScheme;
                            ci.Margin = new Thickness(8, 6, 8, 6);
                            ci.RemoveRequested += counter_RemoveRequested;
                            grdCounters.Children.Add(ci);
                        }
                    }
                }
            }
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Open Counters File";
            ofd.Filter = "JSON Data File|*.json|All Files|*.*";
            ofd.Multiselect = false;

            if (ofd.ShowDialog() ?? false == true)
            {
                ReadJsonFile(ofd.FileName);
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Save Counters File";
            sfd.Filter = "JSON Data File|*.json";

            if (sfd.ShowDialog() ?? false == true)
            {
                WriteJsonFile(sfd.FileName);
            }
        }
    }
}
